using System;

namespace Backtrace.Unity.Common
{
    public static class DateTimeExtensions
    {

        public static int Timestamp(this DateTime dateTime)
        {
            return (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }

}