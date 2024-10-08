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
using System.Text.RegularExpressions;
#if SPOILEDCAT_HAS_JSON_RUNTIME
using NotSerialized=SpoiledCat.Json.NotSerializedAttribute;
using NotSerializedProperty=SpoiledCat.Json.NotSerializedAttribute;
#else
using NotSerialized=System.NonSerializedAttribute;
[System.AttributeUsage(System.AttributeTargets.Property | System.AttributeTargets.Field)]
public sealed class NotSerializedPropertyAttribute : Attribute{}
#endif

namespace SpoiledCat.Utilities
{
#if SPOILEDCAT_HAS_LOGGING
	using Logging;
#endif

	[Serializable]
	public struct TheVersion : IComparable<TheVersion>
	{
		private const string versionRegex = @"^(?<major>\d+)(\.?(?<minor>[^.]+))?(\.?(?<patch>[^.]+))?(\.?(?<build>.+))?";
		private const int PART_COUNT = 4;
		public static TheVersion Default { get; } = default(TheVersion).Initialize(null);

		[NotSerialized] private int major;
		[NotSerializedProperty] public int Major { get { Initialize(Version); return major; } }
		[NotSerialized] private int minor;
		[NotSerializedProperty] public int Minor { get { Initialize(Version); return minor; } }
		[NotSerialized] private int patch;
		[NotSerializedProperty] public int Patch { get { Initialize(Version); return patch; } }
		[NotSerialized] private int build;
		[NotSerializedProperty] public int Build { get { Initialize(Version); return build; } }
		[NotSerialized] private string special;
		[NotSerializedProperty] public string Special { get { Initialize(Version); return special; } }
		[NotSerialized] private bool isAlpha;
		[NotSerializedProperty] public bool IsAlpha { get { Initialize(Version); return isAlpha; } }
		[NotSerialized] private bool isBeta;
		[NotSerializedProperty] public bool IsBeta { get { Initialize(Version); return isBeta; } }
		[NotSerialized] private bool isUnstable;
		[NotSerializedProperty] public bool IsUnstable { get { Initialize(Version); return isUnstable; } }

		[NotSerialized] private int[] intParts;
		[NotSerialized] private string[] stringParts;
		[NotSerialized] private int parts;
		[NotSerialized] private bool initialized;
		[NotSerialized] private string version;
		public string Version { get => version ?? (version = String.Empty); set => version = value; }

		private static readonly Regex regex = new Regex(versionRegex);

		public static TheVersion Parse(string version)
		{
			return default(TheVersion).Initialize(version);
		}

		private TheVersion Initialize(string theVersion)
		{
			if (initialized)
				return this;

			Version = theVersion?.Trim() ?? string.Empty;

			isAlpha = false;
			isBeta = false;
			major = 0;
			minor = 0;
			patch = 0;
			build = 0;
			special = null;
			parts = 0;

			intParts = new int[PART_COUNT];
			stringParts = new string[PART_COUNT];
			for (var i = 0; i < PART_COUNT; i++)
				stringParts[i] = intParts[i].ToString();

			if (string.IsNullOrEmpty(theVersion))
				return this;

			var match = regex.Match(theVersion);
			if (!match.Success)
			{
#if SPOILEDCAT_HAS_LOGGING
				LogHelper.GetLogger<TheVersion>().Error(new ArgumentException("Invalid version: " + theVersion, nameof(theVersion)));
#elif UNITY_2017_1_OR_NEWER
				UnityEngine.Debug.LogException(new ArgumentException("Invalid version: " + theVersion, nameof(theVersion)));
#endif
				return this;
			}

			var majorMatch = match.Groups["major"];
			major = int.Parse(majorMatch.Value);
			intParts[parts] = major;
			stringParts[parts] = major.ToString();
			parts = 1;

			var minorMatch = match.Groups["minor"];
			var patchMatch = match.Groups["patch"];
			var buildMatch = match.Groups["build"];

			if (minorMatch.Success)
			{
				if (!int.TryParse(minorMatch.Value, out minor))
				{
					if (minorMatch.Index >= 0)
						special = Version.Substring(minorMatch.Index).TrimEnd();
					stringParts[parts] = special ?? "0";
				}
				else
				{
					intParts[parts] = minor;
					stringParts[parts] = minor.ToString();
					parts++;

					if (patchMatch.Success)
					{
						if (!int.TryParse(patchMatch.Value, out patch))
						{
							if (patchMatch.Index >= 0)
								special = Version.Substring(patchMatch.Index).TrimEnd();
							stringParts[parts] = special ?? "0";
						}
						else
						{
							intParts[parts] = patch;
							stringParts[parts] = patch.ToString();
							parts++;

							if (buildMatch.Success)
							{
								if (!int.TryParse(buildMatch.Value, out build))
								{
									if (buildMatch.Index >= 0)
										special = Version.Substring(buildMatch.Index).TrimEnd();
									stringParts[parts] = special ?? "0";
								}
								else
								{
									intParts[parts] = build;
									stringParts[parts] = build.ToString();
									parts++;
								}
							}
						}
					}
				}
			}

			isUnstable = special != null;
			if (isUnstable)
			{
				isAlpha = special.IndexOf("alpha", StringComparison.Ordinal) >= 0;
				isBeta = special.IndexOf("beta", StringComparison.Ordinal) >= 0;
			}
			initialized = true;
			return this;
		}

