using System;
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
    /// References:
    /// Main: Vladimir Kolesnikov, Ranjit Kumaresan: Improved OT Extension for Transferring Short Secrets. 2013. https://www.microsoft.com/en-us/research/wp-content/uploads/2017/03/otext_crypto13.pdf
    /// Summary explanation of above in: Michele Orrù, Emmanuela Orsini, Peter Scholl: Actively Secure 1-out-of-N OT Extension with Application to Private Set Intersection. 2017. https://hal.archives-ouvertes.fr/hal-01401005/file/933.pdf
    /// Original 1oo2 OT extension: Yuval Ishai, Joe Kilian, Kobbi Nissim and Erez Petrank: Extending Oblivious Transfers Efficiently. 2003. https://link.springer.com/content/pdf/10.1007/978-3-540-45146-4_9.pdf
    /// </remarks>
    public class ExtendedObliviousTransferChannel : ObliviousTransferChannel
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

        public ExtendedObliviousTransferChannel(ObliviousTransferChannel baseOT, int securityParameter, CryptoContext cryptoContext)
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

            // retrieve seeds for OT extension via _securityParameter many base OTs
            BitMatrix seeds = await _baseOT.ReceiveAsync(
                _senderState.RandomChoices,
                numberOfMessageBits: _securityParameter.InBits
            );
            if (seeds.Rows != CodeLength)
            {
                throw new ProtocolException("Base transfer received unexpected number of invocations!");
            }
            if (seeds.Cols != SecurityLevel)
            {
                throw new ProtocolException("Base transfer received messages with unexpected lengths!");
            }

            // initializing a random oracle based on each seed
            for (int k = 0; k < CodeLength; ++k)
            {
                _senderState.SeededRandomOracles[k] = RandomOracle.Invoke(seeds.GetRow(k).AsByteEnumerable());
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
        }

        public override async Task<BitMatrix> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            if (numberOfOptions > CodeLength)
            {
                throw new ArgumentException($"Extended Oblivious Transfer with security level {SecurityLevel} requires " +
                    $"the number of options to be less than {CodeLength}; was {numberOfOptions}", nameof(numberOfOptions));
            }
            if (_receiverState == null) await ExecuteReceiverBaseTransferAsync();
            
            int numberOfInvocations = selectionIndices.Length;

            NumberLength optionLength = NumberLength.GetLength(numberOfOptions);

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

            
            Task sendingTask = SendReceiverMessage(us);

            BitMatrix results = new BitMatrix(numberOfInvocations, numberOfMessageBits);
            ObliviousTransferOptions maskedOptions = await ReceiveMaskedOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            Debug.Assert(maskedOptions.NumberOfInvocations == numberOfInvocations);

            for (int i = 0; i < numberOfInvocations; ++i, ++_totalNumberOfInvocations)
            {
                int s = selectionIndices[i];
                var q = maskedOptions.GetMessage(i, s);
                Debug.Assert(q.Length == numberOfMessageBits);
                var t0Col = ts[0].GetColumn(i);
                var unmaskedOption = MaskOption(q, t0Col, _totalNumberOfInvocations);
                results.SetRow(i, unmaskedOption);
            }
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

        private async Task SendReceiverMessage(BitMatrix us)
        {
            var message = new MessageComposer();
            message.Write(us);
            await Channel.WriteMessageAsync(message.Compose());
        }

        private async Task<ObliviousTransferOptions> ReceiveMaskedOptions(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            var message = new MessageDecomposer(await Channel.ReadMessageAsync());
            var maskedOptions = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            for (int i = 0; i < numberOfInvocations; ++i)
            {
                maskedOptions.SetInvocation(i, message.ReadBitArray(numberOfOptions * numberOfMessageBits));
            }
            return maskedOptions;
        }

        private async Task<BitMatrix> ReceiveReceiverMessage(int numberOfInvocations)
        {
            var message = new MessageDecomposer(await Channel.ReadMessageAsync());
            return message.ReadBitMatrix(numberOfInvocations, CodeLength);
        }

        public override async Task SendAsync(ObliviousTransferOptions options)
        {
            if (options.NumberOfOptions > CodeLength)
            {
                throw new ArgumentException($"Extended Oblivious Transfer with security level {SecurityLevel} requires " +
                    $"the number of options to be less than {CodeLength}; was {options.NumberOfOptions}", nameof(options));
            }
            if (_senderState == null) await ExecuteSenderBaseTransferAsync();

            BitMatrix us = await ReceiveReceiverMessage(options.NumberOfInvocations);
            Debug.Assert(us.Cols == CodeLength);
            Debug.Assert(us.Rows == options.NumberOfInvocations);
            Debug.Assert(_senderState!.RandomChoices.Length == CodeLength);

            BitMatrix qs = new BitMatrix(options.NumberOfInvocations, CodeLength);
            for (int k = 0; k < CodeLength; ++k)
            {
                var u = us.GetColumn(k);
                Debug.Assert(u.Length == options.NumberOfInvocations);
                var q = u & _senderState!.RandomChoices[k];
                q = q ^ _senderState!.SeededRandomOracles[k].GetBits(options.NumberOfInvocations);
                qs.SetColumn(k, q);
            }

            var optionLength = NumberLength.GetLength(options.NumberOfOptions);
            
            var numberOfMessageBits = options.NumberOfMessageBits;

            var maskedOptions = new ObliviousTransferOptions(options.NumberOfInvocations, options.NumberOfOptions, numberOfMessageBits);

            int totalNumberOfInvocations = _totalNumberOfInvocations;
            _totalNumberOfInvocations += options.NumberOfInvocations;
            for (int j = 0; j < options.NumberOfOptions; ++j)
            {
                var selectionCode = WalshHadamardCode.ComputeWalshHadamardCode(j, CodeLength);
                var queryMask = selectionCode & _senderState.RandomChoices;
                Debug.Assert(queryMask.Length == CodeLength);

                for (int i = 0; i < options.NumberOfInvocations; ++i)
                {
                    var option = options.GetMessage(i, j);
                    
                    var query = queryMask ^ qs.GetRow(i);
                    var maskedOption = MaskOption(option, query, totalNumberOfInvocations + i);
                    Debug.Assert(maskedOption.Length == numberOfMessageBits);

                    maskedOptions.SetMessage(i, j, maskedOption);
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

        private async Task SendMaskedOptions(ObliviousTransferOptions maskedOptions)
        {
            var message = new MessageComposer(1);
            for (int i = 0; i < maskedOptions.NumberOfInvocations; ++i)
            {
                message.Write(maskedOptions.GetInvocation(i));
            }
            await Channel.WriteMessageAsync(message.Compose());
        }

    }
}
