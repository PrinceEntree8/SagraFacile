namespace SagraFacile.Web.Extensions;

public static class TimeStampFormatExtensions
{
    extension(TimeSpan timeSpan) {
        public string FormatTimeSpan()
        {
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m";
            return $"{(int)timeSpan.TotalMinutes}m";
        }
    }
}