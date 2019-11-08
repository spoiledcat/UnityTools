// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Diagnostics;

namespace SpoiledCat.ProcessManager
{
	using Logging;
	using SimpleIO;
	using Unity;

	public interface IProcessEnvironment
	{
		void Configure(ProcessStartInfo psi, SPath? workingDirectory = null);
		IEnvironment Environment { get; }
	}

	public class ProcessEnvironment : IProcessEnvironment
	{
		public ProcessEnvironment(IEnvironment environment)
		{
			Logger = LogHelper.GetLogger(GetType());
			Environment = environment;
		}

		public virtual void Configure(ProcessStartInfo psi, SPath? workingDirectory = null)
		{
			Guard.ArgumentNotNull(psi, "psi");
			workingDirectory = workingDirectory ?? Environment.WorkingDirectory;

			psi.WorkingDirectory = workingDirectory;
			psi.EnvironmentVariables["HOME"] = SPath.HomeDirectory;
			psi.EnvironmentVariables["TMP"] = psi.EnvironmentVariables["TEMP"] = SPath.SystemTemp;

			var path = Environment.Path;
			psi.EnvironmentVariables["PROCESS_WORKINGDIR"] = workingDirectory;

			var pathEnvVarKey = Environment.GetEnvironmentVariableKey("PATH");
			psi.EnvironmentVariables["PROCESS_FULLPATH"] = path;
			psi.EnvironmentVariables[pathEnvVarKey] = path;
		}

		public IEnvironment Environment { get; private set; }
		protected ILogging Logger { get; private set; }
	}
}
