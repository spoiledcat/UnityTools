// Copyright 2016-2020 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using System.Threading;

namespace SpoiledCat.Threading
{
	using SimpleIO;
	using Utilities;

	public class UnzipTask : TaskBase<SPath>
	{
		private readonly string archiveFilePath;
		private readonly SPath extractedPath;
		private readonly IFileSystem fileSystem;
		private readonly IZipHelper zipHelper;
		private ProgressReporter progressReporter = new ProgressReporter();
		private Dictionary<string, TaskData> tasks = new Dictionary<string, TaskData>();

		public UnzipTask(ITaskManager taskManager, SPath archiveFilePath, SPath extractedPath)
			: this(taskManager, taskManager?.Token ?? default, archiveFilePath, extractedPath, null, SPath.FileSystem)
		{}

		public UnzipTask(ITaskManager taskManager, CancellationToken token, SPath archiveFilePath, SPath extractedPath,
			IZipHelper zipHelper, IFileSystem fileSystem)
			: base(taskManager, token)
		{
			this.archiveFilePath = archiveFilePath;
			this.extractedPath = extractedPath;
			this.zipHelper = zipHelper ?? ZipHelper.Instance;
			this.fileSystem = fileSystem;
			Name = $"Unzip {archiveFilePath.FileName}";
			Message = $"Extracting {System.IO.Path.GetFileName(archiveFilePath)}";
			progressReporter.OnProgress += progress.UpdateProgress;
		}

		protected SPath BaseRun(bool success)
		{
			return base.RunWithReturn(success);
		}

		protected override SPath RunWithReturn(bool success)
		{
			var ret = BaseRun(success);
			try
			{
				ret = RunUnzip(success);
			}
			catch (Exception ex)
			{
				if (!RaiseFaultHandlers(ex))
					Exception.Rethrow();
			}
			return ret;
		}

		protected virtual SPath RunUnzip(bool success)
		{
			Logger.Trace("Unzip File: {0} to Path: {1}", archiveFilePath, extractedPath);

			Exception exception = null;
			var attempts = 0;
			do
			{
				if (Token.IsCancellationRequested)
					break;

				exception = null;
				try
				{
					success = zipHelper.Extract(archiveFilePath, extractedPath, Token,
						(file, size) =>
						{
							var task = new TaskData(file, size);
							tasks.Add(file, task);
							progressReporter.UpdateProgress(task.progress);
						},
						(fileRead, fileTotal, file) =>
						{
							if (tasks.TryGetValue(file, out TaskData task))
							{
								task.UpdateProgress(fileRead, fileTotal);
								progressReporter.UpdateProgress(task.progress);
								if (fileRead == fileTotal)
								{
									tasks.Remove(file);
								}
							}
							return !Token.IsCancellationRequested;
						});

					if (!success)
					{
						//extractedPath.DeleteIfExists();
						var message = $"Failed to extract {archiveFilePath} to {extractedPath}";
						exception = new UnzipException(message);
					}
				}
				catch (Exception ex)
				{
					exception = ex;
					success = false;
				}
			} while (attempts++ < RetryCount);

			if (!success)
			{
				Token.ThrowIfCancellationRequested();
				throw new UnzipException("Error unzipping file", exception);
			}
			return extractedPath;
		}

		protected int RetryCount { get; }
	}

	public class UnzipException : Exception
	{
		public UnzipException(string message) : base(message)
		{ }

		public UnzipException(string message, Exception innerException) : base(message, innerException)
		{ }
	}
}
