using System;
using System.Collections.Generic;
using System.Linq;

namespace linq_lib
{
    public static class LinqExtensions
    {
        private static Random _random = new Random();

        public static List<T> Shuffle<T>(this List<T> items)
        {
            if (items == null) { return null; }
            if (!items.Any()) { return new List<T>(); }

            var cloned = items.Clone();
            var shuffled = new List<T>();
            while (cloned.Any())
            {
                var index = _random.Next(cloned.Count);
                var value = cloned[index];
                cloned.RemoveAt(index);
                shuffled.Add(value);
            }

            return shuffled;
        }

        public static List<T> Clone<T>(this List<T> items)
        {
            return items.Select(item => item).ToList();
        }
    }
}
