using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using SpoiledCat.NiceIO;

namespace SpoiledCat.ProcessManager
{
	public interface IProcessEnvironment
	{
		void Configure(ProcessStartInfo psi, NPath workingDirectory);
	}

	public interface IProcess
	{
		void Configure(Process existingProcess);
		void Configure(ProcessStartInfo psi);
		void Stop();
		event Action<string> OnErrorData;
		StreamWriter StandardInput { get; }
		int ProcessId { get; }
		string ProcessName { get; }
		string ProcessArguments { get; }
		Process Process { get; set; }
		event Action<IProcess> OnStartProcess;
		event Action<IProcess> OnEndProcess;
	}


	public interface IProcessManager
	{
		T Configure<T>(T processTask, NPath? executable = null, string arguments = null, NPath? workingDirectory = null,
			bool withInput = false)
			where T : IProcess;
		IProcess Reconnect(IProcess processTask, int i);
		CancellationToken CancellationToken { get; }
		void RunCommandLineWindow(NPath workingDirectory);
		void Stop();
	}
}
