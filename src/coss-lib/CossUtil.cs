using System;

namespace coss_model
{
    public class CossUtil
    {
        public static DateTime? UnixTimeStampToDateTime(double unixTimeStamp)
        {
            if (unixTimeStamp <= 0) { return null; }

            var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp / 1000);//.ToLocalTime();
            return dtDateTime;
        }
    }
}
