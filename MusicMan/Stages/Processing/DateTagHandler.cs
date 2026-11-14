using System.Globalization;

namespace MusicMan.Stages.Processing;

internal static class DateTagHandler
{
    private static readonly TagLib.ReadOnlyByteVector AppleDateBoxType = new((byte)0xa9, (byte)'d', (byte)'a', (byte)'y');
    private static readonly TagLib.ReadOnlyByteVector Id3v2DateFrameId = "TDRC";
    private static readonly TagLib.ReadOnlyByteVector Id3v2YearFrameId = "TYER";
    private static readonly TagLib.ReadOnlyByteVector Id3v2DayMonthFrameId = "TDAT";
    private static readonly TagLib.ReadOnlyByteVector RiffDateItemId = "ICRD";

    public static void Apply(TagLib.Tag tag, string? date)
    {
        string? normalizedDate = NormalizeDate(date);
        uint year = ExtractYear(normalizedDate);

        Apply(tag, normalizedDate, year);
    }

    public static string? Read(TagLib.Tag tag)
    {
        switch (tag)
        {
            case TagLib.Ogg.XiphComment xiph:
                return NormalizeDate(xiph.GetFirstField("DATE"));

            case TagLib.Mpeg4.AppleTag apple:
                foreach (TagLib.Mpeg4.AppleDataBox box in apple.DataBoxes(AppleDateBoxType))
                {
                    string? text = NormalizeDate(box.Text);
                    if (text is not null)
                    {
                        return text;
                    }
                }
                return null;

            case TagLib.Id3v2.Tag id3v2:
                if (SupportsFullDateId3v2(id3v2))
                {
                    return NormalizeDate(id3v2.GetTextAsString(Id3v2DateFrameId));
                }
                return FormatYear(id3v2.Year);

            case TagLib.Riff.InfoTag info:
                foreach (string value in info.GetValuesAsStrings(RiffDateItemId))
                {
                    string? text = NormalizeDate(value);
                    if (text is not null)
                    {
                        return text;
                    }
                }
                return null;

            case TagLib.Ape.Tag ape:
                return NormalizeDate(ape.GetItem("Year")?.ToString());

            case TagLib.Ogg.GroupedComment grouped:
                foreach (TagLib.Ogg.XiphComment comment in grouped.Comments)
                {
                    if (comment is null)
                    {
                        continue;
                    }

                    string? value = Read(comment);
                    if (value is not null)
                    {
                        return value;
                    }
                }
                return null;

            case TagLib.CombinedTag combined:
                return ReadCombined(combined);

            default:
                return FormatYear(tag.Year);
        }
    }

    public static string GetExpected(TagLib.Tag tag, string? date)
    {
        string? normalizedDate = NormalizeDate(date);
        if (normalizedDate is null)
        {
            return string.Empty;
        }

        if (SupportsFullDate(tag))
        {
            return normalizedDate;
        }

        return FormatYear(ExtractYear(normalizedDate)) ?? string.Empty;
    }

    public static bool SupportsFullDate(TagLib.Tag tag)
    {
        return tag switch
        {
            TagLib.Ogg.XiphComment => true,
            TagLib.Mpeg4.AppleTag => true,
            TagLib.Riff.InfoTag => true,
            TagLib.Ape.Tag => true,
            TagLib.Id3v2.Tag id3v2 => SupportsFullDateId3v2(id3v2),
            TagLib.Ogg.GroupedComment grouped => grouped.Comments.Any(c => c is not null),
            TagLib.CombinedTag combined => combined.Tags.Any(t => t is not null && SupportsFullDate(t)),
            _ => false
        };
    }

    internal static string? NormalizeDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return null;
        }

        string cleaned = date.Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Trim();
        return cleaned.Length == 0 ? null : cleaned;
    }

    internal static uint ExtractYear(string? date)
    {
        string? normalized = NormalizeDate(date);
        if (normalized is null || normalized.Length < 4)
        {
            return 0;
        }

        if (uint.TryParse(normalized[..4], NumberStyles.None, CultureInfo.InvariantCulture, out uint year)
            && year is >= 1000 and <= 9999)
        {
            return year;
        }

        return 0;
    }

    private static void Apply(TagLib.Tag tag, string? normalizedDate, uint year)
    {
        switch (tag)
        {
            case TagLib.Ogg.XiphComment xiph:
                if (normalizedDate is not null)
                {
                    xiph.SetField("DATE", normalizedDate);
                }
                else
                {
                    xiph.RemoveField("DATE");
                }
                break;

            case TagLib.Mpeg4.AppleTag apple:
                if (normalizedDate is not null)
                {
                    apple.SetText(AppleDateBoxType, normalizedDate);
                }
                else
                {
                    apple.ClearData(AppleDateBoxType);
                }
                break;

            case TagLib.Id3v2.Tag id3v2:
                id3v2.RemoveFrames(Id3v2DateFrameId);
                id3v2.RemoveFrames(Id3v2YearFrameId);
                id3v2.RemoveFrames(Id3v2DayMonthFrameId);

                if (SupportsFullDateId3v2(id3v2))
                {
                    if (normalizedDate is not null)
                    {
                        id3v2.SetTextFrame(Id3v2DateFrameId, normalizedDate);
                    }
                }
                else
                {
                    id3v2.Year = year;
                }
                break;

            case TagLib.Riff.InfoTag info:
                if (normalizedDate is not null)
                {
                    string[] values = [normalizedDate];
                    info.SetValue(RiffDateItemId, values);
                }
                else
                {
                    info.RemoveValue(RiffDateItemId);
                }
                break;

            case TagLib.Ape.Tag ape:
                if (normalizedDate is not null)
                {
                    ape.SetValue("Year", normalizedDate);
                }
                else
                {
                    ape.RemoveItem("Year");
                }
                break;

            case TagLib.Ogg.GroupedComment grouped:
                foreach (TagLib.Ogg.XiphComment comment in grouped.Comments)
                {
                    if (comment is null)
                    {
                        continue;
                    }

                    Apply(comment, normalizedDate, year);
                    if (normalizedDate is not null)
                    {
                        break;
                    }
                }
                break;

            case TagLib.CombinedTag combined:
                foreach (TagLib.Tag child in combined.Tags)
                {
                    if (child is not null)
                    {
                        Apply(child, normalizedDate, year);
                    }
                }
                break;

            default:
                tag.Year = year;
                break;
        }
    }

    private static string? ReadCombined(TagLib.CombinedTag combined)
    {
        TagLib.Tag[] childTags = combined.Tags;

        // Prefer full-date-capable child tags so a year-only sibling (e.g., ID3v1 or ID3v2.3)
        // does not shadow a full date stored in Xiph, Apple, RIFF INFO, ID3v2.4, or APEv2.
        foreach (TagLib.Tag child in childTags)
        {
            if (child is null || !SupportsFullDate(child))
            {
                continue;
            }

            string? value = Read(child);
            if (value is not null)
            {
                return value;
            }
        }

        foreach (TagLib.Tag child in childTags)
        {
            if (child is null || SupportsFullDate(child))
            {
                continue;
            }

            string? value = Read(child);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    private static bool SupportsFullDateId3v2(TagLib.Id3v2.Tag id3v2)
    {
        return id3v2.Version >= 4 || (id3v2.Flags & TagLib.Id3v2.HeaderFlags.FooterPresent) != 0;
    }

    private static string? FormatYear(uint year)
    {
        return year > 0 ? year.ToString(CultureInfo.InvariantCulture) : null;
    }
}
