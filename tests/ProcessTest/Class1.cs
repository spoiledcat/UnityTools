using SpoiledCat.NiceIO;
using SpoiledCat.ProcessManager;
using SpoiledCat.Threading;
using SpoiledCat.Unity;
using System;
using System.Threading;
using SpoiledCat.Git;
using SpoiledCat.Json;
using SpoiledCat.Utilities;

namespace ProcessTest
{
	public class Program
	{
		public static void Main()
		{
			Test1();
		}

		public static void Test1()
		{
			NPath.FileSystem.LocalAppData = NPath.CurrentDirectory;
			PocoJsonSerializerStrategy.RegisterCustomTypeHandler<NPath>(
				 value => (value.ToString(), true),
				 (value, type) => {
					 string str = value as string;
					 if (!string.IsNullOrEmpty(str))
					 {
						 return (new NPath(str), true);
					 }
					 return (NPath.Default, true);
				 });

			PocoJsonSerializerStrategy.RegisterCustomTypeHandler<UriString>(value => (value.ToString(), true), (value, type) => (new UriString(value as string), true));

			var taskManager = new TaskManager();
			taskManager.UIScheduler = ThreadingHelper.GetUIScheduler(new ThreadSynchronizationContext(taskManager.Token));
			ThreadingHelper.SetUIThread();

			var env = new GitEnvironment("testing", "mylog");
			env.Initialize("2018", NPath.CurrentDirectory, NPath.CurrentDirectory, NPath.CurrentDirectory, NPath.CurrentDirectory);
			var gitProcessEnv = new GitProcessEnvironment(env, env.UnityProjectPath);

			//var download = new DownloadTask("https://api.github.com/repos/desktop/dugite-native/releases/latest", NPath.CurrentDirectory, "index.html")
			//	.RunSynchronously();

			var pm = new ProcessManager(env, NPath.CurrentDirectory, TaskManager.Instance.Token);
			//var ret = new SimpleProcessTask("cmd", "/c echo haha")
			//	.RunSynchronously();
			//Console.WriteLine(ret);

			//var ret2 = new SimpleProcessTask<NPath>("cmd", "/c echo haha", line => line.ToNPath())
			//	.RunSynchronously();
			//Console.WriteLine(ret2);

			var installer = new GitInstaller(env, pm, taskManager.Token)
				.Progress(p => Console.WriteLine($"{p.Percentage * 100} {p.Message} - {p.InnerProgress?.Percentage * 100} {p.InnerProgress?.Message}"));
			var state = installer.RunSynchronously();

			if (state.GitIsValid)
				gitProcessEnv.Reset(state);

			Console.Read();
		}
	}
}
