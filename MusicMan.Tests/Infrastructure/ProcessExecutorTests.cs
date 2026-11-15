using System.ComponentModel;
using FluentAssertions;

namespace MusicMan.Tests.Infrastructure;

public sealed class ProcessExecutorTests
{
    [Fact]
    public async Task RunAsync_ReturnsExitCodeAndDrainsStandardError()
    {
        List<string> errors = [];
        (string exe, ProcessArguments args) = Shell(
            "echo stdout-line&echo first-error>&2&echo last-error>&2&exit /b 7",
            "printf 'stdout-line\n'; printf 'first-error\nlast-error\n' >&2; exit 7");

        int exitCode = await ProcessExecutor.RunAsync(exe, args, errors.Add);

        exitCode.Should().Be(7);
        errors.Should().Equal("first-error", "last-error");
    }

    [Fact]
    public async Task RunAsync_FiltersProgressButPassesNoiseToCustomErrorHandler()
    {
        List<string> errors = [];
        (string exe, ProcessArguments args) = Shell(
            "echo frame=1 fps=20>&2&echo [youtube] detail>&2&echo actual-error>&2",
            "printf 'frame=1 fps=20\n[youtube] detail\nactual-error\n' >&2");

        int exitCode = await ProcessExecutor.RunAsync(exe, args, errors.Add);

        exitCode.Should().Be(0);
        errors.Should().Equal("[youtube] detail", "actual-error");
    }

    [Fact]
    public async Task RunAsync_MissingExecutableThrows()
    {
        Func<Task> run = () => ProcessExecutor.RunAsync(
            Path.Combine(Path.GetTempPath(), $"musicman-missing-{Guid.NewGuid():N}"),
            Array.Empty<string>());

        await run.Should().ThrowAsync<Win32Exception>();
    }

    [Fact]
    public async Task RunAsync_PreCanceledTokenDoesNotStartProcess()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Func<Task> run = () => ProcessExecutor.RunAsync(
            "nonexistent-executable", Array.Empty<string>(), cancellationToken: cancellation.Token);

        await run.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RunAsync_CancellationDuringOutputPropagates()
    {
        using CancellationTokenSource cancellation = new();
        (string exe, ProcessArguments args) = Shell(
            "echo ready>&2&ping -n 30 127.0.0.1 >nul",
            "printf 'ready\n' >&2; sleep 30");

        Func<Task> run = () => ProcessExecutor.RunAsync(
            exe, args, _ => cancellation.Cancel(), cancellation.Token);

        await run.Should().ThrowAsync<OperationCanceledException>();
    }

    private static (string Exe, ProcessArguments Args) Shell(string windows, string unix)
    {
        return OperatingSystem.IsWindows()
            ? (Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe", new ProcessArguments(["/d", "/c", windows]))
            : ("/bin/sh", new ProcessArguments(["-c", unix]));
    }
}
