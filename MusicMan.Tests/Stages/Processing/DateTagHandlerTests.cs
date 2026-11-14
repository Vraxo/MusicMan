using FluentAssertions;

namespace MusicMan.Tests.Stages.Processing;

public sealed class DateTagHandlerTests
{
    [Fact]
    public void XiphComment_AppliesReadsRoundTripsAndClearsFullDate()
    {
        TagLib.Ogg.XiphComment xiph = new();

        DateTagHandler.SupportsFullDate(xiph).Should().BeTrue();
        DateTagHandler.GetExpected(xiph, "2024-03-01").Should().Be("2024-03-01");

        DateTagHandler.Apply(xiph, "2024-03-01");

        TagLib.Ogg.XiphComment reloaded = new(xiph.Render(false));
        DateTagHandler.Read(reloaded).Should().Be("2024-03-01");
        reloaded.GetFirstField("DATE").Should().Be("2024-03-01");
        reloaded.Year.Should().Be(2024u);

        DateTagHandler.Apply(reloaded, null);

        TagLib.Ogg.XiphComment cleared = new(reloaded.Render(false));
        DateTagHandler.Read(cleared).Should().BeNull();
        cleared.GetFirstField("DATE").Should().BeNull();
        cleared.Year.Should().Be(0u);
    }

    [Fact]
    public void XiphComment_UpgradesExistingYearOnlyDateToFullDate()
    {
        TagLib.Ogg.XiphComment xiph = new() { Year = 2024 };

        DateTagHandler.Read(xiph).Should().Be("2024");
        DateTagHandler.GetExpected(xiph, "2024-03-01").Should().Be("2024-03-01");

        DateTagHandler.Apply(xiph, "2024-03-01");

        DateTagHandler.Read(xiph).Should().Be("2024-03-01");
        xiph.Year.Should().Be(2024u);
    }

    [Fact]
    public void GroupedComment_AppliesReadsAndClearsAcrossComments()
    {
        TagLib.Ogg.GroupedComment grouped = new();
        TagLib.Ogg.XiphComment primary = new();
        TagLib.Ogg.XiphComment secondary = new();
        secondary.SetField("DATE", "1999-12-31");
        grouped.AddComment(1u, primary);
        grouped.AddComment(2u, secondary);

        DateTagHandler.SupportsFullDate(grouped).Should().BeTrue();

        DateTagHandler.Apply(grouped, "2024-03-01");
        DateTagHandler.Read(grouped).Should().Be("2024-03-01");
        primary.GetFirstField("DATE").Should().Be("2024-03-01");

        DateTagHandler.Apply(grouped, "   ");
        DateTagHandler.Read(grouped).Should().BeNull();
        primary.GetFirstField("DATE").Should().BeNull();
        secondary.GetFirstField("DATE").Should().BeNull();
    }

    [Fact]
    public void AppleTag_AppliesReadsAndClearsFullDate()
    {
        TagLib.Mpeg4.AppleTag apple = new(new TagLib.Mpeg4.IsoUserDataBox());

        DateTagHandler.SupportsFullDate(apple).Should().BeTrue();
        DateTagHandler.GetExpected(apple, "2024-03-01").Should().Be("2024-03-01");

        DateTagHandler.Apply(apple, "2024-03-01");

        DateTagHandler.Read(apple).Should().Be("2024-03-01");
        apple.Year.Should().Be(2024u);

        DateTagHandler.Apply(apple, null);

        DateTagHandler.Read(apple).Should().BeNull();
        apple.Year.Should().Be(0u);
    }

