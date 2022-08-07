using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

using CompactCryptoGroupAlgebra;

using CompactOT.DataStructures;
using CompactOT.Buffers;

namespace CompactOT
{

    /// <summary>
    /// Commmon base implementation of the OT extension protocol and its random and correlated variants.
    /// 
    /// It provides an implemention of the base OT invocations as well as the common first steps of the online
    /// phase of the protocol (up until the matrix U is sent from receiver to sender).
    /// </summary>
    public class ExtendedObliviousTransferChannelBase
    {

        private IObliviousTransferChannel _baseOT;

        private NumberLength _securityParameter;
        public int SecurityLevel => _securityParameter.InBits;
        protected int CodeLength => 2 * SecurityLevel;

        protected RandomOracle RandomOracle { get; }
        
        /// <summary>
        /// Internal encapsulation of the persistent state for the sender role.
        /// </summary>
        protected class SenderState
        {
            public RandomByteSequence[] SeededRandomOracles;
            public BitArray RandomChoices;

            public SenderState(int stateSize, int numberOfOptions)
            {
                SeededRandomOracles = new RandomByteSequence[stateSize];
                RandomChoices = new BitArray(stateSize);
            }
        };
        protected SenderState? _senderState;

        /// <summary>
        /// Internal encapsulation of the persistent state for the receiver role.
        /// </summary>
        protected class ReceiverState
        {
            public RandomByteSequence[,] SeededRandomOracles;

            public ReceiverState(int stateSize, int numberOfOptions)
            {
                SeededRandomOracles = new RandomByteSequence[stateSize, numberOfOptions];
            }

        }
        protected ReceiverState? _receiverState;

        public IMessageChannel Channel => _baseOT.Channel;
        protected RandomNumberGenerator RandomNumberGenerator { get; private set; }

        /// <summary>
        /// The total number of OT invocations that have been performed on this channel so far.
        /// 
        /// Every call to Send/Receive will advance this by the number of invocations requested for that call,
        /// even if the call should fail.
        /// </summary>
        public int TotalNumberOfInvocations { get; private set; }

        public ExtendedObliviousTransferChannelBase(IObliviousTransferChannel baseOT, int securityParameter, CryptoContext cryptoContext)
        {
            if (securityParameter < 1)
            {
                throw new ArgumentOutOfRangeException(
                    $"Security level must not be less than 1, was {securityParameter}",
                    nameof(securityParameter)
                );
            }

            _baseOT = baseOT;
            if (_baseOT.SecurityLevel < securityParameter)
            {
                throw new ArgumentException(
                    $"The provided base OT must provided at least the requested security level of "+
                    $"{securityParameter} but only provides {baseOT.SecurityLevel}.", nameof(baseOT)
                );
            }

            RandomNumberGenerator = new ThreadsafeRandomNumberGenerator(cryptoContext.RandomNumberGenerator);
            RandomOracle = new HashRandomOracle(cryptoContext.HashAlgorithm);
            _securityParameter = NumberLength.FromBitLength(securityParameter);
            _senderState = null;
            _receiverState = null;
            TotalNumberOfInvocations = 0;
        }

