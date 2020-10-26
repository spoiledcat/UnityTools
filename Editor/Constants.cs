// Copyright 2016-2020 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System.Globalization;

namespace SpoiledCat.UI
{
	public static class Constants
	{
		public const string Iso8601Format = @"yyyy-MM-dd\THH\:mm\:ss.fffzzz";
		public const string Iso8601FormatZ = @"yyyy-MM-dd\THH\:mm\:ss\Z";
		public const DateTimeStyles DateTimeStyle = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
		public static readonly string[] Iso8601Formats = {
			Iso8601Format,
			Iso8601FormatZ,
			@"yyyy-MM-dd\THH\:mm\:ss.fffffffzzz",
			@"yyyy-MM-dd\THH\:mm\:ss.ffffffzzz",
			@"yyyy-MM-dd\THH\:mm\:ss.fffffzzz",
			@"yyyy-MM-dd\THH\:mm\:ss.ffffzzz",
			@"yyyy-MM-dd\THH\:mm\:ss.ffzzz",
			@"yyyy-MM-dd\THH\:mm\:ss.fzzz",
			@"yyyy-MM-dd\THH\:mm\:sszzz",
			@"yyyy-MM-dd\THH\:mm\:ss.fffffff\Z",
			@"yyyy-MM-dd\THH\:mm\:ss.ffffff\Z",
			@"yyyy-MM-dd\THH\:mm\:ss.fffff\Z",
			@"yyyy-MM-dd\THH\:mm\:ss.ffff\Z",
			@"yyyy-MM-dd\THH\:mm\:ss.fff\Z",
			@"yyyy-MM-dd\THH\:mm\:ss.ff\Z",
			@"yyyy-MM-dd\THH\:mm\:ss.f\Z",
		};
	}
}
