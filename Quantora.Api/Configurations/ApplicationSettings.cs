namespace Quantora.Api.Configurations
{
    public sealed class ApplicationSettings
    {
        public const string SectionName = "Application";

        public string Name { get; set; } = "Quantora";

        public string Version { get; set; } = "1.0.0";
    }
}
