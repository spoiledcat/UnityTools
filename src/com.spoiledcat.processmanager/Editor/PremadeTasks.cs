// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.


namespace SpoiledCat.ProcessManager
{
	using SimpleIO;
	using System;
	using System.Collections.Generic;
	using Threading;

	public class SimpleProcessTask : ProcessTask<string>
	{
		public SimpleProcessTask(ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments, SPath? workingDirectory = null,
			IOutputProcessor<string> processor = null)
			: base(taskManager, processManager.DefaultProcessEnvironment,
				  executable, arguments, processor ?? new SimpleOutputProcessor())
		{
			processManager.Configure(this, workingDirectory);
		}
	}

	public class SimpleProcessTask<T> : ProcessTask<T>
	{
		public SimpleProcessTask(
			ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments,
			Func<string, T> processor,
			SPath? workingDirectory = null
		)
			 : base(taskManager, taskManager?.Token ?? default,
				processManager.DefaultProcessEnvironment,
				executable, arguments,
				new BaseOutputProcessor<T>((string line, out T result) => {
					result = default(T);
					if (line == null) return false;
					result = processor(line);
					return true;
				})
		)
		{
			processManager.Configure(this, workingDirectory);
		}

		public SimpleProcessTask(
			ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments,
			IOutputProcessor<T> outputProcessor,
			SPath? workingDirectory = null
		)
			 : base(taskManager, taskManager?.Token ?? default,
				processManager.DefaultProcessEnvironment,
				executable, arguments, outputProcessor)
		{
			processManager.Configure(this, workingDirectory);
		}
	}

	public class SimpleListProcessTask : ProcessTaskWithListOutput<string>
	{
		public SimpleListProcessTask(
			ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments, SPath? workingDirectory = null,
			IOutputProcessor<string, List<string>> processor = null
		)
			 : base(taskManager, taskManager?.Token ?? default,
				processManager.DefaultProcessEnvironment,
				executable, arguments,
				processor ?? new SimpleListOutputProcessor())
		{
			processManager.Configure(this, workingDirectory);
		}
	}

	public class FirstNonNullLineProcessTask : SimpleProcessTask
	{
		public FirstNonNullLineProcessTask(ITaskManager taskManager, IProcessManager processManager,
			string executable, string arguments, SPath? workingDirectory = null)
			: base(taskManager, processManager, executable, arguments, workingDirectory,
				  new FirstNonNullLineOutputProcessor<string>())
		{
		}
	}
}
