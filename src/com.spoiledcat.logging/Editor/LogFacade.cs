// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace SpoiledCat.Logging
{
	using Extensions;

	public class LogFacade : ILogging
    {
	    private readonly LogAdapterBase logger;
	    private bool? traceEnabled;

		public bool TracingEnabled
	    {
		    get => traceEnabled.HasValue ? traceEnabled.Value : LogHelper.TracingEnabled;
		    set
		    {
			    if (traceEnabled.HasValue)
				    traceEnabled = value;
			    else
				    LogHelper.TracingEnabled = value;
		    }
	    }

		public bool Verbose { get => LogHelper.Verbose; set => LogHelper.Verbose = value; }

	    private readonly string context;

	    public LogFacade(string context)
        {
            this.context = context;
            logger = LogHelper.LogAdapter;
        }

	    public LogFacade(string context, LogAdapterBase logger, bool traceEnabled = false)
	    {
		    this.context = context;
			this.logger = logger;
		    this.traceEnabled = traceEnabled;
	    }

	    public void Info(string message)
        {
            logger.Info(context, message);
        }

	    public void Debug(string message)
        {
#if DEVELOPER_BUILD
            logger.Debug(context, message);
#endif
		}

		public void Trace(string message)
        {
            if (!TracingEnabled) return;
            logger.Trace(context, message);
        }

	    public void Info(string format, params object[] objects)
        {
            Info(string.Format(format, objects));
        }

	    public void Info(Exception ex, string message)
        {
            Info(string.Concat(message, Environment.NewLine, Verbose ? ex.GetExceptionMessage() : ex.GetExceptionMessageShort()));
        }

	    public void Info(Exception ex)
        {
            Info(ex, message: string.Empty);
        }

	    public void Info(Exception ex, string format, params object[] objects)
        {
            Info(ex, string.Format(format, objects));
        }

	    public void Debug(string format, params object[] objects)
        {
#if DEVELOPER_BUILD
            Debug(string.Format(format, objects));
#endif
        }

	    public void Debug(Exception ex, string message)
        {
#if DEVELOPER_BUILD
            Debug(string.Concat(message, Environment.NewLine, Verbose ? ex.GetExceptionMessage() : ex.GetExceptionMessageShort()));
#endif
		}

		public void Debug(Exception ex)
        {
#if DEVELOPER_BUILD
            Debug(ex, string.Empty);
#endif
        }

	    public void Debug(Exception ex, string format, params object[] objects)
        {
#if DEVELOPER_BUILD
            Debug(ex, String.Format(format, objects));
#endif
        }

	    public void Trace(string format, params object[] objects)
        {
            if (!TracingEnabled) return;

            Trace(string.Format(format, objects));
        }

	    public void Trace(Exception ex, string message)
        {
            if (!TracingEnabled) return;

            Trace(string.Concat(message, Environment.NewLine, Verbose ? ex.GetExceptionMessage() : ex.GetExceptionMessageShort()));
        }

	    public void Trace(Exception ex)
        {
            if (!TracingEnabled) return;

            Trace(ex, string.Empty);
        }

	    public void Trace(Exception ex, string format, params object[] objects)
        {
            if (!TracingEnabled) return;

            Trace(ex, string.Format(format, objects));
        }

	    public void Warning(string message)
        {
	        logger.Warning(context, message);
        }

	    public void Warning(string format, params object[] objects)
        {
            Warning(string.Format(format, objects));
        }

	    public void Warning(Exception ex, string message)
        {
            Warning(string.Concat(message, Environment.NewLine, Verbose ? ex.GetExceptionMessage() : ex.GetExceptionMessageShort()));
        }

	    public void Warning(Exception ex)
        {
            Warning(ex, string.Empty);
        }

	    public void Warning(Exception ex, string format, params object[] objects)
        {
            Warning(ex, string.Format(format, objects));
        }

	    public void Error(string message)
        {
	        logger.Error(context, message);
        }

	    public void Error(string format, params object[] objects)
        {
            Error(string.Format(format, objects));
        }

	    public void Error(Exception ex, string message)
        {
			Error(string.Concat(message, Environment.NewLine, Verbose ? ex.GetExceptionMessage() : ex.GetExceptionMessageShort()));
        }

	    public void Error(Exception ex)
        {
            Error(ex, string.Empty);
        }

	    public void Error(Exception ex, string format, params object[] objects)
        {
            Error(ex, string.Format(format, objects));
        }
    }
}
