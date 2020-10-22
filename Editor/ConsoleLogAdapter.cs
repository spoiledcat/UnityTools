// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Threading;

namespace SpoiledCat.Logging
{
	public class ConsoleLogAdapter : LogAdapterBase
	{
		public override void Info(string context, string message)
		{
			WriteLine(context, message);
		}

		public override void Debug(string context, string message)
		{
			WriteLine(context, message);
		}

		public override void Trace(string context, string message)
		{
			WriteLine(context, message);
		}

		public override void Warning(string context, string message)
		{
			WriteLine(context, message);
		}

		public override void Error(string context, string message)
		{
			WriteLine(context, message);
		}

		private string GetMessage(string context, string message)
		{
			var time = DateTime.Now.ToString("HH:mm:ss.fff tt");
			var threadId = Thread.CurrentThread.ManagedThreadId;
			return string.Format("{0} [{1,2}] {2} {3}", time, threadId, context, message);
		}

		private void WriteLine(string context, string message)
		{
			Console.WriteLine(GetMessage(context, message));
		}
	}
}
