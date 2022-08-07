using System;
using System.Diagnostics;
using System.Numerics;

using CompactCryptoGroupAlgebra;
using CompactCryptoGroupAlgebra.EllipticCurves;

namespace CompactOT
{

    public delegate double CostCalculationCallback(ObliviousTransferUsageProjection usageProjection);
    
    public class ObliviousTransferChannelBuilder
    {

        private ObliviousTransferUsageProjection _projection;

        public ObliviousTransferChannelBuilder()
        {
            _projection = new ObliviousTransferUsageProjection();
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

        private CryptoGroup<BigInteger, BigInteger> MakeCryptoGroup()
        {
            // TODO: return group based on security level
            return XOnlyMontgomeryCurveAlgebra.CreateCryptoGroup(CurveParameters.Curve25519);
        }

        public IObliviousTransferChannel MakeObliviousTransferChannel(
            IMessageChannel channel, CryptoContext? cryptoContext = null
        )
        {
            if (cryptoContext == null)
            {
                cryptoContext = CryptoContext.CreateDefault(); // TODO: returns SHA1; determine how that factors into security level
            }

            var cryptoGroup = MakeCryptoGroup();
            Debug.Assert(cryptoGroup.SecurityLevel >= _projection.SecurityLevel);

            var naorPinkasChannel = new StatelessObliviousTransferChannel(
                new NaorPinkasObliviousTransfer<BigInteger, BigInteger>(
                    cryptoGroup, cryptoContext
                ),
                channel
            );

            var extendedOtChannel = new ExtendedObliviousTransferChannel(
                naorPinkasChannel, _projection.SecurityLevel, cryptoContext
            );


            if (_projection.HasMaxNumberOfInvocations)
            {
                // bounded number of invocations, estimate actual cost and favour least costly protocol
                double naorPinkasCost = naorPinkasChannel.EstimateCost(_projection);
                double extendedOtCost = extendedOtChannel.EstimateCost(_projection);

                if (naorPinkasCost < extendedOtCost)
                {
                    return naorPinkasChannel;
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
            if (cryptoContext == null)
            {
                cryptoContext = CryptoContext.CreateDefault(); // TODO: returns SHA1; determine how that factors into security level
            }

            var cryptoGroup = MakeCryptoGroup();
            Debug.Assert(cryptoGroup.SecurityLevel >= _projection.SecurityLevel);

            var naorPinkasChannel = new StatelessObliviousTransferChannel(
                new NaorPinkasObliviousTransfer<BigInteger, BigInteger>(
                    cryptoGroup, cryptoContext
                ),
                channel
            );

            var alszCorrelatedChannel = new ALSZCorrelatedObliviousTransferChannel(
                naorPinkasChannel, _projection.SecurityLevel, cryptoContext
            );

            if (_projection.HasMaxNumberOfInvocations)
            {
                // bounded number of invocations, estimate actual cost and favour least costly protocol
                var naorPinkasCorrelatedChannel = new Adapters.CorrelatedFromStandardObliviousTransferChannel(
                    naorPinkasChannel, cryptoContext.RandomNumberGenerator
                );

                double naorPinkasCorrelatedCost = naorPinkasCorrelatedChannel.EstimateCost(
                    _projection
                );
                double alszCorrelatedCost = alszCorrelatedChannel.EstimateCost(
                    _projection
                );

                if (naorPinkasCorrelatedCost < alszCorrelatedCost)
                {
                    return naorPinkasCorrelatedChannel;
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
            if (cryptoContext == null)
            {
                cryptoContext = CryptoContext.CreateDefault(); // TODO: returns SHA1; determine how that factors into security level
            }

            var cryptoGroup = MakeCryptoGroup();
            Debug.Assert(cryptoGroup.SecurityLevel >= _projection.SecurityLevel);

            var naorPinkasChannel = new StatelessObliviousTransferChannel(
                new NaorPinkasObliviousTransfer<BigInteger, BigInteger>(
                    cryptoGroup, cryptoContext
                ),
                channel
            );

            var alszRandomChannel = new ALSZRandomObliviousTransferChannel(
                naorPinkasChannel, _projection.SecurityLevel, cryptoContext
            );

            if (_projection.HasMaxNumberOfInvocations)
            {
                // bounded number of invocations, estimate actual cost and favour least costly protocol
                var naorPinkasRandomChannel = new Adapters.RandomFromStandardObliviousTransferChannel(
                    naorPinkasChannel, cryptoContext.RandomNumberGenerator
                );
                
                double naorPinkasRandomCost = naorPinkasChannel.EstimateCost(
                    _projection
                );

                double alszRandomCost = alszRandomChannel.EstimateCost(
                    _projection
                );

                if (naorPinkasRandomCost < alszRandomCost)
                {
                    return naorPinkasRandomChannel;
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
