namespace Construct.Application.Services
{
    public class CprjRaw
    {
        public string Name { get; set; } = "";
        public string Base64 { get; set; } = "";
        public long LastModified { get; set; } // Unix timestamp in ms
        public long Size { get; set; } // bytes


        public DateTime LastModifiedDateTime
        {
            get
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(LastModified).DateTime.ToLocalTime();
            }
        }
        public double SizeInMB
        {
            get
            {
                return Math.Round((double)Size / 1024 / 1024, 2);
            }
        }
        public double SizeInKB
        {
            get
            {
                return Math.Round((double)Size / 1024, 2);
            }
        }
    }
}