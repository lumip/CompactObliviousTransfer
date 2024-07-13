// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Numerics;
using System.Security.Cryptography;

using CompactCryptoGroupAlgebra;

using CompactOT.DataStructures;

namespace CompactOT
{
    public static class RandomNumberGeneratorExtensions
    {

        /// <summary>
        /// Returns a random positive integer less than toExclusive.
        /// </summary>
        /// <param name="toExclusive">The exclusive upper bound for the random number to generate.</param>
        public static int GetInt32(this RandomNumberGenerator randomNumberGenerator, int toExclusive)
        {
            int[] result = randomNumberGenerator.GetInt32Array(toExclusive, 1);
            return result[0];
        }

        /// <summary>
        /// Returns an array of random positive integers, all less than toExclusive.
        /// </summary>
        /// <param name="toExclusive">The exclusive upper bound for the random numbers to generate.</param>
        /// <param name="amount">The amount of random numbers to generate, i.e., the size of the returned array.</param>
        public static int[] GetInt32Array(this RandomNumberGenerator randomNumberGenerator, int toExclusive, int amount)
        {
            int bitsPerSample = NumberLength.GetLength(toExclusive - 1).InBits;
            int mask = (1 << bitsPerSample) - 1;
            int totalBits = bitsPerSample * amount;
            int totalBytes = NumberLength.FromBitLength(totalBits).InBytes;
            int numberCandidates = (totalBytes * 8) / bitsPerSample;
            byte[] randomBytes = new byte[totalBytes + 1];

            int[] samples = new int[amount];
            int index = 0;
            while (index < amount)
            {

                randomNumberGenerator.GetBytes(randomBytes);
                randomBytes[totalBytes] = 0; // note (lumip): so that stupid BigInteger is unsinged
                                             // we need that for the check in the while and cannot just
                                             // BigInteger.Abs as that will compute the 2s complement, resulting
                                             // in most-significant bit to be always zero instead of random

                var x = new BigInteger(randomBytes);

                for (int slot = 0; slot < numberCandidates && index < amount; ++slot)
                {
                    var candidate = x & mask;
                    if (candidate < toExclusive)
                    {
                        samples[index] = (int)candidate;
                        index += 1;
                    }
                    x = x >> bitsPerSample;
                    // note(lumip): might be tempted to shift by only one bit in failure case,
                    // but I'm not entirely convinced that that doesn't degrade
                    // randomness in outputs (e.g., say we have toExlusive=5) 
                    // and see 0b111 (=7). Shifting by one either results in
                    // 0b111 again or 0b011 (=3), which might cause 3 to be overrepresented
                    // in outputs.. may not be an issue actually, haven't worked it
                    // out in detail and might be erring on the side of caution here.
                }
            }
            return samples;
        }

        /// <summary>
        /// Returns a BitArray of random bits.
        /// </summary>
        /// <param name="amount">The amount of random bits to generate, i.e., the length of the returned BitArray.</param>
        public static BitArray GetBits(this RandomNumberGenerator randomNumberGenerator, int amount)
        {
            int numberOfBytes = DataStructures.BitArray.RequiredBytes(amount);
            byte[] buffer = new byte[numberOfBytes];
            randomNumberGenerator.GetBytes(buffer);
            return DataStructures.BitArray.FromBytes(buffer, amount);
        }
    }
}
