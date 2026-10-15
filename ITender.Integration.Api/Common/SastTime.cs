namespace ITender.Integration.Api.Common
{
    /// <summary>
    /// The API receives South African local date/time. Dataverse date-time columns (User Local behaviour)
    /// are stored in UTC, so values are converted before being written.
    /// TODO: if a column is "Time-Zone Independent", write the local value unchanged instead.
    /// </summary>
    public static class SastTime
    {
        private static readonly TimeZoneInfo Zone = Resolve();

        /// <summary>Values already marked UTC are left alone; everything else is treated as SAST.</summary>
        public static DateTime ToUtc(DateTime value)
            => value.Kind == DateTimeKind.Utc
                ? value
                : TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), Zone);

        private static TimeZoneInfo Resolve()
        {
            foreach (var id in new[] { "Africa/Johannesburg", "South Africa Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch (TimeZoneNotFoundException) { }
            }
            return TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "SAST", "SAST");
        }
    }
}