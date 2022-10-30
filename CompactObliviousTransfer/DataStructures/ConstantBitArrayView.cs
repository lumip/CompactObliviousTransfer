// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Linq;

namespace CompactOT.DataStructures
{

    /// <summary>
    /// A bit array filled with a constant value.
    /// </summary>
    public class ConstantBitArrayView : EnumeratedBitArrayView
    {

        public static byte FillByteWith(Bit bit)
        {
            return (byte)(0 - (byte)bit);
        }

        public ConstantBitArrayView(Bit bit, int length)
            : base(Enumerable.Repeat(FillByteWith(bit), length), length) { }

        public static ConstantBitArrayView MakeOnes(int length) => new ConstantBitArrayView(Bit.One, length);
        public static ConstantBitArrayView MakeZeros(int length) => new ConstantBitArrayView(Bit.Zero, length);

    }

}