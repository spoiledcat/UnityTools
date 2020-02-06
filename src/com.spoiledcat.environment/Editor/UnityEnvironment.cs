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
	using SimpleIO;

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

			LocalAppData = GetFolder(Folders.LocalApplicationData);
			CommonAppData = GetFolder(Folders.CommonApplicationData);
			UserCachePath = LocalAppData.Combine(ApplicationName);
			SystemCachePath = CommonAppData.Combine(ApplicationName);
			LogPath = GetFolder(Folders.Logs).Combine(ApplicationName, logFile);
		}

		public virtual IEnvironment Initialize(
			string projectPath,
			string extensionInstallPath,
			string unityVersion = null,
			string EditorApplication_applicationPath = default,
			string EditorApplication_applicationContentsPath = default
		)
		{
			UserCachePath.EnsureDirectoryExists();
			SystemCachePath.EnsureDirectoryExists();
			LogPath.EnsureDirectoryExists();

			UnityProjectPath = projectPath.ToSPath();
			ExtensionInstallPath = extensionInstallPath.ToSPath();
			UnityVersion = unityVersion;
			if (!string.IsNullOrEmpty(EditorApplication_applicationPath))
				UnityApplication = EditorApplication_applicationPath.ToSPath();
			if (!string.IsNullOrEmpty(EditorApplication_applicationContentsPath))
				UnityApplicationContents = EditorApplication_applicationContentsPath.ToSPath();

			UserSettings = new UserSettings(this);
			LocalSettings = new LocalSettings(this);
			SystemSettings = new SystemSettings(this);

			return this;
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

		public SPath GetFolder(Folders folder)
		{
			switch (folder)
			{
				case Folders.LocalApplicationData:
				{
					if (IsMac)
						return SPath.HomeDirectory.Combine("Library", "Application Support");
				}
					break;
				case Folders.CommonApplicationData:
				{
					return Environment.GetFolderPath(
						IsLinux
							? Environment.SpecialFolder.ApplicationData
							: Environment.SpecialFolder.CommonApplicationData
					).ToSPath();
				}
				case Folders.Logs:
				{
					if (IsMac)
						return SPath.HomeDirectory.Combine("Library/Logs");
				}
					break;
			}
			return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData).ToSPath();
		}

		public string ApplicationName { get; }
		public SPath LocalAppData { get; set; }
		public SPath CommonAppData { get; set; }
		public SPath LogPath { get; }

		public string UnityVersion { get; set; }
		public SPath UnityApplication { get; set; }
		public SPath UnityApplicationContents { get; set; }
		public SPath UnityProjectPath { get; set; }
		public SPath UserCachePath { get; set; }
		public SPath SystemCachePath { get; set; }
		public SPath ExtensionInstallPath { get; set;  }

		public string Path { get; set; } = Environment.GetEnvironmentVariable(GetEnvironmentVariableKeyInternal("PATH"));

		public string NewLine => Environment.NewLine;

		public ISettings LocalSettings { get; protected set; }
		public ISettings SystemSettings { get; protected set; }
		public ISettings UserSettings { get; protected set; }

		public bool Is32Bit => IntPtr.Size == 4;

		public string ExecutableExtension => IsWindows ? ".exe" : string.Empty;

		private bool? isLinux;
		private bool? isMac;
		private bool? isWindows;
		public bool IsWindows
		{
			get
			{
				if (isWindows.HasValue)
					return isWindows.Value;
				return Environment.OSVersion.Platform != PlatformID.Unix && Environment.OSVersion.Platform != PlatformID.MacOSX;
			}
			set => isWindows = value;
		}

		public bool IsLinux
		{
			get
			{
				if (isLinux.HasValue)
					return isLinux.Value;
				return Environment.OSVersion.Platform == PlatformID.Unix && Directory.Exists("/proc");
			}
			set => isLinux = value;
		}

		public bool IsMac
		{
			get
			{
				if (isMac.HasValue)
					return isMac.Value;
				// most likely it'll return the proper id but just to be on the safe side, have a fallback
				return Environment.OSVersion.Platform == PlatformID.MacOSX ||
						(Environment.OSVersion.Platform == PlatformID.Unix && !Directory.Exists("/proc"));
			}
			set => isMac = value;
		}

	}
}
