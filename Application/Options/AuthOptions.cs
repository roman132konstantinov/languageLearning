namespace Application.Options
{
    public class AuthOptions
    {
        public const string SectionName = "Auth";

        public int MaxFailedAccessAttempts { get; set; } = 5;
        public int LockoutMinutes { get; set; } = 15;
        public int MinPasswordLength { get; set; } = 8;
        public bool RequireDigit { get; set; } = true;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireNonAlphanumeric { get; set; }
        public int PasswordHashIterations { get; set; } = 210000;
        public bool BootstrapFirstUserAsAdmin { get; set; } = true;
    }
}