    [Fact]
    public void Id3v24Tag_AppliesReadsRoundTripsAndClearsFullDate()
    {
        TagLib.Id3v2.Tag id3v2 = new() { Version = 4 };

        DateTagHandler.SupportsFullDate(id3v2).Should().BeTrue();
        DateTagHandler.GetExpected(id3v2, "2024-03-01").Should().Be("2024-03-01");

        DateTagHandler.Apply(id3v2, "2024-03-01");

        TagLib.Id3v2.Tag reloaded = new(id3v2.Render());
        DateTagHandler.Read(reloaded).Should().Be("2024-03-01");
        reloaded.GetTextAsString("TDRC").Should().Be("2024-03-01");
        reloaded.Year.Should().Be(2024u);

        DateTagHandler.Apply(reloaded, null);

        TagLib.Id3v2.Tag cleared = new(reloaded.Render());
        DateTagHandler.Read(cleared).Should().BeNull();
        cleared.Year.Should().Be(0u);
    }

    [Fact]
    public void Id3v2TagWithFooter_SupportsFullDate()
    {
        TagLib.Id3v2.Tag id3v2 = new()
        {
            Version = 3,
            Flags = TagLib.Id3v2.HeaderFlags.FooterPresent
        };

        DateTagHandler.SupportsFullDate(id3v2).Should().BeTrue();

        DateTagHandler.Apply(id3v2, "2024-03-01");

        TagLib.Id3v2.Tag reloaded = new(id3v2.Render());
        DateTagHandler.Read(reloaded).Should().Be("2024-03-01");
        reloaded.Version.Should().Be((byte)4);
    }

    [Fact]
    public void Id3v23Tag_FallsBackToYearOnlyAndClearsCleanly()
    {
        TagLib.Id3v2.Tag id3v2 = new() { Version = 3 };

        DateTagHandler.SupportsFullDate(id3v2).Should().BeFalse();
        DateTagHandler.GetExpected(id3v2, "2024-03-01").Should().Be("2024");

        DateTagHandler.Apply(id3v2, "2024-03-01");

        TagLib.Id3v2.Tag reloaded = new(id3v2.Render());
        DateTagHandler.Read(reloaded).Should().Be("2024");
        reloaded.Year.Should().Be(2024u);
        reloaded.GetTextAsString("TDRC").Should().Be("2024");
        reloaded.GetFrames("TDAT").Should().BeEmpty();

        DateTagHandler.Apply(reloaded, null);

        TagLib.Id3v2.Tag cleared = new(reloaded.Render());
        DateTagHandler.Read(cleared).Should().BeNull();
        cleared.Year.Should().Be(0u);
    }

    [Fact]
    public void RiffInfoTag_AppliesReadsRoundTripsAndClearsFullDate()
    {
        TagLib.Riff.InfoTag info = new();

        DateTagHandler.SupportsFullDate(info).Should().BeTrue();
        DateTagHandler.GetExpected(info, "2024-03-01").Should().Be("2024-03-01");

        DateTagHandler.Apply(info, "2024-03-01");

        TagLib.Riff.InfoTag reloaded = new(info.Render());
        DateTagHandler.Read(reloaded).Should().Be("2024-03-01");
        string[] values = reloaded.GetValuesAsStrings("ICRD");
        values.Should().HaveCount(1);
        values[0].Should().Be("2024-03-01");

        DateTagHandler.Apply(reloaded, null);

        TagLib.Riff.InfoTag cleared = new(reloaded.Render());
        DateTagHandler.Read(cleared).Should().BeNull();
        cleared.GetValuesAsStrings("ICRD").Should().BeEmpty();
    }

    [Fact]
    public void ApeTag_AppliesReadsRoundTripsAndClearsFullDate()
    {
        TagLib.Ape.Tag ape = new();

        DateTagHandler.SupportsFullDate(ape).Should().BeTrue();
        DateTagHandler.GetExpected(ape, "2024-03-01").Should().Be("2024-03-01");

        DateTagHandler.Apply(ape, "2024-03-01");

        TagLib.Ape.Tag reloaded = new(ape.Render());
        DateTagHandler.Read(reloaded).Should().Be("2024-03-01");
        reloaded.Year.Should().Be(2024u);

        DateTagHandler.Apply(reloaded, null);

        TagLib.Ape.Tag cleared = new(reloaded.Render());
        DateTagHandler.Read(cleared).Should().BeNull();
        cleared.Year.Should().Be(0u);
    }

