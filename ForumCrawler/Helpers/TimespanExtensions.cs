using System;

namespace ForumCrawler
{
    public static class TimespanExtensions
    {
        public static string ToHumanReadableString(this TimeSpan t)
        {
            if (t.TotalSeconds <= 1)
            {
                return $@"{t:s\.ff} seconds";
            }
            if (t.TotalMinutes <= 1)
            {
                return $@"{Math.Ceiling(t.TotalSeconds)} seconds";
            }
            if (t.TotalHours <= 1)
            {
                return $@"{Math.Ceiling(t.TotalMinutes)} minutes";
            }
            if (t.TotalDays <= 1)
            {
                return $@"{Math.Ceiling(t.TotalHours)} hours";
            }
            return $@"{Math.Ceiling(t.TotalDays)} days";
        }
    }
}