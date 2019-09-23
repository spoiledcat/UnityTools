namespace SpoiledCat.Git
{
	using NiceIO;
	using Unity;

	public interface IGitEnvironment : IEnvironment
	{
		NPath GitInstallationPath { get; }
		NPath GitExecutablePath { get; }
		NPath GitLfsInstallationPath { get; }
		NPath GitLfsExecutablePath { get; }

		void Resolve(GitInstaller.GitInstallationState defaults);
	}

	public class GitEnvironment : UnityEnvironment, IGitEnvironment
	{
		public NPath GitInstallationPath { get; private set; }
		public NPath GitExecutablePath { get; private set; }
		public NPath GitLfsInstallationPath { get; private set; }
		public NPath GitLfsExecutablePath { get; private set; }


		public GitEnvironment(string applicationName, string logFile) : base(applicationName, logFile)
		{ }

		public override void Initialize(string unityVersion, NPath extensionInstallPath, NPath EditorApplication_applicationPath, NPath EditorApplication_applicationContentsPath, NPath Application_dataPath)
		{
			base.Initialize(unityVersion, extensionInstallPath, EditorApplication_applicationPath, EditorApplication_applicationContentsPath, Application_dataPath);
			Resolve(new GitInstaller.GitInstallationState(new GitInstaller.GitInstallDetails(UserCachePath, this)));
		}

		public void Resolve(GitInstaller.GitInstallationState state)
		{

			if (ResolvePaths("git", state.GitExecutablePath, out NPath gitExecPath, out NPath gitInstallPath))
			{
				GitExecutablePath = gitExecPath;
				GitInstallationPath = gitInstallPath;
			}
			else
				GitExecutablePath = GitInstallationPath = NPath.Default;

			if (GitInstallationPath != state.GitInstallationPath && ResolvePaths("git-lfs", state.GitLfsExecutablePath, out NPath gitLfsExecPath, out NPath gitLfsInstallPath)) {
				GitLfsExecutablePath = gitLfsExecPath;
				GitLfsInstallationPath = gitLfsExecPath;
			} else
				GitLfsExecutablePath = GitLfsInstallationPath = NPath.Default;
		}

		private bool ResolvePaths(string execName, NPath pathToExecutable, out NPath execPath, out NPath installPath)
		{
			execPath = installPath = NPath.Default;

			if (pathToExecutable.DirectoryExists())
				pathToExecutable = pathToExecutable.Combine(execName + ExecutableExtension);
			else
				pathToExecutable = pathToExecutable.Parent.Combine(execName + ExecutableExtension);

			if (!pathToExecutable.FileExists())
				return false;

			execPath = pathToExecutable;

			var actualInstallPath = pathToExecutable.Resolve().Parent;
			if (IsWindows)
			{
				if (actualInstallPath.Parent.FileName.StartsWith("mingw")) // we're in a mingw32/64 folder, need to go up one more
					actualInstallPath = actualInstallPath.Parent;
			}
			// if git is a symlink, we need to figure out where it actually is
			installPath = actualInstallPath.Parent;
			return true;
		}
	}
}
