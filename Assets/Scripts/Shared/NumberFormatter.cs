public static class NumberFormatter
{
    private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };

    /// <summary>
    /// Formats a number with magnitude suffix.
    /// Examples: 999 -> "999", 1500 -> "1.5K", 2500000 -> "2.5M"
    /// </summary>
    public static string Format(double value)
    {
        if (value < 0) return "-" + Format(-value);
        if (value < 1000) return value.ToString("F0");

        int magnitude = 0;
        double display = value;
        while (display >= 1000 && magnitude < Suffixes.Length - 1)
        {
            display /= 1000;
            magnitude++;
        }

        // Show 1 decimal place for values under 100 at current magnitude
        if (display < 100)
            return display.ToString("F1") + Suffixes[magnitude];
        return display.ToString("F0") + Suffixes[magnitude];
    }
}
