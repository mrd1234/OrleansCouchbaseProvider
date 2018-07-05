namespace CouchBaseDocumentExpiry.DocumentExpiry
{
    using System;
    using System.Text;

    public static class TimespanExtensions
    {
        public static string ToLongString(this TimeSpan timeSpan)
        {
            var result = new StringBuilder();
            if (timeSpan.Days > 0) result.Append($"{timeSpan.Days} day{(timeSpan.Days != 1 ? "s" : "")} ");
            if (timeSpan.Hours > 0) result.Append($"{timeSpan.Hours} hour{(timeSpan.Hours != 1 ? "s" : "")} ");
            if (timeSpan.Minutes > 0) result.Append($"{timeSpan.Minutes} minute{(timeSpan.Minutes != 1 ? "s" : "")} ");
            if (timeSpan.Seconds > 0) result.Append($"{timeSpan.Seconds} second{(timeSpan.Seconds != 1 ? "s" : "")} ");
            return result.ToString().Trim();
        }
    }
}