namespace Construct.WebUI.Server.Helpers
{

    public static class TimeAgoHelper
    {
        public static string GetTimeAgo(DateTime dateTime)
        {
            var now = DateTime.UtcNow;
            var ts = now - dateTime.ToUniversalTime();

            if (ts.TotalSeconds < 60)
                return "Zojuist";

            if (ts.TotalMinutes < 60)
                return $"{(int)ts.TotalMinutes} {(ts.TotalMinutes >= 2 ? "minuten" : "minuut")} geleden";

            if (ts.TotalHours < 24)
                return $"{(int)ts.TotalHours} uur geleden";

            if (ts.TotalDays < 2)
                return "Gisteren";

            if (ts.TotalDays < 7)
                return $"{(int)ts.TotalDays} {(ts.TotalDays >= 2 ? "dagen" : "dag")} geleden";

            if (ts.TotalDays < 30)
                return $"{(int)(ts.TotalDays / 7)} {(ts.TotalDays / 7 >= 2 ? "weken" : "week")} geleden";

            if (ts.TotalDays < 365)
                return $"{(int)(ts.TotalDays / 30)} {(ts.TotalDays / 30 >= 2 ? "maanden" : "maand")} geleden";

            return $"{(int)(ts.TotalDays / 365)} jaar geleden";
        }
    }

}
