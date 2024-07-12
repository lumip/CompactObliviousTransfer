// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using Xunit;
using System.Security.Cryptography;

namespace CompactOT.Primitives
{
    public class SHAProviderTests
    {

        [Fact]
        public void TestSHA256Provider()
        {
            HashAlgorithmProvider provider = new SHA256Provider();
            Assert.Equal(128, provider.SecurityLevel);
            Assert.IsAssignableFrom<SHA256>(provider.CreateHashAlgorithm());
        }

        [Fact]
        public void TestSHA512Provider()
        {
            HashAlgorithmProvider provider = new SHA512Provider();
            Assert.Equal(256, provider.SecurityLevel);
            Assert.IsAssignableFrom<SHA512>(provider.CreateHashAlgorithm());
        }
    }

}
