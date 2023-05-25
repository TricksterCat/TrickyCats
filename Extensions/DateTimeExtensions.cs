using System;

namespace GameRules.Scripts.Extensions
{
    public static class DateTimeExtensions
    {
        public static int TotalDay(this DateTime time)
        {
            return (int)(time - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;
        }
        
        public static int TotalHours(this DateTime time)
        {
            return (int)(time - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalHours;
        }
        
        public static DateTime FromHours(int hours)
        {
            return new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(hours);
        }
    }
}