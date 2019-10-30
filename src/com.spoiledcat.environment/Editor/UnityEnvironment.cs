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
	using NiceIO;
	using Unity;

	public static class EnvironmentExtensions
	{
		public static IEnumerable<NPath> ToNPathList(this IEnvironment environment, string envPath)
		{
			return envPath
					  .Split(Path.PathSeparator)
					  .Where(x => x != null)
					  .Select(x => environment.ExpandEnvironmentVariables(x.Trim('"', '\'')))
					  .Where(x => !String.IsNullOrEmpty(x))
					  .Select(x => x.ToNPath());
		}
	}
}

namespace SpoiledCat.Unity
{
	using Logging;
	using NiceIO;

	public interface IEnvironment
	{
		IEnvironment Initialize(NPath assetsPath, NPath extensionInstallPath, string unityVersion = null, NPath unityApplicationPath = default, NPath unityApplicationContentsPath = default);
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
		NPath UnityApplication { get; }
		NPath UnityApplicationContents { get; }
		NPath UnityAssetsPath { get; }
		NPath UnityProjectPath { get; }
		NPath ExtensionInstallPath { get; }
		NPath UserCachePath { get; set; }
		NPath SystemCachePath { get; set; }
		NPath LogPath { get; }
		ISettings LocalSettings { get; }
		ISettings SystemSettings { get; }
		ISettings UserSettings { get; }
		string ApplicationName { get; }
		NPath WorkingDirectory { get; }
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

			UserCachePath = NPath.LocalAppData.Combine(applicationName);
			SystemCachePath = NPath.CommonAppData.Combine(applicationName);
			LogPath = IsMac ? NPath.HomeDirectory.Combine("Library/Logs").Combine(applicationName) : UserCachePath;
			LogPath = LogPath.Combine(logFile);
			LogPath.EnsureParentDirectoryExists();
		}

		public virtual IEnvironment Initialize(
			NPath Application_dataPath,
			NPath extensionInstallPath,
			string unityVersion = null,
			NPath EditorApplication_applicationPath = default,
			NPath EditorApplication_applicationContentsPath = default
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
			WorkingDirectory = NPath.CurrentDirectory;

			return this;
		}

		public void SetWorkingDirectory(NPath workingDirectory)
		{
			WorkingDirectory = workingDirectory;
		}

		public string GetSpecialFolder(Environment.SpecialFolder folder) => NPath.FileSystem.GetFolderPath(folder);

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
		public string UnityVersion { get; set; }
		public NPath UnityApplication { get; set; }
		public NPath UnityApplicationContents { get; set; }
		public NPath UnityAssetsPath { get; set; }
		public NPath UnityProjectPath { get; set; }
		public NPath ExtensionInstallPath { get; set; }
		public NPath UserCachePath { get; set; }
		public NPath SystemCachePath { get; set; }
		public NPath WorkingDirectory { get; private set; }

		public string Path { get; set; } = Environment.GetEnvironmentVariable(GetEnvironmentVariableKeyInternal("PATH"));

		public string NewLine => Environment.NewLine;

		public ISettings LocalSettings { get; protected set; }
		public ISettings SystemSettings { get; protected set; }
		public ISettings UserSettings { get; protected set; }

		public bool IsWindows => OnWindows;
		public bool IsLinux => OnLinux;
		public bool IsMac => OnMac;
		public bool Is32Bit => IntPtr.Size == 4;

		public static bool OnWindows => NPath.IsWindows;

		public static bool OnLinux => NPath.IsLinux;
		public static bool OnMac => NPath.IsMac;

		public static string ExecutableExtension => OnWindows ? ".exe" : string.Empty;
		public static ILogging Logger { get; } = LogHelper.GetLogger<UnityEnvironment>();
	}
}
