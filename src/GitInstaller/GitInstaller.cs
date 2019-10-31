using System;
using System.Threading;

namespace SpoiledCat.Git
{
	using Logging;
	using NiceIO;
	using Threading;
	using Utilities;
	using ProcessManager;
	using Unity;
	using System.Collections.Generic;

	public class GitInstaller : TaskBase<GitInstaller.GitInstallationState>
	{
		private readonly IGitEnvironment environment;
		private readonly IProcessManager processManager;
		private GitInstallationState state;
		private readonly GitInstallDetails installDetails;
		private readonly IZipHelper sharpZipLibHelper;
		private Dictionary<string, TaskData> tasks = new Dictionary<string, TaskData>();

		private ProgressReporter progressReporter = new ProgressReporter();

		public GitInstaller(IGitEnvironment environment, IProcessManager processManager,
			 CancellationToken token,
			 GitInstallationState state = null,
			 GitInstallDetails installDetails = null)
			: base(token)
		{
			this.environment = environment;
			this.processManager = processManager;
			this.state = state;
			this.sharpZipLibHelper = ZipHelper.Instance;
			this.installDetails = installDetails ?? new GitInstallDetails(environment.UserCachePath, environment);
			progressReporter.OnProgress += progress.UpdateProgress;
		}

		protected GitInstallationState BaseRun(bool success)
		{
			return base.RunWithReturn(success);
		}

		protected override GitInstallationState RunWithReturn(bool success)
		{
			var ret = BaseRun(success);
			try
			{
				ret = SetupGitIfNeeded();
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					ThrownException.Rethrow();
			}
			return ret;
		}

		private GitInstallationState SetupGitIfNeeded()
		{
			UpdateTask("Setting up git...", 100);

			try
			{
				var skipSystemProbing = state != null;

				state = VerifyGitSettings(state);

				if (state.GitIsValid && state.GitLfsIsValid)
				{
					Logger.Trace("Using git install path from settings: {0}", state.GitExecutablePath);
					state.GitLastCheckTime = DateTimeOffset.Now;
					return state;
				}

				if (!skipSystemProbing)
				{
					if (environment.IsMac)
						state = FindGit(state);
				}

				state = SetDefaultPaths(state);
				state = CheckForGitUpdates(state);

				if (state.GitIsValid && state.GitLfsIsValid)
				{
					state.GitLastCheckTime = DateTimeOffset.Now;
					return state;
				}

				state = VerifyZipFiles(state);

				// on developer builds, prefer local zips over downloading
#if DEVELOPER_BUILD
				state = GrabZipFromResourcesIfNeeded(state);
				state = GetZipsIfNeeded(state);
#else
				state = GetZipsIfNeeded(state);
				state = GrabZipFromResourcesIfNeeded(state);
#endif
				state = ExtractGit(state);

				// if installing from zip failed (internet down maybe?), try to find a usable system git
				if (!state.GitIsValid && state.GitInstallationPath == installDetails.GitInstallationPath)
					state = FindGit(state);
				if (!state.GitLfsIsValid && state.GitLfsInstallationPath == installDetails.GitInstallationPath)
					state = FindGitLfs(state);
				state.GitLastCheckTime = DateTimeOffset.Now;

				return state;

			}
			finally
			{
				UpdateTask("Setting up git...", 100);
			}
		}

		private void UpdateTask(string name, long value)
		{
			TaskData task = null;
			if (!tasks.TryGetValue(name, out task))
			{
				task = new TaskData(name, value);
				tasks.Add(name, task);
			}
			else
				task.UpdateProgress(value, task.progress.Total);
			progressReporter.UpdateProgress(task.progress);
		}

		public GitInstallationState VerifyGitSettings(GitInstallationState state = null)
		{
			UpdateTask("Verifying git settings", 100);

			try
			{
				state = state ?? new GitInstallationState(environment);
				if (!state.GitExecutablePath.IsInitialized)
					return state;

				state = ValidateGitVersion(state);
				if (state.GitIsValid)
					state.GitInstallationPath = state.GitExecutablePath.Parent.Parent;

				state = ValidateGitLfsVersion(state);

				if (state.GitLfsIsValid && state.GitInstallationPath != installDetails.GitInstallationPath)
					state.GitLfsInstallationPath = state.GitLfsExecutablePath.Parent;

				return state;
			}
			finally
			{
				UpdateTask("Verifying git settings", 100);
			}
		}

		public GitInstallationState FindSystemGit(GitInstallationState state)
		{
			state = FindGit(state);
			state = FindGitLfs(state);
			return state;
		}

		private GitInstallationState FindGit(GitInstallationState state)
		{
			if (!state.GitIsValid)
			{
				var gitPath = new FindExecTask("git", environment, Token)
					 .Configure(processManager)
					 .Progress(progressReporter.UpdateProgress)
					 .Catch(e => true)
					 .RunSynchronously();
				state.GitExecutablePath = gitPath;
				state = ValidateGitVersion(state);
				if (state.GitIsValid)
					state.GitInstallationPath = gitPath.Parent.Parent;
			}
			return state;
		}

