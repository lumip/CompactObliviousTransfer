// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

namespace CompactOT
{

    public static class MathUtil
    {
        public static int DivideAndCeiling(int x, int y)
        {
            return (x + (y - 1)) / y;
        }

        public static int NextPowerOfTwo(int x)
        {
            int nextPowerOfTwo = 1;
            for (int i = 0; i < 32 && x > nextPowerOfTwo; ++i)
            {
                nextPowerOfTwo <<= 1;
            }
            if (x > nextPowerOfTwo)
            {
                throw new ArgumentOutOfRangeException($"The next power of two for value {x} cannot be represented as an integer.");
            }
            return nextPowerOfTwo;
        }

        public static bool IsPowerOfTwo(int x)
        {
            return (x & (x - 1)) == 0;
        }
    }
}
