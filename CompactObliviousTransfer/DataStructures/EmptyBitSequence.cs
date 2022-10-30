// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Numerics;

namespace CompactOT.DataStructures
{

    public class EmptyBitSequence : BitSequence
    {
        public override int Length => 0;

        public override bool IsSynchronized => true;

        public override object? SyncRoot => this;

        public override bool IsReadOnly => true;

        public override IEnumerable<byte> AsByteEnumerable()
        {
            return Enumerable.Empty<byte>();
        }

        public override IEnumerator<Bit> GetEnumerator()
        {
            return Enumerable.Empty<Bit>().GetEnumerator();
        }
    }
    
}
