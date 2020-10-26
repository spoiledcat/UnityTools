using System;
using System.Threading;

namespace SpoiledCat.Threading
{
	using Helpers;

#if UNITY_EDITOR
	using System.Collections.Concurrent;
	using UnityEditor;

	public class MainThreadSynchronizationContext: SynchronizationContext, IMainThreadSynchronizationContext
	{
		private readonly ConcurrentQueue<Action> callbacks = new ConcurrentQueue<Action>();

		public MainThreadSynchronizationContext(CancellationToken token = default)
		{
			EditorApplication.update += Update;
		}

		public void Dispose()
		{
			EditorApplication.update -= Update;
		}

		public void Schedule(Action action)
		{
			action.EnsureNotNull(nameof(action));
			Post(act => ((Action)act)(), action);
		}

		public override void Post(SendOrPostCallback d, object state)
		{
			if (d == null)
				return;

			callbacks.Enqueue(() => d(state));
		}

		private void Update()
		{
			while (callbacks.TryDequeue(out var callback))
				callback();
		}
	}
#else
	public class MainThreadSynchronizationContext : ThreadSynchronizationContext, IMainThreadSynchronizationContext
	{
		public MainThreadSynchronizationContext(CancellationToken token = default) : base(token) {}

		public void Schedule(Action action)
		{
			action.EnsureNotNull(nameof(action));
			Post(act => ((Action)act)(), action);
		}
	}
#endif
}
