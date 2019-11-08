namespace SpoiledCat.Git
{
	using SimpleIO;
	using Unity;

	public interface IGitEnvironment : IEnvironment
	{
		SPath GitInstallationPath { get; }
		SPath GitExecutablePath { get; }
		SPath GitLfsInstallationPath { get; }
		SPath GitLfsExecutablePath { get; }

		void Resolve(GitInstaller.GitInstallationState defaults);
	}

	public class GitEnvironment : UnityEnvironment, IGitEnvironment
	{
		public SPath GitInstallationPath { get; private set; }
		public SPath GitExecutablePath { get; private set; }
		public SPath GitLfsInstallationPath { get; private set; }
		public SPath GitLfsExecutablePath { get; private set; }


		public GitEnvironment(string applicationName, string logFile) : base(applicationName, logFile)
		{ }

		public override void Initialize(string unityVersion, SPath extensionInstallPath, SPath EditorApplication_applicationPath, SPath EditorApplication_applicationContentsPath, SPath Application_dataPath)
		{
			base.Initialize(unityVersion, extensionInstallPath, EditorApplication_applicationPath, EditorApplication_applicationContentsPath, Application_dataPath);
			Resolve(new GitInstaller.GitInstallationState(new GitInstaller.GitInstallDetails(UserCachePath, this)));
		}

		public void Resolve(GitInstaller.GitInstallationState state)
		{

			if (ResolvePaths("git", state.GitExecutablePath, out SPath gitExecPath, out SPath gitInstallPath))
			{
				GitExecutablePath = gitExecPath;
				GitInstallationPath = gitInstallPath;
			}
			else
				GitExecutablePath = GitInstallationPath = SPath.Default;

			if (GitInstallationPath != state.GitInstallationPath && ResolvePaths("git-lfs", state.GitLfsExecutablePath, out SPath gitLfsExecPath, out SPath gitLfsInstallPath)) {
				GitLfsExecutablePath = gitLfsExecPath;
				GitLfsInstallationPath = gitLfsExecPath;
			} else
				GitLfsExecutablePath = GitLfsInstallationPath = SPath.Default;
		}

		private bool ResolvePaths(string execName, SPath pathToExecutable, out SPath execPath, out SPath installPath)
		{
			execPath = installPath = SPath.Default;

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