    [Fact]
    public void Id3v1Tag_FallsBackToYearOnlyAndClearsCleanly()
    {
        TagLib.Id3v1.Tag id3v1 = new();

        DateTagHandler.SupportsFullDate(id3v1).Should().BeFalse();
        DateTagHandler.GetExpected(id3v1, "2024-03-01").Should().Be("2024");

        DateTagHandler.Apply(id3v1, "2024-03-01");
        DateTagHandler.Read(id3v1).Should().Be("2024");
        id3v1.Year.Should().Be(2024u);

        DateTagHandler.Apply(id3v1, null);
        DateTagHandler.Read(id3v1).Should().BeNull();
        id3v1.Year.Should().Be(0u);
    }

    [Fact]
    public void CombinedTag_PrefersFullDateChildOverYearOnlySibling()
    {
        TagLib.Id3v2.Tag id3v23 = new() { Version = 3 };
        TagLib.Riff.InfoTag info = new();
        TagLib.CombinedTag combined = new(id3v23, info);

        DateTagHandler.SupportsFullDate(combined).Should().BeTrue();
        DateTagHandler.GetExpected(combined, "2024-03-01").Should().Be("2024-03-01");

        DateTagHandler.Apply(combined, "2024-03-01");

        id3v23.Year.Should().Be(2024u);
        DateTagHandler.Read(info).Should().Be("2024-03-01");
        DateTagHandler.Read(combined).Should().Be("2024-03-01");

        DateTagHandler.Apply(combined, null);

        id3v23.Year.Should().Be(0u);
        DateTagHandler.Read(info).Should().BeNull();
        DateTagHandler.Read(combined).Should().BeNull();
    }

    [Fact]
    public void CombinedTag_WithId3v24AndId3v1_AppliesReadsAndClearsBoth()
    {
        TagLib.Id3v2.Tag id3v24 = new() { Version = 4 };
        TagLib.Id3v1.Tag id3v1 = new();
        TagLib.CombinedTag combined = new(id3v24, id3v1);

        DateTagHandler.SupportsFullDate(combined).Should().BeTrue();

        DateTagHandler.Apply(combined, "2024-03-01");

        DateTagHandler.Read(id3v24).Should().Be("2024-03-01");
        id3v1.Year.Should().Be(2024u);
        DateTagHandler.Read(combined).Should().Be("2024-03-01");

        DateTagHandler.Apply(combined, null);

        DateTagHandler.Read(id3v24).Should().BeNull();
        id3v1.Year.Should().Be(0u);
        DateTagHandler.Read(combined).Should().BeNull();
    }

    [Theory]
    [InlineData("2024-03-01", "2024-03-01", 2024u)]
    [InlineData("  2024-03  ", "2024-03", 2024u)]
    [InlineData("2024", "2024", 2024u)]
    [InlineData("03/01/2024", "03/01/2024", 0u)]
    [InlineData("   ", null, 0u)]
    [InlineData(null, null, 0u)]
    public void NormalizeDateAndExtractYear_HandleVariousInputs(string? input, string? expectedNormalized, uint expectedYear)
    {
        DateTagHandler.NormalizeDate(input).Should().Be(expectedNormalized);
        DateTagHandler.ExtractYear(input).Should().Be(expectedYear);
    }

    [Fact]
    public void GetExpected_WithBlankDate_ReturnsEmptyString()
    {
        TagLib.Ogg.XiphComment xiph = new();
        TagLib.Id3v1.Tag id3v1 = new();

        DateTagHandler.GetExpected(xiph, null).Should().BeEmpty();
        DateTagHandler.GetExpected(xiph, "   ").Should().BeEmpty();
        DateTagHandler.GetExpected(id3v1, null).Should().BeEmpty();
        DateTagHandler.GetExpected(id3v1, "not-a-year").Should().BeEmpty();
    }
}
