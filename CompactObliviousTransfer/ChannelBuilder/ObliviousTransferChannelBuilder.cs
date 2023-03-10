// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Diagnostics;

using CompactOT.Codes;

namespace CompactOT
{

    /// <summary>
    /// Allows to specify required specifications for an oblivious transfer channel and constructs and optimal OT channel instance.
    /// 
    /// Required specifications include e.g.,
    ///     - the required security parameter,
    ///     - the maximum and expected number of message options,
    ///     - the maximum number of total invocations
    ///     - the expected number of invocations that are executed in a single batch (call to Send/ReceiveAsync),
    ///     - the expected number of bits in a message.
    /// Based on this information, the ObliviousTransferChannelBuilder will primarily decide whether to instantiate
    /// a basic or extended oblivous transfer protocol channel, depending on which is expected to result in less communication cost.
    /// 
    /// If no requirements are given, ObliviousTransferChannelBuilder will default to a security level of 128 and an unlimited
    /// number of total invocations with an expected single invocation per call to Send/ReceiveAsync. The number of expected message
    /// options defaults to 2 and the expected number of bits in a message to 1.
    /// 
    /// Note that ObliviousTransferChannelBuilder only guarantees optimality for the returned channel object when used within the
    /// requirements it was provided. In some cases, it may be possible to use the returned channel object outside of the specification
    /// provided to the builder, e.g., by exceeding the maximum number of total invocations, without encountering a failure/exception.
    /// However, in this case optimality with respect to communication cost is no longer guaranteed.
    /// </summary>
    public class ObliviousTransferChannelBuilder
    {

        private ObliviousTransferUsageProjection _projection;
        private IBaseProtocolFactory _baseOtFactory;

        public ObliviousTransferChannelBuilder()
        {
            _projection = new ObliviousTransferUsageProjection();
            _baseOtFactory = new DefaultBaseProtocolFactory();
        }

        public ObliviousTransferChannelBuilder WithMaximumNumberOfOptions(int maxNumberOfOptions)
        {
            _projection.MaxNumberOfOptions = maxNumberOfOptions;
            return this;
        }

        public ObliviousTransferChannelBuilder WithAverageNumberOfOptions(int averageNumberOfOptions)
        {
            _projection.AverageNumberOfOptions = averageNumberOfOptions;
            return this;
        }

        public ObliviousTransferChannelBuilder WithMaximumNumberOfInvocations(int maxNumberOfInvocations)
        {
            _projection.MaxNumberOfInvocations = maxNumberOfInvocations;
            return this;
        }

        public ObliviousTransferChannelBuilder WithMaximumNumberOfBatches(int maxNumberOfBatches)
        {
            _projection.MaxNumberOfBatches = maxNumberOfBatches;
            return this;
        }

        public ObliviousTransferChannelBuilder WithAverageInvocationsPerBatch(int averageInvocationsPerBatch)
        {
            _projection.AverageInvocationsPerBatch = averageInvocationsPerBatch;
            return this;
        }

        public ObliviousTransferChannelBuilder WithSecurityLevel(int securityLevel)
        {
            _projection.SecurityLevel = securityLevel;
            return this;
        }

        public ObliviousTransferChannelBuilder WithCustomBaseProtocol(IBaseProtocolFactory baseProtocolFactory)
        {
            _baseOtFactory = baseProtocolFactory;
            return this;
        }

        private CryptoContext MakeCryptoContext(
            CryptoContext? cryptoContext
        )
        {
            if (cryptoContext == null)
            {
                cryptoContext = CryptoContext.CreateWithSecurityLevel(_projection.SecurityLevel);
            }
            else
            {
                if (cryptoContext.HashAlgorithmProvider.SecurityLevel < _projection.SecurityLevel)
                {
                    throw new ArgumentException(
                        $"Hash algorithm in the provided crypto context cannot satisfy required security level {_projection.SecurityLevel}." +
                        " The length of the hash must be at least twice the security level.",
                        nameof(cryptoContext)
                    );
                }
            }
            return cryptoContext;
        }

