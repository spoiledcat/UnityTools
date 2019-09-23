using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SpoiledCat.ProcessManager
{
	using NiceIO;

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
		T Configure<T>(T processTask,
			NPath? workingDirectory = null,
			bool withInput = false)
			where T : IProcessTask;

		IProcess Reconnect(IProcess processTask, int i);
		CancellationToken CancellationToken { get; }
		void RunCommandLineWindow(NPath workingDirectory);
		void Stop();
	}
}
