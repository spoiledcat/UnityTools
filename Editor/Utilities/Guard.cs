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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace SpoiledCat.Utilities
{
	[Serializable]
	public class InstanceNotInitializedException : InvalidOperationException
	{
		public InstanceNotInitializedException(object the, string property) :
			base(String.Format(CultureInfo.InvariantCulture, "{0} is not correctly initialized, {1} is null", the?.GetType().Name, property))
		{}

		protected InstanceNotInitializedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{ }
	}

	public static class Guard
	{
		public static void NotNull(object the, object value, string propertyName)
		{
			if (value != null) return;
			throw new InstanceNotInitializedException(the, propertyName);
		}

		public static void ArgumentNotNull(object value, string name)
		{
			if (value != null) return;
			string message = String.Format(CultureInfo.InvariantCulture, "Failed Null Check on '{0}'", name);
			throw new ArgumentNullException(name, message);
		}

		public static void ArgumentNotNullOrEmpty<T>(IList<T> value, string name)
		{
			if (value == null)
			{
				string message = String.Format(CultureInfo.InvariantCulture, "Failed Null Check on '{0}'", name);
				throw new ArgumentNullException(name, message);
			}

			if (!value.Any())
			{
				string message = String.Format(CultureInfo.InvariantCulture, "Failed Empty Check on '{0}'", name);
				throw new ArgumentNullException(name, message);
			}
		}

		public static void ArgumentNonNegative(int value, string name)
		{
			if (value > -1) return;

			var message = String.Format(CultureInfo.InvariantCulture, "The value for '{0}' must be non-negative", name);
			throw new ArgumentException(message, name);
		}

		/// <summary>
		///   Checks a string argument to ensure it isn't null or empty.
		/// </summary>
		/// <param name = "value">The argument value to check.</param>
		/// <param name = "name">The name of the argument.</param>
		public static void ArgumentNotNullOrWhiteSpace(string value, string name)
		{
			if (value != null && value.Trim().Length > 0)
				return;
			string message = String.Format(CultureInfo.InvariantCulture, "The value for '{0}' must not be empty", name);
			throw new ArgumentException(message, name);
		}

		public static void ArgumentInRange(int value, int minValue, string name)
		{
			if (value >= minValue) return;
			string message = String.Format(CultureInfo.InvariantCulture,
				"The value '{0}' for '{1}' must be greater than or equal to '{2}'",
				value,
				name,
				minValue);
			throw new ArgumentOutOfRangeException(name, message);
		}

		public static void ArgumentInRange(int value, int minValue, int maxValue, string name)
		{
			if (value >= minValue && value <= maxValue) return;
			string message = String.Format(CultureInfo.InvariantCulture,
				"The value '{0}' for '{1}' must be greater than or equal to '{2}' and less than or equal to '{3}'",
				value,
				name,
				minValue,
				maxValue);
			throw new ArgumentOutOfRangeException(name, message);
		}

		public static bool InUnitTestRunner { get; set; }
	}
}
