using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SpoiledCat.ProcessManager
{
	using SimpleIO;

	public interface IProcess
	{
		event Action<IProcess> OnEndProcess;
		event Action<string> OnErrorData;
		event Action<IProcess> OnStartProcess;
		void Configure(Process existingProcess);
		void Configure(ProcessStartInfo psi);
		void Stop();
		StreamWriter StandardInput { get; }
		int ProcessId { get; }
		string ProcessName { get; }
		string ProcessArguments { get; }
		Process Process { get; set; }
	}


	public interface IProcessManager
	{
		T Configure<T>(T processTask,
			SPath? workingDirectory = null,
			bool withInput = false)
			where T : IProcessTask;

		IProcess Reconnect(IProcess processTask, int i);
		void RunCommandLineWindow(SPath workingDirectory);
		void Stop();
		CancellationToken CancellationToken { get; }
		IProcessEnvironment DefaultProcessEnvironment { get; }
	}
}
