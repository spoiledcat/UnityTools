using System.Collections.Generic;
using System.Diagnostics;

namespace SpoiledCat.ProcessManager
{
	using Logging;
	using NiceIO;
	using Unity;
	using Utilities;

	public class ProcessEnvironment : IProcessEnvironment
    {
        protected IEnvironment Environment { get; private set; }
        protected ILogging Logger { get; private set; }

        public ProcessEnvironment(IEnvironment environment)
        {
            Logger = LogHelper.GetLogger(GetType());
            Environment = environment;
        }

        public virtual void Configure(ProcessStartInfo psi, NPath workingDirectory)
        {
	        Guard.ArgumentNotNull(psi, "psi");

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
