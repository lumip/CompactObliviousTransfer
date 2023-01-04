// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Diagnostics;
using System.Numerics;

using CompactCryptoGroupAlgebra;
using CompactCryptoGroupAlgebra.EllipticCurves;
using CompactCryptoGroupAlgebra.Multiplicative;

namespace CompactOT
{

    /// <summary>
    /// Instantiates oblivious transfer channels using the Naor-Pinkas protocol.
    /// 
    /// Uses cryptographic group implementations used by the Naor-Pinkas protocol from
    /// the CompactCryptoGroupAlgebra library: For security levels below 256 bits,
    /// safe elliptic curves are used; for higher security levels for which no curves
    /// are provided, a multiplicative group of safe-prime characteristic with appropriate size
    /// is randomly generated (this may take a long time).
    /// </summary>
    public class DefaultBaseProtocolFactory : IBaseProtocolFactory
    {
        public IObliviousTransferChannel MakeChannel(
            IMessageChannel channel, CryptoContext cryptoContext, int securityLevel
        )
        {
            Debug.Assert(cryptoContext.SecurityLevel >= securityLevel);
            try
            {
                CryptoGroup<BigInteger, CurvePoint> cryptoGroup = CurveGroupAlgebra.CreateCryptoGroup(securityLevel);
                return new NaorPinkasObliviousTransferChannel<BigInteger, CurvePoint>(channel, cryptoGroup, cryptoContext);
            }
            catch (ArgumentOutOfRangeException)
            {
                CryptoGroup<BigInteger, BigInteger> cryptoGroup = MultiplicativeGroupAlgebra.CreateCryptoGroup(
                    securityLevel, cryptoContext.RandomNumberGenerator
                );
                return new NaorPinkasObliviousTransferChannel<BigInteger, BigInteger>(channel, cryptoGroup, cryptoContext);
            }
        }
    }

}
