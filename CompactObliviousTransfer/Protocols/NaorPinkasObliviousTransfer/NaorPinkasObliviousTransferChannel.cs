// SPDX-FileCopyrightText: 2023 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;

using CompactCryptoGroupAlgebra;
using CompactOT.Buffers;
using CompactOT.DataStructures;

namespace CompactOT
{

    /// <summary>
    /// 1-out-of-N Oblivious Transfer implementation following a protocol by Naor and Pinkas.
    /// </summary>
    /// <remarks>
    /// Reference: Moni Naor and Benny Pinkas: Efficient oblivious transfer protocols 2001. https://dl.acm.org/citation.cfm?id=365502
    ///
    /// Further implementation details: Seung Geol Choi et al.: Secure Multi-Party Computation of Boolean Circuits with Applications
    /// to Privacy in On-Line Marketplaces. https://link.springer.com/chapter/10.1007/978-3-642-27954-6_26
    /// 
    /// Note that this implemenatation is slightly modified so as to remove special treatment for the first options: In the notation
    /// of Naor and Pinkas, the sender chooses an additional constant C_0, the receiver/chooser computes and sends PK = C_sigma / PK_sigma
    /// for any choice sigma (instead of only for sigma > 0), which the sender uses in subsequent computations in place of PK_0 of Naor
    /// and Pinkas' protocol description. Realising that the sender must transmit g^r in addition to C_1, ..., C_(N-1), we set
    /// C_0 = g^r and thus incur no transmission cost for the additional C_0.
    /// </remarks>
    public class NaorPinkasObliviousTransferChannel<TSecret, TCrypto> : IObliviousTransferChannel where TSecret: notnull where TCrypto: notnull
    {

        private IMessageChannel _channel;

        private CryptoGroup<TSecret, TCrypto> _group;

        private RandomOracle _randomOracle;
        private RandomNumberGenerator _randomNumberGenerator;

        public int SecurityLevel => _group.SecurityLevel;

        public IMessageChannel Channel => _channel;
        
        public NaorPinkasObliviousTransferChannel(
            IMessageChannel channel,
            CryptoGroup<TSecret, TCrypto> cryptoGroup,
            CryptoContext cryptoContext
        )
        {
            _channel = channel;
            _group = cryptoGroup;
            _randomOracle = new HashRandomOracle(cryptoContext.HashAlgorithmProvider);
            _randomNumberGenerator = new ThreadsafeRandomNumberGenerator(cryptoContext.RandomNumberGenerator);
#if DEBUG
            Console.WriteLine("Security parameters:");
            Console.WriteLine("order = {0}", _group.Order);
            Console.WriteLine("generator = {0}", _group.Generator);
            Console.WriteLine("group element size = {0} bytes", _group.ElementLength.InBytes);
            Console.WriteLine("scalar size = {0} bytes", _group.OrderLength.InBytes);
#endif
        }

