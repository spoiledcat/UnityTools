using System.Text.RegularExpressions;

namespace SpoiledCat
{
	using Threading;
	using Utilities;

	public class VersionOutputProcessor : BaseOutputProcessor<TheVersion>
	{
		public static Regex GitVersionRegex = new Regex(@"git version (.*)");

		public override void LineReceived(string line)
		{
			if (string.IsNullOrEmpty(line))
				return;

			var match = GitVersionRegex.Match(line);
			if (match.Groups.Count > 1)
			{
				var version = TheVersion.Parse(match.Groups[1].Value);
				RaiseOnEntry(version);
			}
		}
	}
}
