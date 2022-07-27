using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;
using CompactOT.Buffers;
using CompactCryptoGroupAlgebra;

using CompactOT.DataStructures;

namespace CompactOT
{
    /// <remarks>
    /// References:
    /// Asharov, Lindell, Schneider, Zohner: More Efficient Oblivious Transfer and Extensions for Faster Secure Computation. 2013. https://thomaschneider.de/papers/ALSZ13.pdf
    /// describes the protocol for 1oo2-OT; adapted for the 1ooN case in this implementation.
    /// </remarks>
    public class ALSZRandomObliviousTransferChannel : RandomObliviousTransferChannel
    {

        private ObliviousTransferChannel _baseOT;


        private NumberLength _securityParameter;

        public override int SecurityLevel => _securityParameter.InBits;
        protected RandomOracle RandomOracle { get; }

        protected int CodeLength => 2*SecurityLevel;

        
        /// <summary>
        /// Internal encapsulation of the persistent state for the sender role.
        /// </summary>
        private class SenderState
        {
            public RandomByteSequence[] SeededRandomOracles;
            public BitArray RandomChoices;

            public SenderState(int stateSize, int numberOfOptions)
            {
                SeededRandomOracles = new RandomByteSequence[stateSize];
                RandomChoices = new BitArray(stateSize);
            }
        };
        private SenderState? _senderState;

        /// <summary>
        /// Internal encapsulation of the persistent state for the receiver role.
        /// </summary>
        private class ReceiverState
        {
            public RandomByteSequence[,] SeededRandomOracles;

            public ReceiverState(int stateSize, int numberOfOptions)
            {
                SeededRandomOracles = new RandomByteSequence[stateSize, numberOfOptions];
            }

        }
        private ReceiverState? _receiverState;

        public override IMessageChannel Channel => _baseOT.Channel;
        protected RandomNumberGenerator RandomNumberGenerator { get; private set; }

        public int NumberOfOptions { get; private set; }

        private int _totalNumberOfInvocations;
        /// <summary>
        /// The total number of OT invocations that have been performed on this channel so far.
        /// 
        /// Every call to Send/Receive will advance this by the number of invocations requested for that call,
        /// even if the call should fail.
        /// </summary>
        protected int NumberOfInvocations => _totalNumberOfInvocations;

