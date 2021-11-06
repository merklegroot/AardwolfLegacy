using System;

namespace date_time_lib
{
    public static class TimeSpanExtensions
    {
        public static string ToReadableValue(this TimeSpan timeSpan)
        {
            return $"{timeSpan.Hours}h {timeSpan.Minutes}m {timeSpan.Seconds}s";
        }
    }
}
