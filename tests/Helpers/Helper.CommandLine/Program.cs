namespace SpoiledCat.Tests.CommandLine
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Text;
	using System.Threading;
	using Extensions;
	using Logging;
	using Mono.Options;
	using SimpleIO;
	using TestWebServer;
	using Threading;
	using Utilities;

	static class Program
	{
		private static ILogging Logger;

		private static string ReadAllTextIfFileExists(this string path)
		{
			var file = path.ToSPath();
			if (!file.IsInitialized || !file.FileExists())
			{
				return null;
			}
			return file.ReadAllText();
		}

		private static void RunWebServer(SPath path, int port)
		{
			if (!path.IsInitialized)
			{
				path = typeof(Program).Assembly.Location.ToSPath().Parent.Combine("files");
			}

			var evt = new ManualResetEventSlim(false);
			var server = new HttpServer(path, port);
			var thread = new Thread(() => {
				Logger.Error("Press any key to exit");

				try
				{
					server.Start();
					Console.Read();
					server.Stop();
				}
				catch (Exception ex)
				{
					Logger.Error(ex);
				}
				evt.Set();
			});

			thread.Start();
			evt.Wait();
		}

		private static HttpServer RunWebServer(int port)
		{
			var path = typeof(Program).Assembly.Location.ToSPath().Parent.Combine("files");
			var server = new HttpServer(path, port);
			var thread = new Thread(() => { server.Start(); });

			thread.Start();

			return server;
		}

		private static int Main(string[] args)
		{
			LogHelper.LogAdapter = new ConsoleLogAdapter();
			Logger = LogHelper.GetLogger();

			var retCode = 0;
			string data = null;
			string error = null;
			var sleepms = 0;
			var p = new OptionSet();
			var readInputToEof = false;
			var lines = new List<string>();
			var runWebServer = false;
			SPath outfile = SPath.Default;
			SPath path = SPath.Default;
			string releaseNotes = null;
			var webServerPort = -1;
			var generateVersion = false;
			var generatePackage = false;
			string version = null;
			string url = null;
			string readVersion = null;
			string msg = null;
			string host = null;
			var runUsage = false;
			var block = false;
			var exception = false;

			var arguments = new List<string>(args);
			if (arguments.Contains("usage"))
			{
				runUsage = true;
				arguments.RemoveRange(0, 2);
			}

			p = p
				.Add("r=", (int v) => retCode = v)
				.Add("d=|data=", v => data = v)
				.Add("e=|error=", v => error = v)
				.Add("x|exception", v => exception = true)
				.Add("f=|file=", v => data = File.ReadAllText(v))
				.Add("ef=|errorFile=", v => error = File.ReadAllText(v))
				.Add("sleep=", (int v) => sleepms = v)
				.Add("i|input", v => readInputToEof = true)
				.Add("w|web", v => runWebServer = true)
				.Add("p|port=", "Port", (int v) => webServerPort = v)
				.Add("g|generateVersion", v => generateVersion = true)
				.Add("v=|version=", v => version = v)
				.Add("gen-package", "Pass --version --url --path --md5 --rn --msg to generate a package", v => generatePackage = true)
				.Add("u=|url=", v => url = v)
				.Add("path=", v => path = v.ToSPath())
				.Add("rn=", "Path to file with release notes", v => releaseNotes = v.ReadAllTextIfFileExists())
				.Add("msg=", "Path to file with message for package", v => msg = v.ReadAllTextIfFileExists())
				.Add("readVersion=", v => readVersion = v)
				.Add("o=|outfile=", v => outfile = v.ToSPath().MakeAbsolute())
				.Add("h=", "Host", v => host = v)
				.Add("help", v => p.WriteOptionDescriptions(Console.Out))
				.Add("b|block", v => block = true)
				;

			var extra = p.Parse(arguments);
			if (runUsage)
			{
				extra.Remove("usage");
				p.Parse(extra);

				path = extra[extra.Count - 1].ToSPath();
				var server = RunWebServer(webServerPort);
				var webRequest = (HttpWebRequest)WebRequest.Create(new UriString("http://localhost:" + webServerPort + "/api/usage/unity"));
				webRequest.Method = "POST";
				using (var sw = new StreamWriter(webRequest.GetRequestStream()))
				{
					foreach (var line in path.ReadAllLines())
					{
						sw.WriteLine(line);
					}
				}
				using (var webResponse = (HttpWebResponse)webRequest.GetResponseWithoutException())
				{
					var ms = new MemoryStream();
					var responseLength = webResponse.ContentLength;
					using (var sr = new StreamWriter(ms))
					using (var responseStream = webResponse.GetResponseStream())
					{
						Utils.Copy(responseStream, ms, responseLength);
					}
					Console.WriteLine(Encoding.ASCII.GetString(ms.ToArray()));
				}

				server.Stop();
				return 0;
			}

			if (generatePackage)
			{
				var md5 = path.ToMD5();
				url += "/" + path.FileName;
				//var package = new Package
				//{
				//    Message = msg,
				//    Md5 = md5,
				//    ReleaseNotes = releaseNotes,
				//    ReleaseNotesUrl = null,
				//    Url = url,
				//    Version = TheVersion.Parse(version),
				//};

				//var json = package.ToJson(lowerCase: true, onlyPublic: false);
				//if (outfile.IsInitialized)
				//    outfile.WriteAllText(json);
				//else
				//    Console.WriteLine(json);
				return 0;
			}

			if (readVersion != null)
			{
				//var json = File.ReadAllText(readVersion);
				//var package = json.FromJson<Package>(lowerCase: true, onlyPublic: false);
				//Console.WriteLine(package);
				//Console.WriteLine($"{package.Url} {package.Version}");
				return 0;
			}

			if (generateVersion)
			{
				//Logger.Error($"Generating version json {version} to {(outfile.IsInitialized ? outfile : "console")}");
				//var vv = TheVersion.Parse(version);
				//url += $"/unity/releases/github-for-unity-{version}.unitypackage";
				//var package = new Package { Url = url, Version = vv};
				//var json = package.ToJson(lowerCase: true, onlyPublic: false);
				//if (outfile.IsInitialized)
				//    outfile.WriteAllText(json);
				//else
				//    Logger.Info(json);
				return 0;
			}

			if (runWebServer)
			{
				if (webServerPort < 0)
				{
					webServerPort = 50000;
				}
				RunWebServer(outfile, webServerPort);
				return 0;
			}

			if (sleepms > 0)
			{
				Thread.Sleep(sleepms);
			}

			if (block)
			{
				while (true)
				{
					if (readInputToEof)
					{
						Console.WriteLine(Console.ReadLine());
					}
				}
			}

			if (readInputToEof)
			{
				string line;
				while ((line = Console.ReadLine()) != null)
				{
					lines.Add(line);
				}
			}

			if (!string.IsNullOrEmpty(data))
			{
				Console.WriteLine(data);
			}
			else if (readInputToEof)
			{
				Console.WriteLine(string.Join(Environment.NewLine, lines.ToArray()));
			}

			if (!string.IsNullOrEmpty(error))
			{
				Console.Error.WriteLine(error);
			}

			if (exception)
			{
				throw new InvalidOperationException();
			}

			return retCode;
		}
	}
}
