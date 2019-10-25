// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Diagnostics;

namespace SpoiledCat.ProcessManager
{
	using Logging;
	using NiceIO;
	using Unity;

	public interface IProcessEnvironment
	{
		IEnvironment Environment { get; }
		void Configure(ProcessStartInfo psi, NPath? workingDirectory = null);
	}

	public class ProcessEnvironment : IProcessEnvironment
	{
		public IEnvironment Environment { get; private set; }
		protected ILogging Logger { get; private set; }

		public ProcessEnvironment(IEnvironment environment)
		{
			Logger = LogHelper.GetLogger(GetType());
			Environment = environment;
		}

		public virtual void Configure(ProcessStartInfo psi, NPath? workingDirectory = null)
		{
			Guard.ArgumentNotNull(psi, "psi");
			workingDirectory = workingDirectory ?? Environment.WorkingDirectory;

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
