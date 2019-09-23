// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace SpoiledCat.ProcessManager
{
	using Logging;
	using NiceIO;
	using Unity;
	using Utilities;

	public class ProcessManager : IProcessManager
	{
		private static readonly ILogging logger = LogHelper.GetLogger<ProcessManager>();

		private readonly IEnvironment environment;
		private readonly HashSet<IProcess> processes = new HashSet<IProcess>();
		public static IProcessManager Instance { get; private set; }

		public ProcessManager(IEnvironment environment,
			NPath defaultWorkingDirectory,
			CancellationToken cancellationToken)
		{
			Instance = this;
			this.environment = environment;
			DefaultProcessEnvironment = new ProcessEnvironment(environment, defaultWorkingDirectory);
			CancellationToken = cancellationToken;
		}

		public T Configure<T>(T processTask,
			NPath? workingDirectory = null,
			bool withInput = false)
				where T : IProcessTask
		{
			var startInfo = new ProcessStartInfo {
				RedirectStandardInput = withInput,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
				StandardOutputEncoding = Encoding.UTF8,
				StandardErrorEncoding = Encoding.UTF8
			};

			startInfo.Configure(processTask.ProcessEnvironment, workingDirectory);

			startInfo.FileName = processTask.ProcessName;
			startInfo.Arguments = processTask.ProcessArguments;
			processTask.Configure(startInfo);
			processTask.OnStartProcess += p => processes.Add(p);
			processTask.OnEndProcess += p => {
				if (processes.Contains(p))
					processes.Remove(p);
			};
			return processTask;
		}

		public void RunCommandLineWindow(NPath workingDirectory)
		{
			var startInfo = new ProcessStartInfo
			{
				RedirectStandardInput = false,
				RedirectStandardOutput = false,
				RedirectStandardError = false,
				UseShellExecute = false,
				CreateNoWindow = false
			};

			if (environment.IsWindows)
			{
				startInfo.FileName = "cmd";
				startInfo.Configure(DefaultProcessEnvironment, workingDirectory);
			}
			else if (environment.IsMac)
			{
				// we need to create a temp bash script to set up the environment properly, because
				// osx terminal app doesn't inherit the PATH env var and there's no way to pass it in

				var envVarFile = NPath.GetTempFilename();
				startInfo.FileName = "open";
				startInfo.Arguments = $"-a Terminal {envVarFile}";
				startInfo.Configure(DefaultProcessEnvironment, workingDirectory);

				var envVars = startInfo.EnvironmentVariables;
				var scriptContents = new[] {
						  $"cd \"{envVars["GHU_WORKINGDIR"]}\"",
						  $"PATH=\"{envVars["GHU_FULLPATH"]}\" /bin/bash"
					 };
				environment.FileSystem.WriteAllLines(envVarFile, scriptContents);
				MonoPosixShim.Chmod(envVarFile, 493); // -rwxr-xr-x mode (0755)
			}
			else
			{
				startInfo.FileName = "sh";
				startInfo.Configure(DefaultProcessEnvironment, workingDirectory);
			}

			Process.Start(startInfo);
		}

		public IProcess Reconnect(IProcess processTask, int pid)
		{
			logger.Trace("Reconnecting process " + pid);
			var p = Process.GetProcessById(pid);
			p.StartInfo.RedirectStandardInput = true;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.RedirectStandardError = true;
			processTask.Configure(p);
			return processTask;
		}

		public void Stop()
		{
			foreach (var p in processes.ToArray())
				p.Stop();
		}

		public static NPath FindExecutableInPath(string executable, bool recurse = false, params NPath[] searchPaths)
		{
			Guard.ArgumentNotNullOrWhiteSpace(executable, "executable");

			return searchPaths
				 .Where(x => x.IsInitialized && !x.IsRelative && x.DirectoryExists())
				 .SelectMany(x => x.Files(executable, recurse))
				 .FirstOrDefault();
		}

		public CancellationToken CancellationToken { get; }

		public static IProcessEnvironment DefaultProcessEnvironment { get; private set;  }
	}
}
