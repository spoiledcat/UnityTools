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

using System;
using System.Diagnostics;
using System.IO;

namespace SpoiledCat.Utilities
{
#if SPOILEDCAT_HAS_IO
	using Extensions;
	using SimpleIO;
#endif

	public static class Utils
	{
		public static bool Copy(Stream source,
			Stream destination,
			long totalSize = 0,
			int chunkSize = 8192,
			Func<long, long, bool> progress = null,
			int progressUpdateRate = 100)
		{
			byte[] buffer = new byte[chunkSize];
			int bytesRead = 0;
			long totalRead = 0;
			float averageSpeed = -1f;
			float lastSpeed = 0f;
			float smoothing = 0.005f;
			long readLastSecond = 0;
			long timeToFinish = 0;
			Stopwatch watch = null;
			bool success = true;
			totalSize = totalSize < 0 ? 0 : totalSize;

			bool trackProgress = totalSize > 0 && progress != null;
			if (trackProgress)
				watch = new Stopwatch();

			do
			{
				if (trackProgress)
					watch.Start();

				bytesRead = source.Read(buffer, 0, totalSize > 0 && totalRead + chunkSize > totalSize ? (int)(totalSize - totalRead) : chunkSize);

				if (trackProgress)
					watch.Stop();

				totalRead += bytesRead;

				if (bytesRead > 0)
				{
					destination.Write(buffer, 0, bytesRead);
					if (trackProgress)
					{
						readLastSecond += bytesRead;
						if (watch.ElapsedMilliseconds >= progressUpdateRate || totalRead == totalSize || bytesRead == 0)
						{
							watch.Reset();
							if (bytesRead == 0) // we've reached the end
								totalSize = totalRead;

							lastSpeed = readLastSecond;
							readLastSecond = 0;
							averageSpeed = averageSpeed < 0f
								? lastSpeed
								: smoothing * lastSpeed + (1f - smoothing) * averageSpeed;
							timeToFinish = Math.Max(1L,
								(long)((totalSize - totalRead) / (averageSpeed / progressUpdateRate)));

							success = progress(totalRead, timeToFinish);
							if (!success)
								break;
						}
					}
					else // we still need to call the callback if it's there, so we can abort if needed
					{
						success = progress?.Invoke(totalRead, timeToFinish) ?? true;
						if (!success)
							break;
					}
				}
			} while (bytesRead > 0 && (totalSize == 0 || totalSize > totalRead));

			if (totalRead > 0)
				destination.Flush();

			return success;
		}

#if SPOILEDCAT_HAS_IO
		public static bool VerifyFileIntegrity(SPath file, string hash)
		{
			if (!file.IsInitialized || !file.FileExists())
				return false;
			string actual = null;
			if (hash.Length == 32)
				actual = file.ToMD5();
			else
				actual = file.ToSha256();
			return hash.Equals(actual, StringComparison.InvariantCultureIgnoreCase);
		}
#endif
	}
}
