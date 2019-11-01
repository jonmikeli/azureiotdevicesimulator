using System;

namespace IoT.Simulator.Extensions
{
    public static class DateTimeExtensions
    {
        public static long TimeStamp(this DateTime data)
        {
            return (long)(data.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }
}
