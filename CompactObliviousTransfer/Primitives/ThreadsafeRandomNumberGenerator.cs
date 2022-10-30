// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Cryptography;

namespace CompactOT
{
    /// <summary>
    /// Threadsafe wrapper for <see cref="RandomNumberGenerator"/> instances.
    /// </summary>
    public class ThreadsafeRandomNumberGenerator : RandomNumberGenerator
    {

        private RandomNumberGenerator _baseGenerator;

        public ThreadsafeRandomNumberGenerator(RandomNumberGenerator baseGenerator)
        {
            _baseGenerator = baseGenerator;
        }

        /// <inheritdoc/>
        public override void GetBytes(byte[] data)
        {
            lock (_baseGenerator)
            {
                _baseGenerator.GetBytes(data);
            }
        }
    }
}