        /// <summary>
        /// Performs 2k many 1-out-of-2 OTs on k bits for the sender, where k is the security parameter, using the base OT implementation.
        /// 
        /// These are subsequently expanded into m many 1ooN OTs on arbitrarily long messages
        /// by the SendAsync method, where m is only bounded by the amount of secure randomness the random
        /// oracle implementation can produce and N must be smaller than 2k.
        /// </summary>
        public async Task ExecuteSenderBaseTransferAsync()
        {
            int numBaseOTOptions = 2;
            _senderState = new SenderState(CodeLength, numBaseOTOptions);
            _senderState.RandomChoices = RandomNumberGenerator.GetBits(CodeLength);

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
            DebugUtils.WriteLineSender("ExtendedOT", $"Performing base transfers ({CodeLength} times {_securityParameter.InBits} bits).");
#endif
            // retrieve seeds for OT extension via _securityParameter many base OTs
            ObliviousTransferResult seeds = await _baseOT.ReceiveAsync(
                _senderState.RandomChoices.ToSelectionIndices().ToArray(),
                numBaseOTOptions,
                numberOfMessageBits: _securityParameter.InBits
            );
#if DEBUG
            DebugUtils.WriteLineSender("ExtendedOT", "Base transfers completed after {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            if (seeds.NumberOfInvocations != CodeLength)
            {
                throw new ProtocolException("Base transfer received unexpected number of invocations!");
            }
            if (seeds.NumberOfMessageBits != _securityParameter.InBits)
            {
                throw new ProtocolException("Base transfer received messages with unexpected lengths!");
            }

            // initializing a random oracle based on each seed
            for (int k = 0; k < CodeLength; ++k)
            {
                _senderState.SeededRandomOracles[k] = RandomOracle.Invoke(seeds.GetInvocationResult(k).AsByteEnumerable());
            }
        }

        /// <summary>
        /// Performs 2k many 1-out-of-2 OTs on k bits for the receiver, where k is the security parameter, using the base OT implementation.
        /// 
        /// These are subsequently expanded into m many 1ooN OTs on arbitrarily long messages
        /// by the SendAsync method, where m is only bounded by the amount of secure randomness the random
        /// oracle implementation can produce and N must be smaller than 2k.
        /// </summary>
        public async Task ExecuteReceiverBaseTransferAsync()
        {
            int numBaseOTOptions = 2;
            _receiverState = new ReceiverState(CodeLength, numBaseOTOptions);

            // generating _securityParameter many pairs of random seeds of length _securityParameter
            var seeds = ObliviousTransferOptions.CreateRandom(CodeLength, numBaseOTOptions, _securityParameter.InBits, RandomNumberGenerator);

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
            DebugUtils.WriteLineReceiver("ExtendedOT", $"Performing base transfers ({CodeLength} times {_securityParameter.InBits} bits).");
#endif

            // base OTs as _sender_ with the seeds as inputs
            Task sendTask = _baseOT.SendAsync(seeds);

            // initializing a random oracle based on each seed
            for (int k = 0; k < CodeLength; ++k)
            {
                _receiverState.SeededRandomOracles[k, 0] = RandomOracle.Invoke(seeds.GetMessage(k, 0).AsByteEnumerable());
                _receiverState.SeededRandomOracles[k, 1] = RandomOracle.Invoke(seeds.GetMessage(k, 1).AsByteEnumerable());
            };

            await sendTask;
#if DEBUG
            DebugUtils.WriteLineReceiver("ExtendedOT", "Base transfers completed after {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
        }

        /// <summary>
        /// Performs initial steps of the OT extension protocol online phase as the sender and returns matrix Q.
        /// </summary>
        /// <param name="numberOfInvocations">The number of invocations/instances of OT, i.e., how many separate messages the receiver will obtain.</param>
        /// <param name="numberOfOptions">The number of options the receiver can choose from, per message/invocation/instance.</param>
        /// <param name="numberOfMessageBits">The length of each message, in bits.</param>
        /// <returns>The bit matrix T_0 with numberOfInvocations rows and CodeLength columns.</returns>
        protected async Task<BitMatrix> SenderReceiveUAndComputeQ(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            if (numberOfOptions > CodeLength)
            {
                throw new ArgumentException($"Extended Oblivious Transfer with security level {_securityParameter.InBits} requires " +
                    $"the number of options to be less than {CodeLength}; was {numberOfOptions}", nameof(numberOfOptions));
            }
            if (_senderState == null) await ExecuteSenderBaseTransferAsync();

            TotalNumberOfInvocations += numberOfInvocations;

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            BitMatrix us = await ReceiveReceiverMessage(numberOfInvocations);
#if DEBUG
            DebugUtils.WriteLineSender("ExtendedOT", "Receiving U took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif
            Debug.Assert(us.Cols == CodeLength);
            Debug.Assert(us.Rows == numberOfInvocations);
            Debug.Assert(_senderState!.RandomChoices.Length == CodeLength);

            BitMatrix qs = new BitMatrix(numberOfInvocations, CodeLength);
            for (int k = 0; k < CodeLength; ++k)
            {
                var u = us.GetColumn(k);
                var q = u & _senderState!.RandomChoices[k];
                q = q ^ _senderState!.SeededRandomOracles[k].GetBits(numberOfInvocations);
                qs.SetColumn(k, q);
            }
#if DEBUG
            DebugUtils.WriteLineSender("ExtendedOT", "Computing Q took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif

            return qs;
        }

        /// <summary>
        /// Performs initial steps of the OT extension protocol online phase and returns the matrix T_0.
        /// </summary>
        /// <param name="selectionIndices">The indices indicating which option the receiver requests for each message/invocation/instance.</param>
        /// <param name="numberOfOptions">The number of options the receiver can choose from, per message/invocation/instance.</param>
        /// <param name="numberOfMessageBits">The length of each message, in bits.</param>
        /// <returns>The bit matrix T_0 with CodeLength rows and numberOfInvocations columns.</returns>
        protected async Task<BitMatrix> ReceiverComputeAndSendU(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            if (numberOfOptions > CodeLength)
            {
                throw new ArgumentException($"Extended Oblivious Transfer with security level {_securityParameter.InBits} requires " +
                    $"the number of options to be less than {CodeLength}; was {numberOfOptions}", nameof(numberOfOptions));
            }
            if (_receiverState == null) await ExecuteReceiverBaseTransferAsync();
            
            int numberOfInvocations = selectionIndices.Length;
            TotalNumberOfInvocations += numberOfInvocations;

            NumberLength optionLength = NumberLength.GetLength(numberOfOptions);

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            BitMatrix[] ts = new BitMatrix[] {
                new BitMatrix(CodeLength, numberOfInvocations),
                new BitMatrix(CodeLength, numberOfInvocations)
            };

            for (int k = 0; k < CodeLength; ++k)
            {
                ts[0].SetRow(k, _receiverState!.SeededRandomOracles[k, 0].GetBits(numberOfInvocations));
                ts[1].SetRow(k, _receiverState!.SeededRandomOracles[k, 1].GetBits(numberOfInvocations));
            }

            BitMatrix us = new BitMatrix(numberOfInvocations, CodeLength);

            for (int j = 0; j < numberOfInvocations; ++j)
            {
                var t0Col = ts[0].GetColumn(j);
                var t1Col = ts[1].GetColumn(j);

                var selectionCode = WalshHadamardCode.ComputeWalshHadamardCode(selectionIndices[j], CodeLength);
                Debug.Assert(t0Col.Length == CodeLength);
                Debug.Assert(t1Col.Length == CodeLength);
                Debug.Assert(selectionCode.Length == CodeLength);

                var row = t0Col ^ t1Col ^ selectionCode;
                us.SetRow(j, row);
            }
#if DEBUG
            DebugUtils.WriteLineReceiver("ExtendedOT", "Generating random Ts and U took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif      
            Task sendingTask = SendReceiverMessage(us);

            return ts[0];
        }

        /// <summary>
        /// Masks a message option (i.e., a sender input message).
        /// </summary>
        /// <param name="option">Raw bits of the sender's input (message option).</param>
        /// <param name="mask">Raw bits that are expanded into a random sequence masking the message option.</param>
        /// <param name="invocationIndex">Number of the invocation which the message option is part of.</param>
        /// <returns></returns>
        protected BitSequence MaskOption(BitSequence option, BitSequence mask, int invocationIndex)
        {
            var query = BufferBuilder.Empty.With(mask).With(invocationIndex).Create();
            return new EnumeratedBitArrayView(RandomOracle.Mask(option.AsByteEnumerable(), query), option.Length);
        }

        /// <summary>
        /// Creates a message option at random.
        /// </summary>
        /// <param name="numberOfBits">Bit length of a message.</param>
        /// <param name="mask">Raw bits that are expanded into a random sequence.</param>
        /// <param name="invocationIndex">Number of the invocation which the message option is part of.</param>
        /// <returns></returns>
        protected BitSequence MakeRandomOption(int numberOfBits, BitSequence mask, int invocationIndex)
        {
            var option = ConstantBitArrayView.MakeZeros(numberOfBits);
            return MaskOption(option, mask, invocationIndex);
        }

        protected async Task SendReceiverMessage(BitMatrix us)
        {
            var message = new MessageComposer();
            message.Write(us);
            await Channel.WriteMessageAsync(message.Compose());
        }

        private async Task<BitMatrix> ReceiveReceiverMessage(int numberOfInvocations)
        {
            var message = new MessageDecomposer(await Channel.ReadMessageAsync());
            return message.ReadBitMatrix(numberOfInvocations, CodeLength);
        }

        protected async Task<ObliviousTransferOptions> ReceiveMaskedOptions(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            var message = new MessageDecomposer(await Channel.ReadMessageAsync());
            var maskedOptions = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            for (int i = 0; i < numberOfInvocations; ++i)
            {
                maskedOptions.SetInvocation(i, message.ReadBitArray(numberOfOptions * numberOfMessageBits));
            }
            return maskedOptions;
        }

        protected async Task SendMaskedOptions(ObliviousTransferOptions maskedOptions)
        {
            var message = new MessageComposer(1);
            for (int i = 0; i < maskedOptions.NumberOfInvocations; ++i)
            {
                message.Write(maskedOptions.GetInvocation(i));
            }
            await Channel.WriteMessageAsync(message.Compose());
        }

        public static double EstimateCost(
            ObliviousTransferUsageProjection usageProjection,
            CostCalculationCallback calculateBaseOtCostCallback
        )
        {
            if (!usageProjection.HasMaxNumberOfInvocations)
                return double.PositiveInfinity;

            Debug.Assert(usageProjection.HasMaxNumberOfBatches);

            int codeLength = 2 * usageProjection.SecurityLevel;

            // base OT cost
            ObliviousTransferUsageProjection baseOtProjection = new ObliviousTransferUsageProjection();
            baseOtProjection.MaxNumberOfOptions = 2;
            baseOtProjection.MaxNumberOfInvocations = codeLength;
            baseOtProjection.MaxNumberOfBatches = 1;
            baseOtProjection.AverageMessageBits = usageProjection.SecurityLevel;
            baseOtProjection.SecurityLevel = usageProjection.SecurityLevel;
            double baseOtCost = calculateBaseOtCostCallback(baseOtProjection);

            // bandwidth cost of security exchange
            double averageInvocationsPerBatch = usageProjection.AverageInvocationsPerBatch;
            double maxNumberOfBatches = usageProjection.MaxNumberOfBatches;

            double averageNumberOfOptions = usageProjection.AverageNumberOfOptions;

            // bandwidth cost of security exchange
            double securityExchangeBitLength = codeLength;
            double securityExchangeCost = maxNumberOfBatches * averageInvocationsPerBatch * securityExchangeBitLength;

            return baseOtCost + securityExchangeCost;
        }

    }
}
