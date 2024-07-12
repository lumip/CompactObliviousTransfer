// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{

    public class ByteToBitEnumerable : BaseEnumerable<Bit>
    {
        private IEnumerable<byte> _byteEnumerable;
        private int _numberOfBits;

        public ByteToBitEnumerable(IEnumerable<byte> byteEnumerable, int numberOfBits)
        {
            if (numberOfBits < 0)
                throw new ArgumentOutOfRangeException("Number of bits cannot be negative.", nameof(numberOfBits));
            _numberOfBits = numberOfBits;
            _byteEnumerable = byteEnumerable;
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return new ByteToBitEnumerator(_byteEnumerable.GetEnumerator(), _numberOfBits);
        }

    }

}
