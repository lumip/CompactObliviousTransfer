// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;

using CompactOT.DataStructures;

namespace CompactOT
{
    public class ObliviousTransferResult : BitMatrix
    {
        public ObliviousTransferResult(int numberOfInvocations, int numberOfMessageBits)
            : base(numberOfInvocations, numberOfMessageBits)
        {
        }

        public int NumberOfInvocations => base.Rows;
        public int NumberOfMessageBits => base.Cols;

        public BitSequence GetInvocationResult(int i) => base.GetRow(i);

        public IEnumerable<BitSequence> AsBitSequences()
        {
            for (int i = 0; i < NumberOfInvocations; ++i)
            {
                yield return GetInvocationResult(i);
            }
        }

        public byte[][] AsByteArrays()
        {
            return AsBitSequences().Select(bs => bs.AsByteEnumerable().ToArray()).ToArray();
        }
    }
}
