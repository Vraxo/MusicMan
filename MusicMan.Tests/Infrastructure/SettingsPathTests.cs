using FluentAssertions;

namespace MusicMan.Tests.Infrastructure;

public sealed class SettingsPathTests
{
    [Fact]
    public void ResolvePaths_AnchorsDefaultsAndRelativeToolDirectories()
    {
        string root = Path.Combine(Path.GetTempPath(), "musicman-install");
        Settings settings = new() { YtDlpDir = "tools", FfmpegDir = Path.Combine("tools", "ffmpeg") };

        SettingsManager.ResolvePaths(settings, root);

        settings.DatabaseDir.Should().Be(Path.Combine(root, "Music", "base"));
        settings.CoversDir.Should().Be(Path.Combine(root, "Music", "covers"));
        settings.BaseDataDir.Should().Be(Path.Combine(root, "Music", "tracks"));
        settings.CookieFile.Should().Be(Path.Combine(root, "Data", "cookies.txt"));
        settings.PlaylistCheckReportFile.Should().Be(Path.Combine(root, "Data", "PlaylistReport.txt"));
        settings.YtDlpDir.Should().Be(Path.Combine(root, "tools"));
        settings.FfmpegDir.Should().Be(Path.Combine(root, "tools", "ffmpeg"));
    }

    [Fact]
    public void ResolvePaths_PreservesAbsolutePathsAndEmptyToolDirectories()
    {
        string root = Path.Combine(Path.GetTempPath(), "musicman-install");
        string external = Path.Combine(Path.GetTempPath(), "external-library");
        Settings settings = new() { DatabaseDir = external, CookieFile = external };

        SettingsManager.ResolvePaths(settings, root);

        settings.DatabaseDir.Should().Be(external);
        settings.CookieFile.Should().Be(external);
        settings.YtDlpDir.Should().BeEmpty();
        settings.FfmpegDir.Should().BeEmpty();
    }
}
