using System.Threading.Tasks;

namespace SpoiledCat.Base.Tests
{
	using Threading;

	public static class AsyncExtensions
	{
		public static Task<T> StartAndSwallowException<T>(this ITask<T> task)
		{
			return task.StartAwait(handler => default(T));
		}

		public static Task StartAndSwallowException(this ITask task)
		{
			return task.StartAwait(handler => { });
		}
	}

}
