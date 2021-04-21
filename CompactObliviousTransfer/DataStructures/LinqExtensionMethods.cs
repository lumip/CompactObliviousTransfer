using System;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{
    public static class LinqExtensionMethods
    {
        /// <summary>
        /// Writes the contents of the enumerable into a given buffer.
        ///
        /// Note that if the buffer is too small, the operation will result in an exception
        /// but prior to that fill the buffer as far as possible.
        /// </summary>
        public static void WriteInto<T>(this IEnumerable<T> enumerable, T[] buffer, int offset)
        {
            int i = offset;
            foreach (T x in enumerable)
            {
                if (i >= buffer.Length)
                    throw new ArgumentException("Output buffer is too small.", nameof(buffer));
                buffer[i] = x;
                i += 1;
            }
        }

        public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> enumerable)
        {
            int i = 0;
            foreach (T x in enumerable)
            {
                yield return (i, x);
                i += 1;
            }
        }
    }
}