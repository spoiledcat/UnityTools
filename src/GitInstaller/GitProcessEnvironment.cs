using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace SpoiledCat.Git
{
	using SimpleIO;
	using ProcessManager;

	public class GitProcessEnvironment : ProcessEnvironment
	{
		public static IProcessEnvironment Instance { get; private set; }
		public new IGitEnvironment Environment => base.Environment as IGitEnvironment;
		private SPath basePath;
		private SPath gitBinary;
		private SPath libExecPath;
		private string[] envPath;

		public GitProcessEnvironment(IGitEnvironment environment, SPath repositoryRoot)
			: base(environment, FindRepositoryRoot(repositoryRoot))
		{
			Instance = this;
			Reset();
		}

		public void Reset(GitInstaller.GitInstallationState state = null)
		{
			basePath = gitBinary = libExecPath = SPath.Default;
			envPath = null;

			if (!Environment.GitInstallationPath.IsInitialized && !((state?.GitInstallationPath.IsInitialized) ?? false))
				return;

			if (state != null)
				Environment.Resolve(state);

			basePath = ResolveBasePath();
			envPath = CreateEnvPath().ToArray();
			if (ResolveGitExecPath(out SPath p))
				libExecPath = p;
		}

		private static SPath FindRepositoryRoot(SPath repositoryRoot)
		{
			if (repositoryRoot.IsInitialized)
				return repositoryRoot;
			var ret = SPath.CurrentDirectory.RecursiveParents.FirstOrDefault(d => d.Exists(".git"));
			if (ret.IsInitialized)
				return ret;
			return SPath.CurrentDirectory;
		}

		public override void Configure(ProcessStartInfo psi, SPath? workingDirectory = null)
		{
			base.Configure(psi, workingDirectory);

			var pathEntries = new List<string>(envPath);
			string separator = Environment.IsWindows ? ";" : ":";

			// we can only set this env var if there is a libexec/git-core. git will bypass internally bundled tools if this env var
			// is set, which will break Apple's system git on certain tools (like osx-credentialmanager)
			if (libExecPath.IsInitialized)
				psi.EnvironmentVariables["GIT_EXEC_PATH"] = libExecPath.ToString();

			pathEntries.Add("END");

			var path = string.Join(separator, pathEntries.ToArray()) + separator + Environment.Path;

			var pathEnvVarKey = Environment.GetEnvironmentVariableKey("PATH");
			psi.EnvironmentVariables[pathEnvVarKey] = path;

			//if (Environment.IsWindows)
			//{
			//    psi.EnvironmentVariables["PLINK_PROTOCOL"] = "ssh";
			//    psi.EnvironmentVariables["TERM"] = "msys";
			//}

			var httpProxy = Environment.GetEnvironmentVariable("HTTP_PROXY");
			if (!string.IsNullOrEmpty(httpProxy))
				psi.EnvironmentVariables["HTTP_PROXY"] = httpProxy;

			var httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
			if (!string.IsNullOrEmpty(httpsProxy))
				psi.EnvironmentVariables["HTTPS_PROXY"] = httpsProxy;
			psi.EnvironmentVariables["DISPLAY"] = "0";

			if (!Environment.IsWindows)
			{
				psi.EnvironmentVariables["GIT_TEMPLATE_DIR"] = Environment.GitInstallationPath.Combine("share/git-core/templates");
			}

			if (Environment.IsLinux)
			{
				psi.EnvironmentVariables["PREFIX"] = Environment.GitExecutablePath.Parent;
			}

			var sslCAInfo = Environment.GetEnvironmentVariable("GIT_SSL_CAINFO");
			if (string.IsNullOrWhiteSpace(sslCAInfo))
			{
				var certFile = basePath.Combine("ssl/cacert.pem");
				if (certFile.FileExists())
					psi.EnvironmentVariables["GIT_SSL_CAINFO"] = certFile.ToString();
			}
		}


		public bool ResolveGitExecPath(out SPath path)
		{
			path = ResolveBasePath().Combine("libexec", "git-core");
			return path.DirectoryExists();
		}

		private SPath ResolveBasePath()
		{
			var path = Environment.GitInstallationPath;
			if (Environment.IsWindows)
			{
				path = Environment.GitInstallationPath.Combine("mingw64");
				if (!path.DirectoryExists())
					path = Environment.GitInstallationPath.Combine("mingw32");
			}
			return path;
		}

		private IEnumerable<string> CreateEnvPath()
		{
			yield return Environment.GitExecutablePath.Parent.ToString();
			var basePath = ResolveBasePath();
			yield return basePath.Combine("bin").ToString();
			if (Environment.IsWindows)
				yield return Environment.GitInstallationPath.Combine("usr/bin").ToString();
			if (Environment.GitLfsInstallationPath.IsInitialized && Environment.GitLfsExecutablePath.Parent != Environment.GitExecutablePath.Parent)
				yield return Environment.GitLfsExecutablePath.Parent.ToString();
		}
	}
}
