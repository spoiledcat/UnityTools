using System.Threading.Tasks;

namespace SpoiledCat.Base.Tests
{
	using Threading;

	public static class AsyncExtensions
	{
		public static Task<T> StartAndSwallowException<T>(this ITask<T> task)
		{
			var tcs = new TaskCompletionSource<T>();
			task.Then((success, result) => { tcs.SetResult(result); }, TaskAffinity.Concurrent);
			task.Start();
			return tcs.Task;
		}

		public static Task StartAndSwallowException(this ITask task)
		{
			var tcs = new TaskCompletionSource<bool>();
			task.Then(s => { tcs.SetResult(s); });
			task.Start();
			return tcs.Task;
		}
	}

}
