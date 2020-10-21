// Copyright 2018-2019 Andreia Gaita
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace SpoiledCat.Extensions
{
	public static class Extensions
	{
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> enumerable, Action<T> func)
		{
			foreach (var item in enumerable)
			{
				func(item);
				yield return item;
			}
		}

		public static string TryGetLocation(this Assembly asm)
		{
			try
			{
				return asm.Location;
			}
			catch
			{
				return null;
			}
		}
	}
}
