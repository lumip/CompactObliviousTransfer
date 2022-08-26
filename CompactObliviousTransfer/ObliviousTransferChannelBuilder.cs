using System;
using System.Diagnostics;

using CompactOT.Codes;

namespace CompactOT
{

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
                if (cryptoContext.HashAlgorithm.HashSize / 2 < _projection.SecurityLevel)
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
                else if (code.MaximumMessage >= _projection.MaxNumberOfOptions)
                {
                    code = WalshHadamardCode.CreateWithMaximumMessage(_projection.MaxNumberOfOptions - 1);
                    Debug.Assert(code.Distance >= _projection.SecurityLevel);
                }

            }
            return code;
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

        public ICorrelatedObliviousTransferChannel MakeCorrelatedObliviousTransferChannel(
            IMessageChannel channel, CryptoContext? cryptoContext = null
        )
        {
            cryptoContext = MakeCryptoContext(cryptoContext);
            var code = MakeBinaryCode();

            var baseProtocolChannel = _baseOtFactory.MakeChannel(
                channel, cryptoContext, _projection.SecurityLevel
            );

            var alszCorrelatedChannel = new ALSZCorrelatedObliviousTransferChannel(
                baseProtocolChannel, _projection.SecurityLevel, cryptoContext, code
            );

            if (_projection.HasMaxNumberOfInvocations)
            {
                // bounded number of invocations, estimate actual cost and favour least costly protocol
                var baseProtocolCorrelatedChannel = new Adapters.CorrelatedFromStandardObliviousTransferChannel(
                    baseProtocolChannel, cryptoContext.RandomNumberGenerator
                );

                double pureBaseProtocolCost = baseProtocolCorrelatedChannel.EstimateCost(
                    _projection
                );
                double alszCorrelatedCost = alszCorrelatedChannel.EstimateCost(
                    _projection
                );

                if (pureBaseProtocolCost < alszCorrelatedCost)
                {
                    return baseProtocolCorrelatedChannel;
                }
            }
            // Here either we have determined that for the given number of invocations extended OT is less costly,
            // or the maximum number of invocations is unbounded. Since extended OT is less costly asymptotically,
            // we prefer it in the latter case as well. Therefore, we return the extended OT channel here.
            return alszCorrelatedChannel;
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

            var alszRandomChannel = new ALSZRandomObliviousTransferChannel(
                baseProtocolChannel, _projection.SecurityLevel, cryptoContext, code
            );

            if (_projection.HasMaxNumberOfInvocations)
            {
                // bounded number of invocations, estimate actual cost and favour least costly protocol
                var baseProtocolRandomChannel = new Adapters.RandomFromStandardObliviousTransferChannel(
                    baseProtocolChannel, cryptoContext.RandomNumberGenerator
                );
                
                double pureBaseProtocolCost = baseProtocolChannel.EstimateCost(
                    _projection
                );

                double alszRandomCost = alszRandomChannel.EstimateCost(
                    _projection
                );

                if (pureBaseProtocolCost < alszRandomCost)
                {
                    return baseProtocolRandomChannel;
                }
            }
            // Here either we have determined that for the given number of invocations extended OT is less costly,
            // or the maximum number of invocations is unbounded. Since extended OT is less costly asymptotically,
            // we prefer it in the latter case as well. Therefore, we return the extended OT channel here.
            return alszRandomChannel;
        }

        // TODO: can reduce duplicated logic in the above using a generic-typed method?

    }
}
