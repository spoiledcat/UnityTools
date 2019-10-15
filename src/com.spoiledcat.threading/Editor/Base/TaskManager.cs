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
		TaskScheduler UIScheduler { get; set; }
		CancellationToken Token { get; }

		T Schedule<T>(T task) where T : ITask;
		Task Wait();
		ITask Run(Action action, string message = null);
		ITask RunInUI(Action action, string message = null);
		event Action<IProgress> OnProgress;

		/// <summary>
		/// Call this from the main thread so task manager knows which thread and
		/// synchronization context should be used
		/// </summary>
		ITaskManager Initialize(SynchronizationContext synchronizationContext);

		/// <summary>
		/// Call this from the main thread so task manager knows which thread it's in
		/// </summary>
		ITaskManager Initialize(TaskScheduler uiTaskScheduler);
	}

	public class TaskManager : ITaskManager
	{
		private static readonly ILogging logger = LogHelper.GetLogger<TaskManager>();

		private CancellationTokenSource cts;
		private readonly ConcurrentExclusiveInterleave manager;
		public TaskScheduler UIScheduler { get; set; }
		public TaskScheduler ConcurrentScheduler { get { return (TaskScheduler)manager.ConcurrentTaskScheduler; } }
		public TaskScheduler ExclusiveScheduler { get { return manager.ExclusiveTaskScheduler; } }
		public CancellationToken Token { get { return cts.Token; } }

		private static ITaskManager instance;
		public static ITaskManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new TaskManager();
				}

				return instance;
			}
		}

		private ProgressReporter progressReporter = new ProgressReporter();

		public event Action<IProgress> OnProgress
		{
			add { progressReporter.OnProgress += value; }
			remove { progressReporter.OnProgress -= value; }
		}

		public TaskManager()
		{
			instance = this;
			cts = new CancellationTokenSource();
			this.manager = new ConcurrentExclusiveInterleave(cts.Token);
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
			return this;
		}

		public Task Wait()
		{
			return manager.Wait();
		}

		public static TaskScheduler GetScheduler(TaskAffinity affinity)
		{
			switch (affinity)
			{
				case TaskAffinity.Exclusive:
					return Instance.ExclusiveScheduler;
				case TaskAffinity.UI:
					return Instance.UIScheduler;
				case TaskAffinity.Concurrent:
				default:
					return Instance.ConcurrentScheduler;
			}
		}

		public ITask Run(Action action, string message = null)
		{
			return new ActionTask(Token, action) { Message = message }.Start();
		}

		public ITask RunInUI(Action action, string message = null)
		{
			return new ActionTask(Token, action) { Affinity = TaskAffinity.UI, Message = message }.Start();
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
			return (T)task.Start(manager.ExclusiveTaskScheduler);
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
			return (T)task.Start((TaskScheduler)manager.ConcurrentTaskScheduler);
		}

		private void Stop()
		{
			if (cts == null)
				throw new ObjectDisposedException(nameof(TaskManager));
			cts.Cancel();
			Wait();
			cts = null;
		}

		private bool disposed = false;
		private void Dispose(bool disposing)
		{
			if (disposed) return;
			disposed = true;
			if (disposing)
			{
				Stop();
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
	}
}
