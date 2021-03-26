using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;
using CompactOT.Buffers;
using CompactCryptoGroupAlgebra;

using CompactOT.DataStructures;

namespace CompactOT
{
    /// <summary>
    /// Implementation of the OT extension protocol that enables extending a small number of expensive oblivious transfer
    /// invocation (so called "base OTs") into a larger number of usable oblivious transfer invocations using only cheap
    /// symmetric cryptography via a random oracle instantiation.
    /// </summary>
    /// <remarks>
    /// References: Yuval Ishai, Joe Kilian, Kobbi Nissim and Erez Petrank: Extending Oblivious Transfers Efficiently. 2003. https://link.springer.com/content/pdf/10.1007/978-3-540-45146-4_9.pdf
    /// and Gilad Asharov, Yehuda Lindell, Thomas Schneider and Michael Zohner: More Efficient Oblivious Transfer and Extensions for Faster Secure Computation. 2013. Section 5.3. https://thomaschneider.de/papers/ALSZ13.pdf
    /// </remarks>
    public class ExtendedObliviousTransferChannel : ObliviousTransferChannel
    {

        private ObliviousTransferChannel _baseOT;


        private NumberLength _securityParameter;

        public override int SecurityLevel => _securityParameter.InBits;
        protected RandomOracle RandomOracle { get; }

        private int CodeLength => 2*SecurityLevel;

        
        /// <summary>
        /// Internal encapsulation of the persistent state for the sender role.
        /// </summary>
        private struct SenderState
        {
            public RandomByteSequence[] SeededRandomOracles;
            public BitArray RandomChoices;

            public SenderState(int stateSize, int numberOfOptions)
            {
                SeededRandomOracles = new RandomByteSequence[stateSize];
                RandomChoices = new BitArray(stateSize);
            }
        };
        private SenderState _senderState;

        /// <summary>
        /// Internal encapsulation of the persistent state for the receiver role.
        /// </summary>
        private struct ReceiverState
        {
            public RandomByteSequence[,] SeededRandomOracles;

            public ReceiverState(int stateSize, int numberOfOptions)
            {
                SeededRandomOracles = new RandomByteSequence[stateSize, numberOfOptions];
            }
        }
        private ReceiverState _receiverState;

        public override IMessageChannel Channel => _baseOT.Channel;
        protected RandomNumberGenerator RandomNumberGenerator { get; private set; }

        public int NumberOfOptions { get; private set; }

        public ExtendedObliviousTransferChannel(ObliviousTransferChannel baseOT, int numberOfOptions, int securityParameter, CryptoContext cryptoContext)
        {
            _baseOT = baseOT;
            RandomNumberGenerator = new ThreadsafeRandomNumberGenerator(cryptoContext.RandomNumberGenerator);
            RandomOracle = new HashRandomOracle(cryptoContext.HashAlgorithm);
            _securityParameter = NumberLength.FromBitLength(securityParameter);
            NumberOfOptions = numberOfOptions;
            _senderState = new SenderState(CodeLength, NumberOfOptions);
            _receiverState = new ReceiverState(CodeLength, NumberOfOptions);
        }


        /// <summary>
        /// Performs k many 1-out-of-N OTs on k bits for the sender, where k is the security parameter, using the base OT implementation.
        /// 
        /// These are subsequently expanded into m many 1ooN OTs on arbitrarily long messages
        /// by the SendAsync method, where m is only bounded by the amount of secure randomness the random
        /// oracle implementation can produce.
        /// </summary>
        public async Task ExecuteSenderBaseOTAsync()
        {
            _senderState.RandomChoices = RandomNumberGenerator.GetBits(CodeLength);

            // retrieve seeds for OT extension via _securityParameter many base OTs
            byte[][] seeds = await _baseOT.ReceiveAsync(
                _senderState.RandomChoices,
                numberOfMessageBits: _securityParameter.InBits
            );
            Debug.Assert(seeds.Length == CodeLength);

            // initializing a random oracle based on each seed
            for (int k = 0; k < CodeLength; ++k)
            {
                Debug.Assert(seeds[k].Length == _securityParameter.InBytes);
                _senderState.SeededRandomOracles[k] = RandomOracle.Invoke(seeds[k]);
            }
        }

        /// <summary>
        /// Performs k many 1-out-of-N OTs on k bits for the receiver, where k is the security parameter, using the base OT implementation.
        /// 
        /// These are subsequently expanded into m many 1ooN OTs on arbitrarily long messages
        /// by the ReceiveAsync method, where m is only bounded by the amount of secure randomness the random
        /// oracle implementation can produce.
        /// </summary>
        public async Task ExecuteReceiverBaseOTAsync()
        {
            // generating _securityParameter many pairs of random seeds of length _securityParameter
            int numBaseOTOptions = 2;
            var seeds = new ObliviousTransferOptions<byte>(CodeLength, numBaseOTOptions, _securityParameter.InBytes);
            ObliviousTransferOptions<byte>.FillWithRandom(seeds, RandomNumberGenerator);

            // base OTs as _sender_ with the seeds as inputs
            // Task sendTask = _baseOT.SendAsync(seeds, SecurityParameter, _securityParameter.InBytes);
            Task sendTask = _baseOT.SendAsync(seeds);

            // initializing a random oracle based on each seed
            for (int k = 0; k < CodeLength; ++k)
            {
                _receiverState.SeededRandomOracles[k, 0] = RandomOracle.Invoke(seeds.GetMessageOption(k, 0));
                _receiverState.SeededRandomOracles[k, 1] = RandomOracle.Invoke(seeds.GetMessageOption(k, 1));
            };

            await sendTask;
        }