        private IBinaryCode MakeBinaryCode()
        {
            IBinaryCode code = WalshHadamardCode.CreateWithDistance(_projection.SecurityLevel);
            if (_projection.HasMaxNumberOfOptions)
            {
                if (_projection.MaxNumberOfOptions == 2)
                {
                    code = RepeatingBitCode.CreateWithDistance(_projection.SecurityLevel);
                }
                else if (code.MaximumMessage < _projection.MaxNumberOfOptions - 1)
                {
                    code = WalshHadamardCode.CreateWithMaximumMessage(_projection.MaxNumberOfOptions - 1);
                    Debug.Assert(code.Distance >= _projection.SecurityLevel);
                }

            }
            return code;
        }

        private T SelectBaseOrExtendedChannel<T>(T baseProtocolChannel, T extendedOtChannel) where T : ICostEstimator
        {
            if (_projection.HasMaxNumberOfInvocations)
            {
                // bounded number of invocations, estimate actual cost and favour least costly protocol
                double pureBaseProtocolCost = baseProtocolChannel.EstimateCost(_projection);
                double extendedOtCost = extendedOtChannel.EstimateCost(_projection);

                if (pureBaseProtocolCost < extendedOtCost)
                {
                    return baseProtocolChannel;
                }
            }
            // Here either we have determined that for the given number of invocations extended OT is less costly,
            // or the maximum number of invocations is unbounded. Since extended OT is less costly asymptotically,
            // we prefer it in the latter case as well. Therefore, we return the extended OT channel here.
            return extendedOtChannel;
        }

        public IObliviousTransferChannel MakeObliviousTransferChannel(
            IMessageChannel channel, CryptoContext? cryptoContext = null
        )
        {
            cryptoContext = MakeCryptoContext(cryptoContext);
            var code = MakeBinaryCode();

            var baseProtocolChannel = _baseOtFactory.MakeChannel(
                channel, cryptoContext, _projection.SecurityLevel
            );

            var extendedOtChannel = new ExtendedObliviousTransferChannel(
                baseProtocolChannel, _projection.SecurityLevel, cryptoContext, code
            );

            return SelectBaseOrExtendedChannel<IObliviousTransferChannel>(baseProtocolChannel, extendedOtChannel);
        }

        public ICorrelatedObliviousTransferChannel MakeCorrelatedObliviousTransferChannel(
            IMessageChannel channel, CryptoContext? cryptoContext = null
        )
        {
            cryptoContext = MakeCryptoContext(cryptoContext);
            var code = MakeBinaryCode();

            var baseProtocolChannel = _baseOtFactory.MakeChannel(
                channel, cryptoContext, _projection.SecurityLevel
            );

            var baseProtocolCorrelatedChannel = new Adapters.CorrelatedFromStandardObliviousTransferChannel(
                baseProtocolChannel, cryptoContext.RandomNumberGenerator
            );

            var alszCorrelatedChannel = new CorrelatedObliviousTransferChannel(
                baseProtocolChannel, _projection.SecurityLevel, cryptoContext, code
            );

            return SelectBaseOrExtendedChannel<ICorrelatedObliviousTransferChannel>(baseProtocolCorrelatedChannel, alszCorrelatedChannel);
        }

        public IRandomObliviousTransferChannel MakeRandomObliviousTransferChannel(
            IMessageChannel channel, CryptoContext? cryptoContext = null
        )
        {
            cryptoContext = MakeCryptoContext(cryptoContext);
            var code = MakeBinaryCode();

            var baseProtocolChannel = _baseOtFactory.MakeChannel(
                channel, cryptoContext, _projection.SecurityLevel
            );

            var baseProtocolRandomChannel = new Adapters.RandomFromStandardObliviousTransferChannel(
                baseProtocolChannel, cryptoContext.RandomNumberGenerator
            );

            var alszRandomChannel = new RandomObliviousTransferChannel(
                baseProtocolChannel, _projection.SecurityLevel, cryptoContext, code
            );

            return SelectBaseOrExtendedChannel<IRandomObliviousTransferChannel>(baseProtocolRandomChannel, alszRandomChannel);
        }

    }
}
