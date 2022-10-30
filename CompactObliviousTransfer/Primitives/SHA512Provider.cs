// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Cryptography;

namespace CompactOT
{
    public class SHA512Provider : HashAlgorithmProvider
    {
        public int SecurityLevel => 256;

        public HashAlgorithm CreateHashAlgorithm()
        {
            return SHA512.Create();
        }
    }
}