        public ALSZRandomObliviousTransferChannel(ObliviousTransferChannel baseOT, int securityParameter, CryptoContext cryptoContext)
        {
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
            _totalNumberOfInvocations = 0;
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
            DebugUtils.WriteLineSender("RandomOT", $"Performing base transfers ({CodeLength} times {SecurityLevel} bits).");
#endif
            // retrieve seeds for OT extension via _securityParameter many base OTs
            ObliviousTransferResult seeds = await _baseOT.ReceiveAsync(
                _senderState.RandomChoices,
                numberOfMessageBits: _securityParameter.InBits
            );
#if DEBUG
            DebugUtils.WriteLineSender("RandomOT", "Base transfers completed after {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            if (seeds.NumberOfInvocations != CodeLength)
            {
                throw new ProtocolException("Base transfer received unexpected number of invocations!");
            }
            if (seeds.NumberOfMessageBits != SecurityLevel)
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
            DebugUtils.WriteLineReceiver("RandomOT", $"Performing base transfers ({CodeLength} times {SecurityLevel} bits).");
#endif

            // base OTs as _sender_ with the seeds as inputs
            // Task sendTask = _baseOT.SendAsync(seeds, SecurityParameter, _securityParameter.InBytes);
            Task sendTask = _baseOT.SendAsync(seeds);

            // initializing a random oracle based on each seed
            for (int k = 0; k < CodeLength; ++k)
            {
                _receiverState.SeededRandomOracles[k, 0] = RandomOracle.Invoke(seeds.GetMessage(k, 0).AsByteEnumerable());
                _receiverState.SeededRandomOracles[k, 1] = RandomOracle.Invoke(seeds.GetMessage(k, 1).AsByteEnumerable());
            };

            await sendTask;
#if DEBUG
            DebugUtils.WriteLineReceiver("RandomOT", "Base transfers completed after {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
        }

        public override async Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            if (numberOfOptions > CodeLength)
            {
                throw new ArgumentException($"Extended Oblivious Transfer with security level {SecurityLevel} requires " +
                    $"the number of options to be less than {CodeLength}; was {numberOfOptions}", nameof(numberOfOptions));
            }
            if (_receiverState == null) await ExecuteReceiverBaseTransferAsync();
            
            int numberOfInvocations = selectionIndices.Length;

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
            DebugUtils.WriteLineReceiver("RandomOT", "Generating random Ts and U took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif      
            await SendReceiverMessage(us);

            var results = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);

            int totalNumberOfInvocations = _totalNumberOfInvocations;
            _totalNumberOfInvocations += numberOfInvocations;

            for (int i = 0; i < numberOfInvocations; ++i)
            {
                var t0Col = ts[0].GetColumn(i);
                var unmaskedOption = MakeRandomOption(numberOfMessageBits, t0Col, totalNumberOfInvocations + i);
                results.SetRow(i, unmaskedOption);
            }
#if DEBUG
            DebugUtils.WriteLineReceiver("RandomOT", "Computing outputs took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif
            return results;
        }

        /// <summary>
        /// Masks an option (i.e., a sender input message).
        /// </summary>
        private BitSequence MaskOption(BitSequence option, BitSequence mask, int invocationIndex)
        {
            var query = BufferBuilder.Empty.With(mask).With(invocationIndex).Create();
            return new EnumeratedBitArrayView(RandomOracle.Mask(option.AsByteEnumerable(), query), option.Length);
        }

        private BitSequence MakeRandomOption(int numberOfBits, BitSequence mask, int invocationIndex)
        {
            var option = ConstantBitArrayView.MakeZeros(numberOfBits);
            return MaskOption(option, mask, invocationIndex);
        }

        private async Task SendReceiverMessage(BitMatrix us)
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

        public override async Task<ObliviousTransferOptions> SendAsync(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            if (numberOfOptions > CodeLength)
            {
                throw new ArgumentException($"Extended Oblivious Transfer with security level {SecurityLevel} requires " +
                    $"the number of options to be less than {CodeLength}; was {numberOfOptions}", nameof(numberOfOptions));
            }
            if (_senderState == null) await ExecuteSenderBaseTransferAsync();

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            BitMatrix us = await ReceiveReceiverMessage(numberOfInvocations);
#if DEBUG
            DebugUtils.WriteLineSender("RandomOT", "Receiving U took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif
            Debug.Assert(us.Cols == CodeLength);
            Debug.Assert(us.Rows == numberOfInvocations);
            Debug.Assert(_senderState!.RandomChoices.Length == CodeLength);

            BitMatrix qs = new BitMatrix(numberOfInvocations, CodeLength);
            for (int k = 0; k < CodeLength; ++k)
            {
                var u = us.GetColumn(k);
                Debug.Assert(u.Length == numberOfInvocations);
                var q = u & _senderState!.RandomChoices[k];
                q = q ^ _senderState!.SeededRandomOracles[k].GetBits(numberOfInvocations);
                qs.SetColumn(k, q);
            }

            var randomOptions = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);

            int totalNumberOfInvocations = _totalNumberOfInvocations;
            _totalNumberOfInvocations += numberOfInvocations;
            for (int j = 0; j < numberOfOptions; ++j)
            {
                var selectionCode = WalshHadamardCode.ComputeWalshHadamardCode(j, CodeLength);
                var queryMask = selectionCode & _senderState.RandomChoices;
                Debug.Assert(queryMask.Length == CodeLength);

                for (int i = 0; i < numberOfInvocations; ++i)
                {
                    var query = queryMask ^ qs.GetRow(i);

                    var option = MakeRandomOption(numberOfMessageBits, query, totalNumberOfInvocations + i);
                    Debug.Assert(option.Length == numberOfMessageBits);

                    randomOptions.SetMessage(i, j, option);
                }
            }

#if DEBUG
            DebugUtils.WriteLineSender("RandomOT", "Computing outputs took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif

            return randomOptions;
        }

    }
}
