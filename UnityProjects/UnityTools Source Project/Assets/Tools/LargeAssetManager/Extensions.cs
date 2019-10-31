namespace LocalTools
{
	using System;
	using System.Security.Cryptography;
	using SpoiledCat.NiceIO;

	public static class Extensions
	{
		public static string ToMD5(this NPath path)
		{
			byte[] computeHash;
			using (var md5 = MD5.Create())
			{
				using (var stream = NPath.FileSystem.OpenRead(path.MakeAbsolute()))
				{
					computeHash = md5.ComputeHash(stream);
				}
			}

			return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
		}
	}
}
