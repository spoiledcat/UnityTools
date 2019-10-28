// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SpoiledCat.Threading
{
	using Logging;
    public interface ITaskManager : IDisposable
    {
		TaskScheduler ConcurrentScheduler { get; }
		TaskScheduler ExclusiveScheduler { get; }
		TaskScheduler LongRunningScheduler { get; }
		TaskScheduler UIScheduler { get; set; }
		CancellationToken Token { get; }

		T Schedule<T>(T task) where T : ITask;
		ITask Run(Action action, string message = null);
		ITask RunInUI(Action action, string message = null);
		event Action<IProgress> OnProgress;


		/// <summary>
		/// Call this from the main thread so task manager knows which thread is the main thread
		/// It uses the current synchronization context to queue tasks to the main thread
		/// </summary>
		ITaskManager Initialize();

		/// <summary>
		/// Call this from the main thread so task manager knows which thread and
		/// synchronization context should be used
		/// </summary>
		ITaskManager Initialize(SynchronizationContext synchronizationContext);

		/// <summary>
		/// Call this from the main thread so task manager knows which thread it's in
		/// </summary>
		ITaskManager Initialize(TaskScheduler uiTaskScheduler);


		TaskScheduler GetScheduler(TaskAffinity affinity);
    }

	public class TaskManager : ITaskManager
	{
		private static readonly ILogging logger = LogHelper.GetLogger<TaskManager>();
		private CancellationTokenSource cts;
		private readonly ConcurrentExclusiveSchedulerPairCustom manager;

		public TaskScheduler UIScheduler { get; set; }
		public TaskScheduler ConcurrentScheduler => manager.ConcurrentScheduler;
		public TaskScheduler ExclusiveScheduler => manager.ExclusiveScheduler;
		public TaskScheduler LongRunningScheduler { get; private set; }
		public CancellationToken Token => cts.Token;

		private readonly ProgressReporter progressReporter = new ProgressReporter();

		public event Action<IProgress> OnProgress
		{
			add => progressReporter.OnProgress += value;
			remove => progressReporter.OnProgress -= value;
		}

		public TaskManager()
		{
			cts = new CancellationTokenSource();
			manager = new ConcurrentExclusiveSchedulerPairCustom(cts.Token);
		}

		public ITaskManager Initialize()
		{
			return Initialize(ThreadingHelper.GetUIScheduler(SynchronizationContext.Current));
		}

		public ITaskManager Initialize(SynchronizationContext synchronizationContext)
		{
			return Initialize(ThreadingHelper.GetUIScheduler(synchronizationContext));
		}

		public ITaskManager Initialize(TaskScheduler uiTaskScheduler)
		{
			UIScheduler = uiTaskScheduler;
			ThreadingHelper.SetUIThread();
			ThreadingHelper.MainThreadScheduler = UIScheduler;
			LongRunningScheduler = new TaskSchedulerExcludingThread(ThreadingHelper.MainThread);
			return this;
		}

		public TaskScheduler GetScheduler(TaskAffinity affinity)
		{
			switch (affinity)
			{
				case TaskAffinity.Exclusive:
					return ExclusiveScheduler;
				case TaskAffinity.UI:
					return UIScheduler;
				case TaskAffinity.LongRunning:
					return LongRunningScheduler;
				case TaskAffinity.Concurrent:
				default:
					return ConcurrentScheduler;
			}
		}

		public ITask Run(Action action, string message = null)
		{
			return new ActionTask(this, action) { Message = message }.Start();
		}

		public ITask RunInUI(Action action, string message = null)
		{
			return new ActionTask(this, action) { Affinity = TaskAffinity.UI, Message = message }.Start();
		}

		public T Schedule<T>(T task)
			where T : ITask
		{
			return Schedule(task, true);
		}

		private T Schedule<T>(T task, bool setupFaultHandler)
			where T : ITask
		{
			switch (task.Affinity)
			{
				case TaskAffinity.Exclusive:
					return ScheduleExclusive(task, setupFaultHandler);
				case TaskAffinity.UI:
					return ScheduleUI(task, setupFaultHandler);
				case TaskAffinity.LongRunning:
					return ScheduleLongRunning(task, setupFaultHandler);
				case TaskAffinity.Concurrent:
				default:
					return ScheduleConcurrent(task, setupFaultHandler);
			}
		}


		private T ScheduleUI<T>(T task, bool setupFaultHandler)
			where T : ITask
		{
			if (setupFaultHandler)
			{
				task.Task.ContinueWith(tt => {
					Exception ex = tt.Exception.GetBaseException();
					while (ex.InnerException != null) ex = ex.InnerException;
					logger.Error(ex, String.Format("Exception on ui thread: {0} {1}", tt.Id, task.Name));
				},
					cts.Token,
					TaskContinuationOptions.OnlyOnFaulted, ConcurrentScheduler
				);
			}
			return (T)task.Start(UIScheduler);
		}

		private T ScheduleExclusive<T>(T task, bool setupFaultHandler)
			where T : ITask
		{
			if (setupFaultHandler)
			{
				task.Task.ContinueWith(tt => {
					Exception ex = tt.Exception.GetBaseException();
					while (ex.InnerException != null) ex = ex.InnerException;
					logger.Error(ex, String.Format("Exception on exclusive thread: {0} {1}", tt.Id, task.Name));
				},
					cts.Token,
					TaskContinuationOptions.OnlyOnFaulted, ConcurrentScheduler
				);
			}

			task.Progress(progressReporter.UpdateProgress);
			return (T)task.Start(manager.ExclusiveScheduler);
		}

		private T ScheduleConcurrent<T>(T task, bool setupFaultHandler)
			where T : ITask
		{
			if (setupFaultHandler)
			{
				task.Task.ContinueWith(tt => {
					Exception ex = tt.Exception.GetBaseException();
					while (ex.InnerException != null) ex = ex.InnerException;
					logger.Error(ex, String.Format("Exception on concurrent thread: {0} {1}", tt.Id, task.Name));
				},
					cts.Token,
					TaskContinuationOptions.OnlyOnFaulted, ConcurrentScheduler
				);
			}

			task.Progress(progressReporter.UpdateProgress);
			return (T)task.Start(manager.ConcurrentScheduler);
		}

		private T ScheduleLongRunning<T>(T task, bool setupFaultHandler)
			where T : ITask
		{
			if (setupFaultHandler)
			{
				task.Task.ContinueWith(tt => {
					Exception ex = tt.Exception.GetBaseException();
					while (ex.InnerException != null) ex = ex.InnerException;
					logger.Error(ex, String.Format("Exception on long running thread: {0} {1}", tt.Id, task.Name));
				},
					cts.Token,
					TaskContinuationOptions.OnlyOnFaulted, LongRunningScheduler
				);
			}
			return (T)task.Start(LongRunningScheduler);
		}

		private async Task Stop()
		{
			if (cts == null)
				throw new ObjectDisposedException(nameof(TaskManager));
			manager.Complete();
			cts.Cancel();
			cts = null;
			await manager.Completion;
		}

		private bool disposed = false;
		private void Dispose(bool disposing)
		{
			if (disposed) return;
			disposed = true;
			if (disposing)
			{
				Stop().FireAndForget();
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
