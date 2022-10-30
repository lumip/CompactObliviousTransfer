// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Cryptography;

namespace CompactOT
{
    public interface HashAlgorithmProvider
    {
        HashAlgorithm CreateHashAlgorithm();

        int SecurityLevel { get; }
    }
}
