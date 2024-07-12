// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections;
using System.Collections.Generic;

namespace CompactOT.DataStructures
{

    public class BitToByteEnumerable : BaseEnumerable<byte>
    {
        private IEnumerable<Bit> _bitEnumerable;

        public BitToByteEnumerable(IEnumerable<Bit> bitEnumerable)
        {
            _bitEnumerable = bitEnumerable;
        }

        public override IEnumerator<byte> GetEnumerator()
        {
            return new BitToByteEnumerator(_bitEnumerable.GetEnumerator());
        }
        
    }
    
}