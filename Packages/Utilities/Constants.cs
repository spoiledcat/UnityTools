namespace SpoiledCat.Utilities
{
    public static class Constants
    {
        public const string GuidKey = "Guid";
        public const string MetricsKey = "MetricsEnabled";
        public const string UsageFile = "metrics.json";
        public const string GitInstallPathKey = "GitInstallPath";
        public const string TraceLoggingKey = "EnableTraceLogging";
        public const string WebTimeoutKey = "WebTimeout";
        public const string GitTimeoutKey = "GitTimeout";
        public const string SkipVersionKey = "SkipVersion";
        public const string GitInstallationState = "GitInstallationState";

        public static readonly TheVersion MinimumGitVersion = TheVersion.Parse("2.0");
        public static readonly TheVersion MinimumGitLfsVersion = TheVersion.Parse("2.0");
        public static readonly TheVersion DesiredGitVersion = TheVersion.Parse("2.11");
        public static readonly TheVersion DesiredGitLfsVersion = TheVersion.Parse("2.4");
    }
}
