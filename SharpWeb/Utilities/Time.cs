using System;

namespace SharpWeb.Utilities
{
    public static class TypeUtil
    {
        public static DateTime TimeStamp(long stamp)
        {
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timestampDateTime = unixEpoch.AddSeconds(stamp).ToLocalTime();

            if (timestampDateTime.Year > 9999)
            {
                return new DateTime(9999, 12, 13, 23, 59, 59, DateTimeKind.Local);
            }

            return timestampDateTime;
        }

        public static DateTime TimeEpoch(long epoch)
        {
            var maxTime = 99633311740000000L;

            if (epoch > maxTime)
            {
                return new DateTime(2049, 1, 1, 1, 1, 1, DateTimeKind.Local);
            }

            var epochTicks = epoch * 10; // Convert to ticks
            var epochDateTime = new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(epochTicks).ToLocalTime();

            return epochDateTime;
        }
    }
}
