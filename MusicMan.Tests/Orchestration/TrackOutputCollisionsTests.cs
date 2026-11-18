using FluentAssertions;
using MusicMan.Orchestration;

namespace MusicMan.Tests.Orchestration;

public sealed class TrackOutputCollisionsTests
{
    private static readonly string BaseDataDir = Path.Combine(Path.GetTempPath(), "musicman-library");

    [Theory]
    [InlineData("Hits", "Intro", "Hits", "Intro", "Hits")]
    [InlineData(" Hits: ", "In/tro?", "Hits", "Intro", "Hits")]
    [InlineData("Hits", "Intro", "hits", "intro", "Hits")]
    [InlineData("", "Intro", "?", "Intro", "")]
    [InlineData("", "Intro", ".", "Intro", "")]
    public void Find_WithMatchingDestinations_ReportsBothCsvRows(
        string firstAlbum, string firstTitle, string secondAlbum, string secondTitle, string outputAlbum)
    {
        Track first = new() { Album = firstAlbum, Title = firstTitle, Artist = "Artist A", CsvLineNumber = 2 };
        Track second = new() { Album = secondAlbum, Title = secondTitle, Artist = "Artist B", CsvLineNumber = 5 };

        IGrouping<string, Track> collision = TrackOutputCollisions.Find([first, second], BaseDataDir)
            .Should().ContainSingle().Subject;

        collision.Key.Should().Be(Path.Combine(BaseDataDir, outputAlbum, "Intro.opus"));
        collision.Select(track => track.CsvLineNumber).Should().Equal(2, 5);
    }

    [Fact]
    public void Find_WithIdenticalRows_ReportsBoth()
    {
        Track first = new() { Album = "Hits", Title = "Intro", Source = "same-source", CsvLineNumber = 2 };
        Track second = first with { CsvLineNumber = 3 };

        TrackOutputCollisions.Find([first, second], BaseDataDir).Should().ContainSingle()
            .Subject.Should().Equal(first, second);
    }

    [Fact]
    public void Find_WithDistinctDestinations_ReportsNothing()
    {
        Track[] tracks =
        [
            new() { Album = "First", Title = "Intro" },
            new() { Album = "Second", Title = "Intro" },
            new() { Album = "First", Title = "Outro" }
        ];

        TrackOutputCollisions.Find(tracks, BaseDataDir).Should().BeEmpty();
    }
}
