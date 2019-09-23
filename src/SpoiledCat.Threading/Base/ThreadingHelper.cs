// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Threading;
using System.Threading.Tasks;

namespace SpoiledCat.Threading
{
	using Utilities;
    public static class ThreadingHelper
    {
        public static TaskScheduler MainThreadScheduler { get; set; }

        public static int MainThread { get; set; }
        static bool InMainThread { get { return MainThread == 0 || Thread.CurrentThread.ManagedThreadId == MainThread; } }

        public static void SetUIThread()
        {
            MainThread = Thread.CurrentThread.ManagedThreadId;
        }

        public static bool InUIThread => InMainThread || Guard.InUnitTestRunner;

        public static TaskScheduler GetUIScheduler(SynchronizationContext synchronizationContext)
        {
            // quickly swap out the sync context so we can leverage FromCurrentSynchronizationContext for our ui scheduler
            var currentSyncContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(synchronizationContext);
            var ret = TaskScheduler.FromCurrentSynchronizationContext();
            if (currentSyncContext != null)
                SynchronizationContext.SetSynchronizationContext(currentSyncContext);
            return ret;
        }
    }
}