		private GitInstallationState FindGitLfs(GitInstallationState state)
		{
			if (!state.GitLfsIsValid)
			{
				var gitLfsPath = new FindExecTask("git-lfs", environment, Token)
					.Configure(processManager)
					.Progress(progressReporter.UpdateProgress)
					.Catch(e => true)
					.RunSynchronously();
				state.GitLfsExecutablePath = gitLfsPath;
				state = ValidateGitLfsVersion(state);
				if (state.GitLfsIsValid)
					state.GitLfsInstallationPath = state.GitLfsExecutablePath.Parent;
			}
			return state;
		}

		public GitInstallationState SetDefaultPaths(GitInstallationState state)
		{
			if (!state.GitIsValid && environment.IsWindows)
			{
				state.GitInstallationPath = installDetails.GitInstallationPath;
				state.GitExecutablePath = installDetails.GitExecutablePath;
				state = ValidateGitVersion(state);
			}

			if (!state.GitLfsIsValid)
			{
				state.GitLfsInstallationPath = installDetails.GitInstallationPath;
				state.GitLfsExecutablePath = installDetails.GitLfsExecutablePath;
				state = ValidateGitLfsVersion(state);
			}
			return state;
		}

		public GitInstallationState ValidateGitVersion(GitInstallationState state)
		{
			if (!state.GitExecutablePath.IsInitialized || !state.GitExecutablePath.FileExists())
			{
				state.GitIsValid = false;
				return state;
			}
			var version = new GitVersionTask(Token)
				.Configure(processManager)
				.Progress(progressReporter.UpdateProgress)
				.Catch(e => true)
				.RunSynchronously();
			state.GitIsValid = version >= Constants.MinimumGitVersion;
			state.GitVersion = version;
			return state;
		}

		public GitInstallationState ValidateGitLfsVersion(GitInstallationState state)
		{
			// default installation doesn't have a standalone "git-lfs" exe
			if (state.GitInstallationPath == installDetails.GitInstallationPath)
			{
				state.GitLfsIsValid = true;
				state.GitLfsVersion = state.GitVersion;
				return state;
			}

			if (!state.GitLfsExecutablePath.IsInitialized || !state.GitLfsExecutablePath.FileExists())
			{
				state.GitLfsIsValid = false;
				return state;
			}

			var version = new ProcessTask<TheVersion>(Token, state.GitLfsExecutablePath, "version", new LfsVersionOutputProcessor())
				.Configure(processManager)
				.Progress(progressReporter.UpdateProgress)
				.Catch(e => true)
				.RunSynchronously();
			state.GitLfsIsValid = version >= Constants.MinimumGitLfsVersion;
			state.GitLfsVersion = version;
			return state;
		}

		public GitInstallationState CheckForGitUpdates(GitInstallationState state)
		{
			if (state.GitInstallationPath == installDetails.GitInstallationPath)
			{
				state.GitReleaseManifest = DugiteReleaseManifest.Load(environment.UserCachePath.Combine("embedded-git.json"),
					installDetails.GitPackageFeed, environment);
				if (state.GitReleaseManifest != null)
				{
					state.GitIsValid = state.GitVersion >= state.GitReleaseManifest.Version;
					if (state.GitIsValid)
					{
						state.IsCustomGitPath = state.GitExecutablePath != installDetails.GitExecutablePath;
					}
					else
					{
						Logger.Trace($"{installDetails.GitExecutablePath} is out of date");
					}
				}
			}
			return state;
		}

		public GitInstallationState VerifyZipFiles(GitInstallationState state)
		{
			UpdateTask("Verifying package files", 100);
			try
			{
				if (!state.GitIsValid && state.GitReleaseManifest != null)
				{
					var asset = state.GitReleaseManifest.DugitePackage;
					installDetails.GitZipPath = installDetails.ZipPath.Combine(asset.Name);
					state.GitZipExists = installDetails.GitZipPath.FileExists();
					if (!Utils.VerifyFileIntegrity(installDetails.GitZipPath, asset.Hash))
					{
						installDetails.GitZipPath.DeleteIfExists();
					}
					state.GitZipExists = installDetails.GitZipPath.FileExists();
				}

				return state;
			}
			finally
			{
				UpdateTask("Verifying package files", 100);
			}
		}

