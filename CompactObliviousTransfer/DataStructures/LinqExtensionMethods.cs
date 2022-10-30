// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;

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
        public static void WriteInto<T>(this IEnumerable<T> enumerable, T[] buffer, int offset = 0)
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

        public static IEnumerable<T> Tile<T>(this IEnumerable<T> enumerable, int numberOfRepeats)
        {
            return Enumerable.Aggregate(
                Enumerable.Repeat(enumerable, numberOfRepeats),
                (accumulated, next) => accumulated.Concat(next)
            );
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> enumerable)
        {
            return enumerable.Aggregate(Enumerable.Empty<T>(), (flattened, next) => flattened.Concat(next));
        }

    }
}