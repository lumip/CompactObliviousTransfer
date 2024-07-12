// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
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
    /// Uses cryptographic group implementations from the CompactCryptoGroupAlgebra
    /// library for cryptographic routines required by the Naor-Pinkas protocol.
    /// </summary>
    public class DefaultBaseProtocolFactory : IBaseProtocolFactory
    {
        /// <summary>
        /// Instantiates an oblivious transfer channels using the Naor-Pinkas protocol.
        /// 
        /// The underlying cryptography is implement using safe elliptic curves for
        /// security level up to 256 bits.
        /// </summary>
        /// <param name="channel">The <see cref="IMessageChannel"/>  over which all protocol communication is performed.</param>
        /// <param name="cryptoContext">The <see cref="CryptoContext"/> providing cryptographic primitives, such as a
        /// random number generator and a cryptographic hash function. The security level of this must be at
        /// least as high as the one requested for the returned oblivious transfer channel.</param>
        /// <param name="securityLevel">The desired security level for the returned oblivious transfer channel.</param>
        /// <returns>A <see cref="IObliviousTransferChannel"/> with at least the requested security level.</returns>
        public IObliviousTransferChannel MakeChannel(
            IMessageChannel channel, CryptoContext cryptoContext, int securityLevel
        )
        {
            if (cryptoContext.SecurityLevel < securityLevel)
                throw new ArgumentException($"The provided CryptoContext must have at least the security level requested for the channel ({securityLevel}), but only has {cryptoContext.SecurityLevel}.", nameof(cryptoContext));

            CryptoGroup<BigInteger, CurvePoint> cryptoGroup = CurveGroupAlgebra.CreateCryptoGroup(securityLevel);
            return new NaorPinkasObliviousTransferChannel<BigInteger, CurvePoint>(channel, cryptoGroup, cryptoContext);
        }
    }

}
