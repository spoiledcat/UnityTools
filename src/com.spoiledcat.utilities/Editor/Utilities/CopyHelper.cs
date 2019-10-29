﻿// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;

namespace SpoiledCat.Utilities
{
	using Logging;
	using NiceIO;

	public static class CopyHelper
	{
		private static readonly ILogging Logger = LogHelper.GetLogger(typeof(CopyHelper));

		public static void Copy(NPath fromPath, NPath toPath)
		{
			Logger.Trace("Copying from {0} to {1}", fromPath, toPath);

			try
			{
				CopyFolder(fromPath, toPath);
			}
			catch (Exception ex1)
			{
				Logger.Warning(ex1, "Error copying.");

				try
				{
					CopyFolderContents(fromPath, toPath);
				}
				catch (Exception ex2)
				{
					Logger.Error(ex2, "Error copying contents.");
					throw;
				}
			}
			finally
			{
				fromPath.DeleteIfExists();
			}
		}

		public static void CopyFolder(NPath fromPath, NPath toPath)
		{
			Logger.Trace("CopyFolder from {0} to {1}", fromPath, toPath);
			toPath.DeleteIfExists();
			toPath.EnsureParentDirectoryExists();
			fromPath.Move(toPath);
		}

		public static void CopyFolderContents(NPath fromPath, NPath toPath)
		{
			Logger.Trace("CopyFolderContents from {0} to {1}", fromPath, toPath);
			toPath.DeleteContents();
			fromPath.MoveFiles(toPath, true);
		}
	}
}