		public override string ToString()
		{
			return Version;
		}

		public int CompareTo(TheVersion other)
		{
			if (this > other)
				return 1;
			if (this == other)
				return 0;
			return -1;
		}

		public override int GetHashCode()
		{
			var hash = 17;
			hash = hash * 23 + Major.GetHashCode();
			hash = hash * 23 + Minor.GetHashCode();
			hash = hash * 23 + Patch.GetHashCode();
			hash = hash * 23 + Build.GetHashCode();
			hash = hash * 23 + (Special != null ? Special.GetHashCode() : 0);
			return hash;
		}

		public override bool Equals(object obj)
		{
			if (obj is TheVersion theVersion)
				return Equals(theVersion);
			return false;
		}

		public bool Equals(TheVersion other)
		{
			return this == other;
		}

		public static bool operator==(TheVersion lhs, TheVersion rhs)
		{
			if (lhs.Version == rhs.Version)
				return true;
			return
				(lhs.Major == rhs.Major) &&
					(lhs.Minor == rhs.Minor) &&
					(lhs.Patch == rhs.Patch) &&
					(lhs.Build == rhs.Build) &&
					(lhs.Special == rhs.Special);
		}

		public static bool operator!=(TheVersion lhs, TheVersion rhs)
		{
			return !(lhs == rhs);
		}

		public static bool operator>(TheVersion lhs, TheVersion rhs)
		{
			if (lhs.Version == rhs.Version)
				return false;
			if (!lhs.initialized)
				return false;
			if (!rhs.initialized)
				return true;

			for (var i = 0; i < lhs.parts && i < rhs.parts; i++)
			{
				if (lhs.intParts[i] != rhs.intParts[i])
					return lhs.intParts[i] > rhs.intParts[i];
			}

			for (var i = 1; i < PART_COUNT; i++)
			{
				var ret = CompareVersionStrings(lhs.stringParts[i], rhs.stringParts[i]);
				if (ret != 0)
					return ret > 0;
			}

			return false;
		}

		public static bool operator<(TheVersion lhs, TheVersion rhs)
		{
			return !(lhs > rhs);
		}

		public static bool operator>=(TheVersion lhs, TheVersion rhs)
		{
			return lhs > rhs || lhs == rhs;
		}

		public static bool operator<=(TheVersion lhs, TheVersion rhs)
		{
			return lhs < rhs || lhs == rhs;
		}

		private static int CompareVersionStrings(string lhs, string rhs)
		{
			var lhsNumber = GetNumberFromVersionString(lhs, out int lhsNonDigitPos);

			var rhsNumber = GetNumberFromVersionString(rhs, out int rhsNonDigitPos);

			if (lhsNumber != rhsNumber)
				return lhsNumber.CompareTo(rhsNumber);

			if (lhsNonDigitPos < 0 && rhsNonDigitPos < 0)
				return 0;

			// versions with alphanumeric characters are always lower than ones without
			// i.e. 1.1alpha is lower than 1.1
			if (lhsNonDigitPos < 0)
				return 1;
			if (rhsNonDigitPos < 0)
				return -1;
			return string.Compare(lhs.Substring(lhsNonDigitPos), rhs.Substring(rhsNonDigitPos), StringComparison.Ordinal);
		}

		private static int GetNumberFromVersionString(string lhs, out int nonDigitPos)
		{
			nonDigitPos = IndexOfFirstNonDigit(lhs);
			int number = -1;
			if (nonDigitPos > -1)
			{
				int.TryParse(lhs.Substring(0, nonDigitPos), out number);
			}
			else
			{
				int.TryParse(lhs, out number);
			}
			return number;
		}

		private static int IndexOfFirstNonDigit(string str)
		{
			for (var i = 0; i < str.Length; i++)
			{
				if (!char.IsDigit(str[i]))
				{
					return i;
				}
			}
			return -1;
		}
	}
}
