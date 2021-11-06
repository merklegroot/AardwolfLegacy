using System;

namespace date_time_lib
{
    public static class DateTimeUtil
    {
        public static DateTime? UnixTimeStampToDateTime(decimal unixTimeStamp)
        {
            return UnixTimeStampToDateTime((double)unixTimeStamp);
        }

        public static DateTime? UnixTimeStampToDateTime(long unixTimeStamp)
        {
            return UnixTimeStampToDateTime((double)unixTimeStamp);
        }

        public static DateTime? UnixTimeStampToDateTime(double unixTimeStamp)
        {
            if (unixTimeStamp <= 0) { return null; }

            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        public static DateTime? UnixTimeStamp13DigitToDateTime(decimal unixTimeStamp)
        {
            return UnixTimeStamp13DigitToDateTime((double)unixTimeStamp);
        }

        public static DateTime? UnixTimeStamp13DigitToDateTime(long unixTimeStamp)
        {
            return UnixTimeStamp13DigitToDateTime((double)unixTimeStamp);
        }

        public static DateTime? UnixTimeStamp13DigitToDateTime(double unixTimeStamp)
        {
            if (unixTimeStamp <= 0) { return null; }

            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp);
            return dtDateTime;
        }

        public static long GetUnixTimeStamp()
        {
            return GetUnixTimeStamp(DateTime.UtcNow);
        }

        public static long GetUnixTimeStamp(DateTime utcTime)
        {
            return (long)(utcTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static long GetUnixMillisecondsTimeStamp()
        {
            var utcTime = DateTime.UtcNow;
            return (long)(utcTime.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds);
        }

        public static long GetUnixTimeStamp13Digit()
        {
            return GetUnixTimeStamp13Digit(DateTime.UtcNow);
        }

        public static long GetUnixTimeStamp13Digit(DateTime utcNow)
        {
            var totalSeconds = (utcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return (long)(totalSeconds * 1000);
        }
    }
}
