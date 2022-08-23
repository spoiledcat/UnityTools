// Copyright 2016-2022 Andreia Gaita
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if SPOILEDCAT_HAS_IO && SPOILEDCAT_HAS_LOGGING

using System;

namespace SpoiledCat.Utilities
{
	using Logging;
	using SimpleIO;

	public static class CopyHelper
	{
		private static readonly ILogging Logger = LogHelper.GetLogger(typeof(CopyHelper));

		public static void Copy(SPath fromPath, SPath toPath)
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

		public static void CopyFolder(SPath fromPath, SPath toPath)
		{
			Logger.Trace("CopyFolder from {0} to {1}", fromPath, toPath);
			toPath.DeleteIfExists();
			toPath.EnsureParentDirectoryExists();
			fromPath.Move(toPath);
		}

		public static void CopyFolderContents(SPath fromPath, SPath toPath)
		{
			Logger.Trace("CopyFolderContents from {0} to {1}", fromPath, toPath);
			toPath.DeleteContents();
			fromPath.MoveFiles(toPath, true);
		}
	}
}
#endif