        public override async Task<byte[][]> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            int numberOfInvocations = selectionIndices.Length;

            NumberLength optionLength = NumberLength.GetLength(numberOfOptions);

            BitMatrix[] ts = new BitMatrix[] {
                new BitMatrix(CodeLength, numberOfInvocations),
                new BitMatrix(CodeLength, numberOfInvocations)
            };

            for (int k = 0; k < CodeLength; ++k)
            {
                ts[0].SetRow(k, _receiverState.SeededRandomOracles[k, 0].GetBits(numberOfInvocations));
                ts[1].SetRow(k, _receiverState.SeededRandomOracles[k, 1].GetBits(numberOfInvocations));
            }

            BitMatrix us = new BitMatrix(numberOfInvocations, CodeLength);

            for (int j = 0; j < numberOfInvocations; ++j)
            {
                var t0Col = ts[0].GetColumn(j);
                var t1Col = ts[1].GetColumn(j);

                var selectionCode = WalshHadamardCode.ComputeWalshHadamardCode(selectionIndices[j], optionLength.InBits);
                Debug.Assert(t0Col.Length == CodeLength);
                Debug.Assert(t1Col.Length == CodeLength);
                Debug.Assert(selectionCode.Length == CodeLength);

                us.SetRow(j,
                    t0Col ^ (t1Col) ^ (WalshHadamardCode.ComputeWalshHadamardCode(selectionIndices[j], optionLength.InBits))
                );
            }

            
            Task sendingTask = SendReceiverMessage(us);

            BitMatrix[] maskedOptions = await ReceiveMaskedOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);

            byte[][] results = new byte[numberOfInvocations][];

            for (int j = 0; j < numberOfInvocations; ++j)
            {
                int s = selectionIndices[j];
                var q = maskedOptions[s].GetRow(j);
                Debug.Assert(q.Length == CodeLength);
                var t0Col = ts[0].GetColumn(j);
                var unmaskedOption = MaskOption(q, t0Col, (int)j);
                results[j] = unmaskedOption.ToBytes();
            }
            return results;
        }

        /// <summary>
        /// Masks an option (i.e., a sender input message).
        /// </summary>
        private BitArray MaskOption(BitArray option, BitArray mask, int invocationIndex)
        {
            var query = BufferBuilder.From(mask.ToBytes()).With(invocationIndex).Create();
            return RandomOracle.Mask(option.ToBytes(), query);
        }

        private async Task SendReceiverMessage(BitMatrix us)
        {
            var message = new MessageComposer();
            message.Write(us);
            await Channel.WriteMessageAsync(message.Compose());
        }

        private async Task<BitMatrix[]> ReceiveMaskedOptions(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            var message = new MessageDecomposer(await Channel.ReadMessageAsync());
            BitMatrix[] maskedOptions = new BitMatrix[numberOfOptions];
            for (int i = 0; i < numberOfOptions; ++i)
            {
                maskedOptions[i] = message.ReadBitMatrix(numberOfInvocations, numberOfMessageBits);
            }
            return maskedOptions;
        }

        private async Task<BitMatrix> ReceiveReceiverMessage(int numberOfInvocations)
        {
            var message = new MessageDecomposer(await Channel.ReadMessageAsync());
            return message.ReadBitMatrix(numberOfInvocations, CodeLength);
        }

        public override async Task SendAsync(ObliviousTransferOptions<byte> options)
        {
            BitMatrix us = await ReceiveReceiverMessage(options.NumberOfInvocations);
            Debug.Assert(us.Cols == CodeLength);
            Debug.Assert(us.Rows == options.NumberOfInvocations);
            Debug.Assert(_senderState.RandomChoices.Length == CodeLength);

            BitMatrix qs = new BitMatrix(options.NumberOfInvocations, CodeLength);
            for (int k = 0; k < CodeLength; ++k)
            {
                var u = us.GetColumn(k);
                Debug.Assert(u.Length == options.NumberOfInvocations);
                var q = u & _senderState.RandomChoices[k];
                q = q ^ _senderState.SeededRandomOracles[k].GetBits(options.NumberOfInvocations);
                qs.SetColumn(k, q);
            }

            var optionLength = NumberLength.GetLength(options.NumberOfOptions);
            
            var numberOfMessageBits = NumberLength.FromByteLength(options.MessageLength).InBits;

            BitMatrix[] maskedOptions = new BitMatrix[options.NumberOfOptions];
            for (int j = 0; j < options.NumberOfOptions; ++j)
            {
                maskedOptions[j] = new BitMatrix(options.NumberOfInvocations, numberOfMessageBits);
                var selectionCode = WalshHadamardCode.ComputeWalshHadamardCode(j, optionLength.InBits);

                var queryMask = selectionCode & _senderState.RandomChoices;
                Debug.Assert(queryMask.Length == CodeLength);

                for (int i = 0; i < options.NumberOfInvocations; ++i)
                {
                    var option = BitArray.CreateFromByteEnumerator(
                        options.GetMessageOption(i, j).GetEnumerator(), numberOfMessageBits
                    );

                    var query = queryMask ^ qs.GetRow(i);
                    var maskedOption = MaskOption(option, query, i);
                    Debug.Assert(maskedOption.Length == options.MessageLength);

                    maskedOptions[j].SetRow(i, maskedOption);
                }
            }

            await SendMaskedOptions(maskedOptions);
        }

        private async Task SendMaskedOptions(BitMatrix[] maskedOptions)
        {
            var message = new MessageComposer(maskedOptions.Length);
            foreach (var option in maskedOptions)
            {
                message.Write(option);
            }
            await Channel.WriteMessageAsync(message.Compose());
        }

    }
}
