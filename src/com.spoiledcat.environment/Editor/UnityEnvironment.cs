// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace SpoiledCat.Extensions
{
	using SimpleIO;
	using Unity;

	public static class EnvironmentExtensions
	{
		public static IEnumerable<SPath> ToSPathList(this IEnvironment environment, string envPath)
		{
			return envPath
					  .Split(Path.PathSeparator)
					  .Where(x => x != null)
					  .Select(x => environment.ExpandEnvironmentVariables(x.Trim('"', '\'')))
					  .Where(x => !String.IsNullOrEmpty(x))
					  .Select(x => x.ToSPath());
		}
	}
}

namespace SpoiledCat.Unity
{
	using Logging;
	using SimpleIO;

	public interface IEnvironment
	{
		IEnvironment Initialize(SPath assetsPath, SPath extensionInstallPath, string unityVersion = null, SPath unityApplicationPath = default, SPath unityApplicationContentsPath = default);
		string ExpandEnvironmentVariables(string name);
		string GetEnvironmentVariable(string v);
		string GetSpecialFolder(Environment.SpecialFolder folder);
		string GetEnvironmentVariableKey(string name);

		string Path { get; set; }
		string NewLine { get; }
		bool IsWindows { get; }
		bool IsLinux { get; }
		bool IsMac { get; }
		bool Is32Bit { get; }
		string UnityVersion { get; }
		SPath UnityApplication { get; }
		SPath UnityApplicationContents { get; }
		SPath UnityAssetsPath { get; }
		SPath UnityProjectPath { get; }
		SPath ExtensionInstallPath { get; }
		SPath UserCachePath { get; set; }
		SPath SystemCachePath { get; set; }
		SPath LogPath { get; }
		ISettings LocalSettings { get; }
		ISettings SystemSettings { get; }
		ISettings UserSettings { get; }
		string ApplicationName { get; }
		SPath WorkingDirectory { get; }
	}

	public class UnityEnvironment : IEnvironment
	{
#if UNITY_EDITOR
		private const string DefaultLogFilename = "editor.log";
#else
		private const string DefaultLogFilename = "player.log";
#endif

		public UnityEnvironment(string applicationName, string logFile = DefaultLogFilename)
		{
			ApplicationName = applicationName;

			UserCachePath = SPath.LocalAppData.Combine(applicationName);
			SystemCachePath = SPath.CommonAppData.Combine(applicationName);
			LogPath = IsMac ? SPath.HomeDirectory.Combine("Library/Logs").Combine(applicationName) : UserCachePath;
			LogPath = LogPath.Combine(logFile);
			LogPath.EnsureParentDirectoryExists();
		}

		public virtual IEnvironment Initialize(
			SPath Application_dataPath,
			SPath extensionInstallPath,
			string unityVersion = null,
			SPath EditorApplication_applicationPath = default,
			SPath EditorApplication_applicationContentsPath = default
		)
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
			WorkingDirectory = SPath.CurrentDirectory;

			return this;
		}

		public void SetWorkingDirectory(SPath workingDirectory)
		{
			WorkingDirectory = workingDirectory;
		}

		public string GetSpecialFolder(Environment.SpecialFolder folder) => SPath.FileSystem.GetFolderPath(folder);

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
		public SPath LogPath { get; }
		public string UnityVersion { get; set; }
		public SPath UnityApplication { get; set; }
		public SPath UnityApplicationContents { get; set; }
		public SPath UnityAssetsPath { get; set; }
		public SPath UnityProjectPath { get; set; }
		public SPath ExtensionInstallPath { get; set; }
		public SPath UserCachePath { get; set; }
		public SPath SystemCachePath { get; set; }
		public SPath WorkingDirectory { get; private set; }

		public string Path { get; set; } = Environment.GetEnvironmentVariable(GetEnvironmentVariableKeyInternal("PATH"));

		public string NewLine => Environment.NewLine;

		public ISettings LocalSettings { get; protected set; }
		public ISettings SystemSettings { get; protected set; }
		public ISettings UserSettings { get; protected set; }

		public bool IsWindows => OnWindows;
		public bool IsLinux => OnLinux;
		public bool IsMac => OnMac;
		public bool Is32Bit => IntPtr.Size == 4;

		public static bool OnWindows => SPath.IsWindows;

		public static bool OnLinux => SPath.IsLinux;
		public static bool OnMac => SPath.IsMac;

		public static string ExecutableExtension => OnWindows ? ".exe" : string.Empty;
		public static ILogging Logger { get; } = LogHelper.GetLogger<UnityEnvironment>();
	}
}
