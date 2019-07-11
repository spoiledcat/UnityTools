using System.Collections.Generic;
using System.Diagnostics;

namespace SpoiledCat.ProcessManager
{
	using Logging;
	using NiceIO;
	using Unity;
	using Utilities;

	public interface IProcessEnvironment
	{
		IEnvironment Environment { get; }
		NPath DefaultWorkingDirectory { get; }
		void Configure(ProcessStartInfo psi, NPath? workingDirectory = null);
	}

	public class ProcessEnvironment : IProcessEnvironment
	{
		public IEnvironment Environment { get; private set; }
		protected ILogging Logger { get; private set; }
		public NPath DefaultWorkingDirectory { get; private set; }

		public ProcessEnvironment(IEnvironment environment, NPath defaultWorkingDirectory)
		{
			Logger = LogHelper.GetLogger(GetType());
			Environment = environment;
			DefaultWorkingDirectory = defaultWorkingDirectory;
		}

		public virtual void Configure(ProcessStartInfo psi, NPath? workingDirectory = null)
		{
			Guard.ArgumentNotNull(psi, "psi");
			workingDirectory = workingDirectory ?? DefaultWorkingDirectory;

			psi.WorkingDirectory = workingDirectory;
			psi.EnvironmentVariables["HOME"] = NPath.HomeDirectory;
			psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = NPath.SystemTemp;

			var path = Environment.Path;
			psi.EnvironmentVariables["PROCESS_WORKINGDIR"] = workingDirectory;

			var pathEnvVarKey = Environment.GetEnvironmentVariableKey("PATH");
			psi.EnvironmentVariables["PROCESS_FULLPATH"] = path;
			psi.EnvironmentVariables[pathEnvVarKey] = path;
		}
	}
}
