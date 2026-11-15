using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace MusicMan.Infrastructure;

internal class ProcessExecutor
{
    public static async Task<int> RunAsync(
        string exe,
        ProcessArguments args,
        Action<string>? stdErrHandler = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using Process proc = CreateProcess(exe, args);
            proc.Start();

            try
            {
                await foreach (ProcessOutputLine line in proc.ReadAllLinesAsync(cancellationToken))
                {
                    string data = line.Content;
                    if (string.IsNullOrEmpty(data) || IsProgressLine(data))
                    {
                        continue;
                    }

                    if (line.StandardError)
                    {
                        if (stdErrHandler is not null)
                        {
                            stdErrHandler(data);
                        }
                        else
                        {
                            HandleStdErr(data);
                        }
                    }
                    else if (!IsNoiseLine(data))
                    {
                        Log.Info(data);
                    }
                }

                await proc.WaitForExitAsync(cancellationToken);
                return proc.ExitCode;
            }
            catch
            {
                // Disposing Process releases its handle but doesn't stop the child.
                // Also clean up if output handling fails, not just on cancellation.
                try
                {
                    proc.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // The process has already exited.
                }

                await proc.WaitForExitAsync(CancellationToken.None);
                throw;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Win32Exception)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error($"Unexpected error running '{exe}': {ex.Message}");
            throw;
        }
    }

    private static Process CreateProcess(string exe, ProcessArguments args)
    {
        Process proc = new()
        {
            StartInfo = new()
            {
                FileName = exe,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        foreach (string arg in args)
        {
            proc.StartInfo.ArgumentList.Add(arg);
        }

        return proc;
    }

    private static bool IsProgressLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        return (line.Contains("[download]", StringComparison.OrdinalIgnoreCase) && line.Contains('%'))
            || line.Contains("[frag]", StringComparison.OrdinalIgnoreCase)
            || (line.Contains("frame=", StringComparison.OrdinalIgnoreCase) && line.Contains("fps=", StringComparison.OrdinalIgnoreCase))
            || (line.Contains("size=", StringComparison.OrdinalIgnoreCase) && line.Contains("time=", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsNoiseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return true;
        }

        return line.StartsWith("[youtube]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[generic]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("WARNING: [generic]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[redirect]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[info]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[download] Destination:", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("[ExtractAudio]", StringComparison.OrdinalIgnoreCase)
            || line.Contains("The url doesn't specify the protocol", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Extracting cookies from", StringComparison.OrdinalIgnoreCase)
            || (line.StartsWith("Extracted", StringComparison.OrdinalIgnoreCase) && line.Contains("cookies from", StringComparison.OrdinalIgnoreCase))
            || line.StartsWith("[ThumbnailsConvertor]", StringComparison.OrdinalIgnoreCase)
            || line.StartsWith("Deleting original file", StringComparison.OrdinalIgnoreCase);
    }

    private static void HandleStdErr(string data)
    {
        if (IsNoiseLine(data))
        {
            return;
        }

        if (data.Contains("DPAPI", StringComparison.OrdinalIgnoreCase))
        {
            Log.Error("Chrome cookie extraction failed due to a recent Google Chrome update.");
            Log.Error("Solution: Open 'Data/settings.toml', set CookiesBrowser = \"\", and use 'cookies.txt' instead.");
            return;
        }

        if (data.Contains("Sign in to confirm", StringComparison.OrdinalIgnoreCase) ||
            (data.Contains("confirm", StringComparison.OrdinalIgnoreCase) && data.Contains("not a bot", StringComparison.OrdinalIgnoreCase)))
        {
            Log.Error("ERROR: YouTube bot verification requested. Please configure 'CookiesBrowser' or 'CookieFile' in 'Data/settings.toml'.");
            return;
        }

        if (data.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            data.Contains("failed", StringComparison.OrdinalIgnoreCase) ||
            data.Contains("fatal", StringComparison.OrdinalIgnoreCase))
        {
            if (data.Contains("WARNING", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning(data);
            }
            else
            {
                Log.Error(data);
            }
        }
        else if (data.Contains("warning", StringComparison.OrdinalIgnoreCase))
        {
            Log.Warning(data);
        }
        else
        {
            Log.Info(data);
        }
    }
}