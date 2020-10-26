// Copyright 2016-2020 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

namespace SpoiledCat.Logging
{
    public class MultipleLogAdapter : LogAdapterBase
    {
	    private readonly LogAdapterBase[] logAdapters;

	    public MultipleLogAdapter(params LogAdapterBase[] logAdapters)
        {
            this.logAdapters = logAdapters ?? new LogAdapterBase[0];
        }

	    public override void Info(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Info(context, message);
            }
        }

	    public override void Debug(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Debug(context, message);
            }
        }

	    public override void Trace(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Trace(context, message);
            }
        }

	    public override void Warning(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Warning(context, message);
            }
        }

	    public override void Error(string context, string message)
        {
            foreach (var logger in logAdapters)
            {
                logger.Error(context, message);
            }
        }
    }
}
