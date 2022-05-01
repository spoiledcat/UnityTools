// Copyright 2016-2022 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

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
