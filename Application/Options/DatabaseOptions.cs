namespace Application.Options
{
    public class DatabaseOptions
    {
        public const string SectionName = "Database";

        public bool ApplyMigrationsOnStartup { get; set; }
    }
}
