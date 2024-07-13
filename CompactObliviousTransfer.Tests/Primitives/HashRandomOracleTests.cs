// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

using Xunit;

namespace CompactOT.Primitives
{
    public class HashRandomOracleTests
    {

        [Fact]
        public void TestInvokeForEnumerator()
        {
            var hashProvider = new SHA256Provider();
            var oracle = new HashRandomOracle(hashProvider);

            byte[] query = new byte[] { 1, 0 };

            var enumerator = oracle.InvokeForEnumerator(query);

            var hashAlgorithm = hashProvider.CreateHashAlgorithm();

            byte[] seed = hashAlgorithm.ComputeHash(query);

            byte[] innerQuery = new byte[seed.Length + 4];
            Buffer.BlockCopy(seed, 0, innerQuery, 0, seed.Length);
            innerQuery[^4] = 0;
            innerQuery[^3] = 0;
            innerQuery[^2] = 0;
            innerQuery[^1] = 0;

            byte[] expectedValues = hashAlgorithm.ComputeHash(innerQuery);

            for (int i = 0; i < expectedValues.Length; i++)
            {
                enumerator.MoveNext();
                Assert.Equal(expectedValues[i], enumerator.Current);
            }

            innerQuery[^4] = 1;
            expectedValues = hashAlgorithm.ComputeHash(innerQuery);

            for (int i = 0; i < expectedValues.Length; i++)
            {
                enumerator.MoveNext();
                Assert.Equal(expectedValues[i], enumerator.Current);
            }
        }

        [Fact]
        public void TestInvoke()
        {
            var hashProvider = new SHA256Provider();
            var oracle = new HashRandomOracle(hashProvider);

            byte[] query = new byte[] { 1, 0 };

            var randomByteSequence = oracle.Invoke(query);
            var enumerator = randomByteSequence.Enumerator;

            var hashAlgorithm = hashProvider.CreateHashAlgorithm();

            byte[] seed = hashAlgorithm.ComputeHash(query);

            byte[] innerQuery = new byte[seed.Length + 4];
            Buffer.BlockCopy(seed, 0, innerQuery, 0, seed.Length);
            innerQuery[^4] = 0;
            innerQuery[^3] = 0;
            innerQuery[^2] = 0;
            innerQuery[^1] = 0;

            byte[] expectedValues = hashAlgorithm.ComputeHash(innerQuery);

            for (int i = 0; i < expectedValues.Length; i++)
            {
                enumerator.MoveNext();
                Assert.Equal(expectedValues[i], enumerator.Current);
            }

            innerQuery[^4] = 1;
            expectedValues = hashAlgorithm.ComputeHash(innerQuery);

            for (int i = 0; i < expectedValues.Length; i++)
            {
                enumerator.MoveNext();
                Assert.Equal(expectedValues[i], enumerator.Current);
            }
        }

    }

}
