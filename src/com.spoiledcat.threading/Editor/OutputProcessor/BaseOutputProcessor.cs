// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using System.Text;

namespace SpoiledCat.Threading
{
	using Logging;
	using NiceIO;

	public interface IOutputProcessor
	{
		void Process(string line);
	}

	public interface IOutputProcessor<T> : IOutputProcessor
	{
		T Result { get; }
		event Action<T> OnEntry;
	}

	public interface IOutputProcessor<TData, T> : IOutputProcessor<T>
	{
		new event Action<TData> OnEntry;
	}

	public class BaseOutputProcessor<T> : IOutputProcessor<T>
	{
		public event Action<T> OnEntry;
		public delegate T3 FuncO<in T1, T2, out T3>(T1 arg1, out T2 out1);

		private readonly FuncO<string, T, bool> handler;
		private readonly Func<string, T> simpleHandler;

		public BaseOutputProcessor(FuncO<string, T, bool> handler = null)
		{
			this.handler = handler;
		}

		public BaseOutputProcessor(Func<string, T> handler)
		{
			this.simpleHandler = handler;
		}

		public void Process(string line)
		{
			if (handler != null)
			{
				if (handler(line, out T entry))
					RaiseOnEntry(entry);
			}
			else if (simpleHandler != null)
			{
				RaiseOnEntry(simpleHandler(line));
			}
			else
				LineReceived(line);
		}

		public virtual void LineReceived(string line) {}
		protected void RaiseOnEntry(T entry)
		{
			Result = entry;
			OnEntry?.Invoke(entry);
		}
		public virtual T Result { get; protected set; }

		private ILogging logger;
		protected ILogging Logger { get { return logger = logger ?? LogHelper.GetLogger(GetType()); } }
	}

	public class BaseOutputProcessor<TData, T> : BaseOutputProcessor<T>, IOutputProcessor<TData, T>
	{
		public new event Action<TData> OnEntry;

		protected virtual void RaiseOnEntry(TData entry)
		{
			OnEntry?.Invoke(entry);
		}
	}

	public abstract class BaseOutputListProcessor<T> : BaseOutputProcessor<T, List<T>>
	{
		protected override void RaiseOnEntry(T entry)
		{
			if (Result == null)
			{
				Result = new List<T>();
			}
			Result.Add(entry);
			base.RaiseOnEntry(entry);
		}
	}

	/// <summary>
	/// Takes a string, raises an event with it, discards the result
	/// </summary>
	public class RaiseAndDiscardOutputProcessor : BaseOutputProcessor<string>
	{
		public override void LineReceived(string line)
		{
			if (line == null)
				return;
			RaiseOnEntry(line);
		}
		public override string Result => string.Empty;
	}

	public class SimpleOutputProcessor : BaseOutputProcessor<string>
	{
		private readonly StringBuilder sb = new StringBuilder();
		public override void LineReceived(string line)
		{
			if (line == null)
				return;
			sb.AppendLine(line);
			RaiseOnEntry(line);
		}
		public override string Result { get { return sb.ToString(); } }
	}

	public class SimpleListOutputProcessor : BaseOutputListProcessor<string>
	{
		public override void LineReceived(string line)
		{
			if (line == null)
				return;
			RaiseOnEntry(line);
		}
	}

	public class FirstResultOutputProcessor<T> : BaseOutputProcessor<T>
	{
		private readonly FuncO<string, T, bool> handler;
		private bool isSet = false;

		public FirstResultOutputProcessor(FuncO<string, T, bool> handler)
			: base()
		{
			this.handler = handler;
		}

		public override void LineReceived(string line)
		{
			if (!isSet)
			{
				if (handler(line, out T res))
				{
					Result = res;
					isSet = true;
					RaiseOnEntry(res);
				}
			}
		}
	}

	public class FirstNonNullLineOutputProcessor<T> : FirstResultOutputProcessor<T>
	{
		public FirstNonNullLineOutputProcessor(Func<string, T> converter = null)
		: base((string line, out T ret) => Parse(line, out ret, converter))
		{}

		private static bool Parse(string line, out T result, Func<string, T> converter = null)
		{
			result = default;
			if (String.IsNullOrEmpty(line))
				return false;

			line = line.Trim();

			if (converter != null) {
				result = converter(line);
				return true;
			}

			result = (T)(object)line;
			return true;
		}
	}
}
