using System;
using System.Collections;
using System.Security.Cryptography;
using CompactCryptoGroupAlgebra;

namespace CompactOT
{
    public static class RandomNumberGeneratorExtensions
    {
        public static BitArray GetBits(this RandomNumberGenerator randomNumberGenerator, int numberOfBits)
        {
            byte[] randomSource = new byte[NumberLength.FromBitLength(numberOfBits).InBytes];
            randomNumberGenerator.GetBytes(randomSource);
            return new BitArray(randomSource); // todo(lumip): only numberOfBits please..
        }

        /// <summary>
        /// Returns a random integer less than limit.
        /// </summary>
        public static int GetInteger(this RandomNumberGenerator randomNumberGenerator, int limit)
        {
            int bitsPerSample = NumberLength.GetLength(limit).InBits;
            int mask = 1 << (bitsPerSample - 1);

            byte[] randomBytes = new byte[4];
            int sample;
            do
            {
                randomNumberGenerator.GetBytes(randomBytes);
                sample = BitConverter.ToInt32(randomBytes, 0) ^ mask;
            } while (sample >= limit);

            return sample;

            // int bitsPerSample = NumberLength.GetLength(limit).InBits;
            // int totalBytes = NumberLength.FromBitLength(bitsPerSample * count).InBytes;

            // byte[] randomBytes = new byte[totalBytes];
            // randomNumberGenerator.GetBytes(randomBytes);
            // int[] results = 
        }
    }
}