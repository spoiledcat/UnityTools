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

#if SPOILEDCAT_HAS_IO

using System.IO;

namespace SpoiledCat.Extensions
{
	using System;
	using SimpleIO;

	public static class StreamExtensions
	{
		public static byte[] ToByteArray(this Stream input)
		{
			var buffer = new byte[16 * 1024];
			using (var ms = new MemoryStream())
			{
				int read;
				while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, read);
				}

				return ms.ToArray();
			}
		}
	}

	public static class SPathExtensions
	{
		public static string ToMD5(this SPath path)
		{
			byte[] computeHash;
			using (var hash = System.Security.Cryptography.MD5.Create())
			{
				using (var stream = SPath.FileSystem.OpenRead(path))
				{
					computeHash = hash.ComputeHash(stream);
				}
			}

			return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
		}

		public static string ToSha256(this SPath path)
		{
			byte[] computeHash;
			using (var hash = System.Security.Cryptography.SHA256.Create())
			{
				using (var stream = SPath.FileSystem.OpenRead(path))
				{
					computeHash = hash.ComputeHash(stream);
				}
			}

			return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
		}
	}
}
#endif
