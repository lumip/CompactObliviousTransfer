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


            if (_projection.HasMaxNumberOfInvocations)
            {
                // bounded number of invocations, estimate actual cost and favour least costly protocol
                double cryptoGroupElementSize = (double)cryptoGroup.ElementLength.InBits;
                CostCalculationCallback naorPinkasCostCallback = (ObliviousTransferUsageProjection p) => 
                    NaorPinkasObliviousTransfer<BigInteger, BigInteger>.EstimateCost(
                        p, cryptoGroupElementSize
                    );

                double naorPinkasCost = naorPinkasCostCallback(_projection);

                double extendedOtCost = ExtendedObliviousTransferChannel.EstimateCost(
                    _projection, naorPinkasCostCallback
                );

                if (naorPinkasCost < extendedOtCost)
                {
                    return naorPinkasChannel;
                }
            }
            // Here either we have determined that for the given number of invocations extended OT is less costly,
            // or the maximum number of invocations is unbounded. Since extended OT is less costly asymptotically,
            // we prefer it in the latter case as well. Therefore, we return the extended OT channel here.
            return new ExtendedObliviousTransferChannel(
                naorPinkasChannel, _projection.SecurityLevel, cryptoContext
            );
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

            if (_projection.HasMaxNumberOfInvocations)
            {
                // bounded number of invocations, estimate actual cost and favour least costly protocol
                double cryptoGroupElementSize = (double)cryptoGroup.ElementLength.InBits;
                CostCalculationCallback naorPinkasCostCallback = (ObliviousTransferUsageProjection p) => 
                    NaorPinkasObliviousTransfer<BigInteger, BigInteger>.EstimateCost(
                        p, cryptoGroupElementSize
                    );

                double naorPinkasCorrelatedCost = Adapters.CorrelatedFromStandardObliviousTransferChannel.EstimateCost(
                    _projection, naorPinkasCostCallback
                );

                double alszCorrelatedOtCost = ALSZCorrelatedObliviousTransferChannel.EstimateCost(
                    _projection, naorPinkasCostCallback
                );

                if (naorPinkasCorrelatedCost < alszCorrelatedOtCost)
                {
                    return new Adapters.CorrelatedFromStandardObliviousTransferChannel(
                        naorPinkasChannel, cryptoContext.RandomNumberGenerator
                    );
                }
            }
            // Here either we have determined that for the given number of invocations extended OT is less costly,
            // or the maximum number of invocations is unbounded. Since extended OT is less costly asymptotically,
            // we prefer it in the latter case as well. Therefore, we return the extended OT channel here.
            return new ALSZCorrelatedObliviousTransferChannel(
                naorPinkasChannel, _projection.SecurityLevel, cryptoContext
            );
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

            if (_projection.HasMaxNumberOfInvocations)
            {
                // bounded number of invocations, estimate actual cost and favour least costly protocol
                double cryptoGroupElementSize = (double)cryptoGroup.ElementLength.InBits;
                CostCalculationCallback naorPinkasCostCallback = (ObliviousTransferUsageProjection p) => 
                    NaorPinkasObliviousTransfer<BigInteger, BigInteger>.EstimateCost(
                        p, cryptoGroupElementSize
                    );

                double naorPinkasCorrelatedCost = Adapters.RandomFromStandardObliviousTransferChannel.EstimateCost(
                    _projection, naorPinkasCostCallback
                );

                double alszCorrelatedOtCost = ALSZRandomObliviousTransferChannel.EstimateCost(
                    _projection, naorPinkasCostCallback
                );

                if (naorPinkasCorrelatedCost < alszCorrelatedOtCost)
                {
                    return new Adapters.RandomFromStandardObliviousTransferChannel(
                        naorPinkasChannel, cryptoContext.RandomNumberGenerator
                    );
                }
            }
            // Here either we have determined that for the given number of invocations extended OT is less costly,
            // or the maximum number of invocations is unbounded. Since extended OT is less costly asymptotically,
            // we prefer it in the latter case as well. Therefore, we return the extended OT channel here.
            return new ALSZRandomObliviousTransferChannel(
                naorPinkasChannel, _projection.SecurityLevel, cryptoContext
            );
        }

        // TODO: think about making EstimateCost callback non-static. It seems the benefit of having it be static
        // are fairly limited (we instantiate all classes in most cases above anyways), and it would avoid passing around
        // callbacks.
    }
}
