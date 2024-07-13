// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Security.Cryptography;

namespace CompactOT
{
    /// <summary>
    /// Used only for testing that Dispose is correctly called in CryptoContext.
    /// </summary>
    public class TestRandomNumberGenerator : RandomNumberGenerator
    {
        public bool Disposed { get; private set; }

        public override void GetBytes(byte[] data)
        {
            throw new NotImplementedException();
        }

        public TestRandomNumberGenerator() : base()
        {
            Disposed = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            Disposed = true;
        }
    }
}
