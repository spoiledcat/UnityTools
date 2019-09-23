using System;

namespace SpoiledCat.Git
{
	using Threading;
	using Utilities;
	public class LfsVersionOutputProcessor : FirstResultOutputProcessor<TheVersion>
	{
		public LfsVersionOutputProcessor()
			: base(Parse)
		{ }

		private static bool Parse(string line, out TheVersion version)
		{
			version = default;
			var parts = line.Split('/', ' ');
			if (parts.Length <= 1)
				return false;
			version = TheVersion.Parse(parts[1]);
			return true;
		}
	}
}
