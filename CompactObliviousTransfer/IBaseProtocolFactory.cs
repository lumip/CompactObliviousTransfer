// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

namespace CompactOT
{

    public interface IBaseProtocolFactory
    {
        IObliviousTransferChannel MakeChannel(
            IMessageChannel channel, CryptoContext cryptoContext, int securityLevel
        );
    }
}