        /// <inheritdoc/>
        public async Task SendAsync(ObliviousTransferOptions options)
        {
#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            
            var listOfCs = new CryptoGroupElement<TSecret, TCrypto>[options.NumberOfOptions];
            var listOfExponents = new TSecret[options.NumberOfOptions];

            Parallel.For(0, options.NumberOfOptions, i =>
            {
                (listOfExponents[i], listOfCs[i]) = _group.GenerateRandom(_randomNumberGenerator);
            });

            var alpha = listOfExponents[0];

#if DEBUG
            stopwatch.Stop();
            DebugUtils.WriteLineSender("NaorPinkas", "Generating group elements took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            Task writeCsTask = WriteGroupElements(_channel, listOfCs);
            Task<CryptoGroupElement<TSecret, TCrypto>[]> readDsTask = ReadGroupElements(_channel, options.NumberOfInvocations);

            var listOfExponentiatedCs = new CryptoGroupElement<TSecret, TCrypto>[options.NumberOfOptions];
            Parallel.For(0, options.NumberOfOptions, i =>
            {
                listOfExponentiatedCs[i] = listOfCs[i] * alpha;
            });

            await Task.WhenAll(writeCsTask, readDsTask);
            var listOfDs = readDsTask.Result;

#if DEBUG
            stopwatch.Stop();
            DebugUtils.WriteLineSender("NaorPinkas", "Precomputing exponentations, sending c and reading d took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            var maskedOptions = ObliviousTransferOptions.CreateLike(options);

            Parallel.For(0, maskedOptions.NumberOfInvocations, j =>
            {
                var exponentiatedD = listOfDs[j] * alpha;
                var inverseExponentiatedD = -exponentiatedD;

                Parallel.For(0, maskedOptions.NumberOfOptions, i =>
                {
                    var e = listOfExponentiatedCs[i] + inverseExponentiatedD;

                    Debug.Assert(!e.Value.Equals(_group.Algebra.NeutralElement));
                        
                    // note(lumip): the protocol as proposed by Naor and Pinkas includes a random value
                    //  to be incorporated in the random oracle query to ensure that the same query does
                    //  not occur several times. This is partly because they envision several receivers
                    //  over which the same Cs are used. Since we are having seperate sets of Cs for each
                    //  sender-receiver pair, the requirement of unique queries is satisified just using
                    //  the index j of the OT invocation and we can save a bit of bandwidth.

                    // todo: think about whether we want to use a static set of Cs for each sender for all
                    //  connection to reduce the required amount of computation per OT. Would require to
                    //  maintain state in this class and negate the points made in the note above.
                    var maskedOption = MaskOption(options.GetMessage(j, i), e, j, i);

                    // Writing into ObliviousTransferOptions is not threadsafe, need to lock
                    lock (maskedOptions)
                    {
                        maskedOptions.SetMessage(j, i, maskedOption);
                    }
                });
            });

#if DEBUG
            stopwatch.Stop();
            DebugUtils.WriteLineSender("NaorPinkas", "Computing masked options took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            await WriteOptions(_channel, maskedOptions);

#if DEBUG
            stopwatch.Stop();
            DebugUtils.WriteLineSender("NaorPinkas", "Sending masked options took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
        }

        /// <inheritdoc/>
        public async Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            int numberOfInvocations = selectionIndices.Length;

            var listOfCs = await ReadGroupElements(_channel, numberOfOptions);

#if DEBUG
            stopwatch.Stop();
            DebugUtils.WriteLineReceiver("NaorPinkas", "Reading values for c took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            var listOfBetas = new TSecret[numberOfInvocations];
            var listOfDs = new CryptoGroupElement<TSecret, TCrypto>[numberOfInvocations];

            Parallel.For(0, numberOfInvocations, j =>
            {
                (listOfBetas[j], listOfDs[j]) = _group.GenerateRandom(_randomNumberGenerator);
                listOfDs[j] = listOfCs[selectionIndices[j]] - listOfDs[j];
            });

#if DEBUG
            stopwatch.Stop();
            DebugUtils.WriteLineReceiver("NaorPinkas", "Generating d took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            Task writeDsTask = WriteGroupElements(_channel, listOfDs);
            Task<ObliviousTransferOptions> readMaskedOptionsTask = ReadOptions(_channel, numberOfInvocations, numberOfOptions, numberOfMessageBits);

            var listOfEs = new CryptoGroupElement<TSecret, TCrypto>[numberOfInvocations];

            Parallel.For(0, numberOfInvocations, j =>
            {
                listOfEs[j] = listOfCs[0] * listOfBetas[j];
            });

            await Task.WhenAll(writeDsTask, readMaskedOptionsTask);
            var maskedOptions = readMaskedOptionsTask.Result;

#if DEBUG
            stopwatch.Stop();
            DebugUtils.WriteLineReceiver("NaorPinkas", "Computing e, sending d and reading masked options took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            var selectedOptions = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);
            Parallel.For(0, numberOfInvocations, j =>
            {
                int i = selectionIndices[j];

                var unmaskedMessage = MaskOption(maskedOptions.GetMessage(j, i), listOfEs[j], j, i);
                // Writing into ObliviousTransferResult is not threadsafe, need to lock
                lock (selectedOptions)
                {
                    selectedOptions.SetRow(j, unmaskedMessage);
                }
            });

#if DEBUG
            stopwatch.Stop();
            DebugUtils.WriteLineReceiver("NaorPinkas", "Unmasking result took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            
            return selectedOptions;
        }

        private Task WriteGroupElements(IMessageChannel channel, IReadOnlyList<CryptoGroupElement<TSecret, TCrypto>> groupElements)
        {
            MessageComposer message = new MessageComposer(2 * groupElements.Count);
            foreach (var groupElement in groupElements)
            {
                byte[] packedGroupElement = groupElement.ToBytes();
                message.Write(packedGroupElement.Length);
                message.Write(packedGroupElement);
            }

            return channel.WriteMessageAsync(message.Compose());
        }

        private async Task<CryptoGroupElement<TSecret, TCrypto>[]> ReadGroupElements(IMessageChannel channel, int numberOfGroupElements)
        {
            MessageDecomposer message = new MessageDecomposer(await channel.ReadMessageAsync());

            var groupElements = new CryptoGroupElement<TSecret, TCrypto>[numberOfGroupElements];
            for (int i = 0; i < numberOfGroupElements; ++i)
            {
                int length = message.ReadInt();
                byte[] packedGroupElement = message.ReadBuffer(length);
                groupElements[i] = _group.FromBytes(packedGroupElement);
            }

            return groupElements;
        }

        private Task WriteOptions(IMessageChannel channel, ObliviousTransferOptions options)
        {
            MessageComposer message = new MessageComposer(options.NumberOfOptions * options.NumberOfInvocations);
            for (int j = 0; j < options.NumberOfInvocations; ++j)
            {
                for (int i = 0; i < options.NumberOfOptions; ++i)
                    message.Write(options.GetMessage(j, i));
            }

            return channel.WriteMessageAsync(message.Compose());
        }

        private async Task<ObliviousTransferOptions> ReadOptions(
            IMessageChannel channel, int numberOfInvocations, int numberOfOptions, int numberOfMessageBits
        )
        {
            MessageDecomposer message = new MessageDecomposer(await channel.ReadMessageAsync());

            var options = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            for (int j = 0; j < numberOfInvocations; ++j)
            {
                for (int i = 0; i < numberOfOptions; ++i)
                    options.SetMessage(j, i, message.ReadBitArray(NumberLength.FromBitLength(numberOfMessageBits).InBits));
            }

            return options;
        }

        /// <summary>
        /// Masks an option (i.e., a sender input message).
        /// </summary>
        /// <remarks>
        /// The option is XOR-masked with the output of a random oracle queried with the
        /// concatentation of the binary representations of the given groupElement, invocationIndex and optionIndex.
        /// </remarks>
        /// <param name="option">The sender input/option to be masked.</param>
        /// <param name="groupElement">The group element that acts as "key" in the query to the random oracle.</param>
        /// <param name="invocationIndex">The index of the OT invocation this options belongs to.</param>
        /// <param name="optionIndex">The index of the option.</param>
        /// <returns>The masked option.</returns>
        private BitSequence MaskOption(BitSequence option, CryptoGroupElement<TSecret, TCrypto> groupElement, int invocationIndex, int optionIndex)
        {
            var query = BufferBuilder.From(groupElement.ToBytes()).With(invocationIndex).With(optionIndex).Create();
            return _randomOracle.Mask(option, query.AsEnumerable());
        }

        public double EstimateCost(ObliviousTransferUsageProjection usageProjection)
        {
            // TODO: currently ignoring computation cost
            
            if (!usageProjection.HasMaxNumberOfInvocations)
                return double.PositiveInfinity;

            Debug.Assert(usageProjection.HasMaxNumberOfBatches);

            double averageInvocationsPerBatch = usageProjection.AverageInvocationsPerBatch;
            double maxNumberOfInvocations = usageProjection.MaxNumberOfInvocations;
            double maxNumberOfBatches = usageProjection.MaxNumberOfBatches;

            double averageNumberOfOptions = usageProjection.AverageNumberOfOptions;

            // bandwidth cost of security exchange
            double cryptoGroupElementSize = _group.ElementLength.InBits;
            double securityExchangeCost = 2.0 * maxNumberOfBatches * averageNumberOfOptions * cryptoGroupElementSize;

            // bandwidth cost of exchanging masked options
            double averageMessageBits = usageProjection.AverageMessageBits;
            double optionsExchangeCost = maxNumberOfInvocations * averageNumberOfOptions * averageMessageBits;

            return securityExchangeCost + optionsExchangeCost;
        }

    }
}
