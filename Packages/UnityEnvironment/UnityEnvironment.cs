using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SpoiledCat.Unity
{
	using Logging;
	using NiceIO;
	public interface IEnvironment
	{
		void Initialize(string unityVersion, NPath extensionInstallPath, NPath unityApplicationPath, NPath unityApplicationContentsPath, NPath assetsPath);
		string ExpandEnvironmentVariables(string name);
		string GetEnvironmentVariable(string v);
		string GetSpecialFolder(Environment.SpecialFolder folder);

		string Path { get; set; }
		string NewLine { get; }
		bool IsWindows { get; }
		bool IsLinux { get; }
		bool IsMac { get; }
		string UnityVersion { get; }
		NPath UnityApplication { get; }
		NPath UnityApplicationContents { get; }
		NPath UnityAssetsPath { get; }
		NPath UnityProjectPath { get; }
		NPath ExtensionInstallPath { get; }
		NPath UserCachePath { get; set; }
		NPath SystemCachePath { get; set; }
		NPath LogPath { get; }
		IFileSystem FileSystem { get; set; }
		ISettings LocalSettings { get; }
		ISettings SystemSettings { get; }
		ISettings UserSettings { get; }
		string ApplicationName { get; }
		string GetEnvironmentVariableKey(string name);
	}

    public class UnityEnvironment : IEnvironment
    {
	    private const string DefaultLogFilename = "github-unity.log";
        private static bool? onWindows;
        private static bool? onLinux;
        private static bool? onMac;

        public UnityEnvironment(string applicationName, string logFile = DefaultLogFilename)
        {
	        ApplicationName = applicationName;
	        NPath localAppData;
            NPath commonAppData;
            if (IsWindows)
            {
                localAppData = GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData).ToNPath();
                commonAppData = GetSpecialFolder(Environment.SpecialFolder.CommonApplicationData).ToNPath();
            }
            else if (IsMac)
            {
                localAppData = NPath.HomeDirectory.Combine("Library", "Application Support");
                // there is no such thing on the mac that is guaranteed to be user accessible (/usr/local might not be)
                commonAppData = GetSpecialFolder(Environment.SpecialFolder.ApplicationData).ToNPath();
            }
            else
            {
                localAppData = GetSpecialFolder(Environment.SpecialFolder.LocalApplicationData).ToNPath();
                commonAppData = GetSpecialFolder(Environment.SpecialFolder.ApplicationData).ToNPath();
            }

            UserCachePath = localAppData.Combine(applicationName);
            SystemCachePath = commonAppData.Combine(applicationName);
            LogPath = IsMac ? NPath.HomeDirectory.Combine("Library/Logs").Combine(applicationName) : UserCachePath;
            LogPath = LogPath.Combine(logFile);
            LogPath.EnsureParentDirectoryExists();
        }

        /// <summary>
        /// This is for tests to reset the static OS flags
        /// </summary>
        public static void Reset()
        {
            onWindows = null;
            onLinux = null;
            onMac = null;
        }

        public void Initialize(string unityVersion,
	        NPath extensionInstallPath,
			NPath EditorApplication_applicationPath,
			NPath EditorApplication_applicationContentsPath,
			NPath Application_dataPath)
        {
            ExtensionInstallPath = extensionInstallPath;
            UnityApplication = EditorApplication_applicationPath;
            UnityApplicationContents = EditorApplication_applicationContentsPath;
            UnityAssetsPath = Application_dataPath;
            UnityProjectPath = UnityAssetsPath.Parent;
            UnityVersion = unityVersion;
            UserSettings = new UserSettings(this);
            LocalSettings = new LocalSettings(this);
            SystemSettings = new SystemSettings(this);
        }

        public string GetSpecialFolder(Environment.SpecialFolder folder)
        {
            return Environment.GetFolderPath(folder);
        }

        public string ExpandEnvironmentVariables(string name)
        {
            var key = GetEnvironmentVariableKey(name);
            return Environment.ExpandEnvironmentVariables(key);
        }

        public string GetEnvironmentVariable(string name)
        {
            var key = GetEnvironmentVariableKey(name);
            return Environment.GetEnvironmentVariable(key);
        }

        public string GetEnvironmentVariableKey(string name)
        {
            return GetEnvironmentVariableKeyInternal(name);
        }

        private static string GetEnvironmentVariableKeyInternal(string name)
        {
            return Environment.GetEnvironmentVariables().Keys.Cast<string>()
                                 .FirstOrDefault(k => string.Compare(name, k, true, CultureInfo.InvariantCulture) == 0) ?? name;
        }

        public string ApplicationName { get; }
        public NPath LogPath { get; }
        public IFileSystem FileSystem { get => NPath.FileSystem; set => NPath.FileSystem = value; }
        public string UnityVersion { get; set; }
        public NPath UnityApplication { get; set; }
        public NPath UnityApplicationContents { get; set; }
        public NPath UnityAssetsPath { get; set; }
        public NPath UnityProjectPath { get; set; }
        public NPath ExtensionInstallPath { get; set; }
        public NPath UserCachePath { get; set; }
        public NPath SystemCachePath { get; set; }
        public string Path { get; set; } = Environment.GetEnvironmentVariable(GetEnvironmentVariableKeyInternal("PATH"));

        public string NewLine => Environment.NewLine;

        public ISettings LocalSettings { get; protected set; }
        public ISettings SystemSettings { get; protected set; }
        public ISettings UserSettings { get; protected set; }

        public bool IsWindows => OnWindows;
        public bool IsLinux => OnLinux;
        public bool IsMac => OnMac;

        public static bool OnWindows
        {
            get
            {
                if (onWindows.HasValue)
                    return onWindows.Value;
                return Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX;
            }
            set => onWindows = value;
        }

        public static bool OnLinux
        {
            get
            {
                if (onLinux.HasValue)
                    return onLinux.Value;
                return Environment.OSVersion.Platform == PlatformID.Unix && Directory.Exists("/proc");
            }
            set => onLinux = value;
        }

        public static bool OnMac
        {
            get
            {
                if (onMac.HasValue)
                    return onMac.Value;
                // most likely it'll return the proper id but just to be on the safe side, have a fallback
                return Environment.OSVersion.Platform == PlatformID.MacOSX ||
                      (Environment.OSVersion.Platform == PlatformID.Unix && !Directory.Exists("/proc"));
            }
            set => onMac = value;
        }

        public static string ExecutableExtension => OnWindows ? ".exe" : string.Empty;
        public static ILogging Logger { get; } = LogHelper.GetLogger<UnityEnvironment>();
    }
}
