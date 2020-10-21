namespace SpoiledCat.Threading.Extensions
{
	using System.Threading;
	using System.Threading.Tasks;

	public static class SyncContextExtensions
	{
		public static TaskScheduler FromSynchronizationContext(this SynchronizationContext context) => new SynchronizationContextTaskScheduler(context);
	}
}
