namespace Application.Options
{
    public class RateLimitingOptions
    {
        public const string SectionName = "RateLimiting";

        public int PermitLimit { get; set; } = 100;
        public int WindowSeconds { get; set; } = 60;
        public int QueueLimit { get; set; } = 20;
        public int AuthPermitLimit { get; set; } = 10;
        public int AuthWindowSeconds { get; set; } = 60;
    }
}
