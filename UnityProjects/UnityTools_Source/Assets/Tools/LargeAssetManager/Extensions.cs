namespace LocalTools
{
	using System;
	using System.Security.Cryptography;
	using SpoiledCat.SimpleIO;

	public static class Extensions
	{
		public static string ToMD5(this SPath path)
		{
			byte[] computeHash;
			using (var md5 = MD5.Create())
			{
				using (var stream = SPath.FileSystem.OpenRead(path.MakeAbsolute()))
				{
					computeHash = md5.ComputeHash(stream);
				}
			}

			return BitConverter.ToString(computeHash).Replace("-", string.Empty).ToLower();
		}
	}
}
