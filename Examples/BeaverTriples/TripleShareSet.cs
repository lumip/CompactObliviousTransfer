// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

using CompactOT;
using CompactOT.DataStructures;


namespace CompactOT.Examples.BeaverTriples
{

    class TripleShareSet
    {
        public BitMatrix FirstFactorShare { get; }
        public BitMatrix SecondFactorShare { get; }
        public BitMatrix ProductShare { get; }

        public TripleShareSet(BitMatrix firstFactorShare, BitMatrix secondFactorShare, BitMatrix productShare)
        {
            if (firstFactorShare.Rows != secondFactorShare.Rows || secondFactorShare.Rows != productShare.Rows ||
                firstFactorShare.Cols != secondFactorShare.Cols || secondFactorShare.Cols != productShare.Cols)
            {
                throw new ArgumentException("All inputs must have same dimensions.");
            }

            FirstFactorShare = firstFactorShare;
            SecondFactorShare = secondFactorShare;
            ProductShare = productShare;
        }

        public int NumberOfTriples => FirstFactorShare.Rows;
        public int NumberOfTripleBits => FirstFactorShare.Cols;

        public (BitSequence, BitSequence, BitSequence) GetTripleShare(int i)
        {
            if (i < 0 || i >= NumberOfTriples)
                throw new ArgumentOutOfRangeException(nameof(i));

            return (FirstFactorShare.GetRow(i), SecondFactorShare.GetRow(i), ProductShare.GetRow(i));
        }
    }

}
