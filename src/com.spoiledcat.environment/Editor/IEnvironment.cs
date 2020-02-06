namespace SpoiledCat.Unity
{
	using SimpleIO;

	public enum Folders
	{
		LocalApplicationData,
		CommonApplicationData,
		Logs
	}

	public interface IEnvironment
	{
		IEnvironment Initialize(string projectPath,
            string extensionInstallPath,
			string unityVersion = null,
			string EditorApplication_applicationPath = default,
			string EditorApplication_applicationContentsPath = default);

		string ExpandEnvironmentVariables(string name);
		string GetEnvironmentVariable(string v);
		string GetEnvironmentVariableKey(string name);

		string Path { get; set; }
		string NewLine { get; }
		string ExecutableExtension { get; }
		bool IsWindows { get; }
		bool IsLinux { get; }
		bool IsMac { get; }
		bool Is32Bit { get; }
		string UnityVersion { get; }
		SPath UnityApplication { get; }
		SPath UnityApplicationContents { get; }
		SPath UnityProjectPath { get; }
		SPath UserCachePath { get; set; }
		SPath SystemCachePath { get; set; }
		SPath LogPath { get; }
		SPath ExtensionInstallPath { get; }
		ISettings LocalSettings { get; }
		ISettings SystemSettings { get; }
		ISettings UserSettings { get; }
		string ApplicationName { get; }
	}
}
