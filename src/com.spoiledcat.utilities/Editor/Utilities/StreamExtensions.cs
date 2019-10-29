// Copyright 2016-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.IO;

namespace SpoiledCat.Extensions
{
	using System;
	using NiceIO;

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

    public static class NPathExtensions
    {
	    public static string ToMD5(this NPath path)
	    {
		    byte[] computeHash;
		    using (var hash = System.Security.Cryptography.MD5.Create())
		    {
			    using (var stream = NPath.FileSystem.OpenRead(path))
			    {
				    computeHash = hash.ComputeHash(stream);
			    }
		    }

		    return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
	    }

	    public static string ToSha256(this NPath path)
	    {
		    byte[] computeHash;
		    using (var hash = System.Security.Cryptography.SHA256.Create())
		    {
			    using (var stream = NPath.FileSystem.OpenRead(path))
			    {
				    computeHash = hash.ComputeHash(stream);
			    }
		    }

		    return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
	    }
    }
}