		public GitInstallationState GetZipsIfNeeded(GitInstallationState state)
		{
			if (state.GitZipExists)
				return state;

			var downloader = new Downloader(environment.FileSystem);
			downloader.Catch(e => {
				LogHelper.Trace(e, "Failed to download");
				return true;
			});
			downloader.Progress(progressReporter.UpdateProgress);

			var asset = state.GitReleaseManifest.DugitePackage;

			if (!state.GitZipExists && !state.GitIsValid && state.GitReleaseManifest != null)
			{
				downloader.QueueDownload(asset.Url, installDetails.ZipPath, asset.Name);
			}
			downloader.RunSynchronously();

			state.GitZipExists = installDetails.GitZipPath.FileExists();
			return state;
		}

		private GitInstallationState GrabZipFromResourcesIfNeeded(GitInstallationState state)
		{
			if (!state.GitZipExists && !state.GitIsValid && state.GitInstallationPath == installDetails.GitInstallationPath)
				AssemblyResources.ToFile(ResourceType.Platform, "git.zip", installDetails.ZipPath, environment);
			state.GitZipExists = installDetails.GitZipPath.FileExists();

			return state;
		}

		public GitInstallationState ExtractGit(GitInstallationState state)
		{
			var tempZipExtractPath = NPath.CreateTempDirectory("git_zip_extract_zip_paths");

			if (state.GitZipExists && !state.GitIsValid)
			{
				var gitExtractPath = tempZipExtractPath.Combine("git").CreateDirectory();
				var unzipTask = new UnzipTask(Token, installDetails.GitZipPath, gitExtractPath,
											sharpZipLibHelper, environment.FileSystem)
					.Progress(progressReporter.UpdateProgress)
					.Catch(e => {
						LogHelper.Trace(e, "Failed to unzip " + installDetails.GitZipPath);
						return true;
					});

				unzipTask.RunSynchronously();
				var target = state.GitInstallationPath;
				if (unzipTask.Successful)
				{
					Logger.Trace("Moving Git source:{0} target:{1}", gitExtractPath.ToString(), target.ToString());

					UpdateTask("Copying git", 100);
					CopyHelper.Copy(gitExtractPath, target);
					UpdateTask("Copying git", 100);

					state.GitIsValid = true;

					state.IsCustomGitPath = state.GitExecutablePath != installDetails.GitExecutablePath;
				}
			}

			tempZipExtractPath.DeleteIfExists();
			return state;
		}

		public class GitInstallationState
		{
			public bool GitIsValid { get; set; }
			public bool GitLfsIsValid { get; set; }
			public bool GitZipExists { get; set; }
			public NPath GitInstallationPath { get; set; }
			public NPath GitExecutablePath { get; set; }
			public NPath GitLfsInstallationPath { get; set; }
			public NPath GitLfsExecutablePath { get; set; }
			public DateTimeOffset GitLastCheckTime { get; set; }
			public bool IsCustomGitPath { get; set; }
			public TheVersion GitVersion { get; set; }
			public TheVersion GitLfsVersion { get; set; }
			public DugiteReleaseManifest GitReleaseManifest { get; set; }

			public GitInstallationState() { }
			public GitInstallationState(IGitEnvironment currentEnvironment)
			{
				GitInstallationPath = currentEnvironment.GitInstallationPath;
				GitExecutablePath = currentEnvironment.GitExecutablePath;
				GitLfsInstallationPath = currentEnvironment.GitLfsInstallationPath;
				GitLfsExecutablePath = currentEnvironment.GitLfsExecutablePath;
			}

			public GitInstallationState(GitInstallDetails defaults)
			{
				GitInstallationPath = defaults.GitInstallationPath;
				GitExecutablePath = defaults.GitExecutablePath;
				GitLfsInstallationPath = defaults.GitInstallationPath;
				GitLfsExecutablePath = defaults.GitLfsExecutablePath;
			}
		}

		public class GitInstallDetails
		{
#if DEVELOPER_BUILD
			private const string packageFeed = "http://localhost:50000/unity/git/git.json";
#else
			private const string packageFeed = "https://api.github.com/repos/desktop/dugite-native/releases/latest";
#endif

			public const string GitDirectory = "git";

			public GitInstallDetails(NPath baseDataPath, IEnvironment environment)
			{
				ZipPath = baseDataPath.Combine("downloads");
				ZipPath.EnsureDirectoryExists();

				GitInstallationPath = baseDataPath.Combine(GitDirectory);
				GitExecutablePath = GitInstallationPath.Combine(environment.IsWindows ? "cmd" : "bin", "git" + UnityEnvironment.ExecutableExtension);
				//GitLfsExecutablePath = GitExecutablePath.Parent.Combine("git-lfs" + UnityEnvironment.ExecutableExtension);
				GitLfsExecutablePath = NPath.Default;
				GitPackageFeed = packageFeed;
			}

			public NPath ZipPath { get; }
			public NPath GitZipPath { get; set; }
			public NPath GitInstallationPath { get; }
			public NPath GitExecutablePath { get; }
			public NPath GitLfsExecutablePath { get; }
			public UriString GitPackageFeed { get; set; }
		}
	}
}
