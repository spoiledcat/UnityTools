using System;
using System.Collections.Generic;
using System.Reflection;

namespace SpoiledCat
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