namespace Wisp;

[DebuggerDisplay("{ToString(),nq}")]
[CosPrimitive]
public sealed partial class CosDate(DateTimeOffset value) : ICosPrimitive
{
    public DateTimeOffset Value { get; } = value;

    public static bool TryParse(ReadOnlySpan<char> input, [NotNullWhen(true)] out DateTimeOffset? time)
    {
        static bool TryParseNonNegativeIntegerAndSlice(ref ReadOnlySpan<char> input, int length, ref uint value)
        {
            if (input.Length < length)
                return false;

            if (uint.TryParse(input.Slice(0, length), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                input = input.Slice(length);
                return true;
            }
            else return false;
        }

        time = null;

        // PDF 32000-1:2008: The prefix D: shall be present..
        if (!input.StartsWith("D:", StringComparison.Ordinal))
            return false;

        input = input.Slice(2);

        uint year = 0;

        // .. the year field (YYYY) shall be present..
        if (!TryParseNonNegativeIntegerAndSlice(ref input, 4, ref year) || year == 0) return false;

        // .. and all other fields may be present but only if all of their preceding fields are also present.
        uint month = 1, day = 1, hour = 0, minute = 0, second = 0;

        // If no UT information is specified, the relationship of the specified time to
        // UT shall be considered to be GMT.
        TimeSpan offset = TimeSpan.Zero;

        if (!TryParseNonNegativeIntegerAndSlice(ref input, 2, ref month) || month == 0 || month > 12 ||
            !TryParseNonNegativeIntegerAndSlice(ref input, 2, ref day) || day == 0 || day > DateTime.DaysInMonth((int)year, (int)month) ||
            !TryParseNonNegativeIntegerAndSlice(ref input, 2, ref hour) || hour >= 24 ||
            !TryParseNonNegativeIntegerAndSlice(ref input, 2, ref minute) || minute >= 60 ||
            !TryParseNonNegativeIntegerAndSlice(ref input, 2, ref second) || second >= 60)
        {
            // TODO: Check if we can restructure this control flow. "goto considered harmful" meme.
            goto Done;
        }

        if (!input.IsEmpty)
        {
            char tzSign = input[0];
            input = input.Slice(1);

            if (tzSign is '+' or '-')
            {
                uint offsetHour = 0, offsetMinute = 0;

                // The offset hour is bound to [0, 14] in DateTimeOffset
                if (!TryParseNonNegativeIntegerAndSlice(ref input, 2, ref offsetHour)
                    || offsetHour > 14)
                {
                    return false;
                }

                // .. The APOSTROPHE following the hour offset field (HH) shall only be present if the HH field is present.
                if (!input.IsEmpty && input[0] == '\'')
                {
                    input = input.Slice(1);

                    // The minute offset field (mm) shall only be present if the
                    // APOSTROPHE following the hour offset field(HH) is present.
                    if (!TryParseNonNegativeIntegerAndSlice(ref input, 2, ref offsetMinute) ||
                        offsetMinute >= 60)
                    {
                        return false;
                    }
                }

                offset = new TimeSpan((int)offsetHour, (int)offsetMinute, seconds: 0);

                if (tzSign == '-') offset = -offset;
            }
        }

    Done:
        time = new DateTimeOffset((int)year, (int)month, (int)day, (int)hour, (int)minute, (int)second, offset);
        return true;
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"[ObjectID] {Value:yyyyMMddHHmmsszzz}");
}