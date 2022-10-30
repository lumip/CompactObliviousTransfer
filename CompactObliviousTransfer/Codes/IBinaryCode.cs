// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using CompactOT.DataStructures;

namespace CompactOT.Codes
{
    public interface IBinaryCode
    {
        public BitSequence Encode(int x);

        public int CodeLength { get; }
        public int Distance { get; }
        public int MaximumMessage { get; }
    }

}
