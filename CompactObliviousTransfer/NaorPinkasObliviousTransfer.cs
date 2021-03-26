using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;

using CompactCryptoGroupAlgebra;
using CompactOT.Buffers;

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
    /// </remarks>
    public class NaorPinkasObliviousTransfer<TSecret, TCrypto> : StatelessObliviousTransfer where TSecret: notnull where TCrypto: notnull
    {

        private CryptoGroup<TSecret, TCrypto> _group;

        private RandomOracle _randomOracle;
        private RandomNumberGenerator _randomNumberGenerator;

        public override int SecurityLevel => _group.SecurityLevel;
        
        public NaorPinkasObliviousTransfer(CryptoGroup<TSecret, TCrypto> cryptoGroup, CryptoContext cryptoContext)
        {
            _group = cryptoGroup;
            _randomOracle = new HashRandomOracle(cryptoContext.HashAlgorithm);
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
        public override async Task SendAsync(IMessageChannel channel, ObliviousTransferOptions<byte> options)
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
            Console.WriteLine("[Sender] Generating group elements took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            Task writeCsTask = WriteGroupElements(channel, listOfCs);
            Task<CryptoGroupElement<TSecret, TCrypto>[]> readDsTask = ReadGroupElements(channel, options.NumberOfInvocations);

            var listOfExponentiatedCs = new CryptoGroupElement<TSecret, TCrypto>[options.NumberOfOptions];
            Parallel.For(1, options.NumberOfOptions, i =>
            {
                listOfExponentiatedCs[i] = listOfCs[i] * alpha;
            });

            await Task.WhenAll(writeCsTask, readDsTask);
            var listOfDs = readDsTask.Result;

#if DEBUG
            stopwatch.Stop();
            Console.WriteLine("[Sender] Precomputing exponentations, sending c and reading d took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            var maskedOptions = ObliviousTransferOptions<byte>.MakeNewLike(options);

            Parallel.For(0, maskedOptions.NumberOfInvocations, j =>
            {
                var exponentiatedD = listOfDs[j] * alpha;
                var inverseExponentiatedD = -exponentiatedD;

                Parallel.For(0, maskedOptions.NumberOfOptions, i =>
                {
                    var e = exponentiatedD;
                    if (i > 0)
                        e = listOfExponentiatedCs[i] + inverseExponentiatedD;

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
                    maskedOptions.SetMessageOption(j, i, MaskOption(options.GetMessageOption(j, i), e, j, i));
                });
            });

#if DEBUG
            stopwatch.Stop();
            Console.WriteLine("[Sender] Computing masked options took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            await WriteOptions(channel, maskedOptions);

#if DEBUG
            stopwatch.Stop();
            Console.WriteLine("[Sender] Sending masked options took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
        }

        /// <inheritdoc/>
        public override async Task<byte[][]> ReceiveAsync(IMessageChannel channel, int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            int numberOfInvocations = selectionIndices.Length;

            var listOfCs = await ReadGroupElements(channel, numberOfOptions);

#if DEBUG
            stopwatch.Stop();
            Console.WriteLine("[Receiver] Reading values for c took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            var listOfBetas = new TSecret[numberOfInvocations];
            var listOfDs = new CryptoGroupElement<TSecret, TCrypto>[numberOfInvocations];

            Parallel.For(0, numberOfInvocations, j =>
            {
                (listOfBetas[j], listOfDs[j]) = GenerateGroupElement();
                if (selectionIndices[j] > 0)
                    listOfDs[j] = listOfCs[selectionIndices[j]] - listOfDs[j];
            });

#if DEBUG
            stopwatch.Stop();
            Console.WriteLine("[Receiver] Generating and d took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            Task writeDsTask = WriteGroupElements(channel, listOfDs);
            Task<ObliviousTransferOptions<byte>> readMaskedOptionsTask = ReadOptions(channel, numberOfInvocations, numberOfOptions, numberOfMessageBits);

            var listOfEs = new CryptoGroupElement<TSecret, TCrypto>[numberOfInvocations];

            Parallel.For(0, numberOfInvocations, j =>
            {
                listOfEs[j] = listOfCs[0] * listOfBetas[j];
            });

            await Task.WhenAll(writeDsTask, readMaskedOptionsTask);
            var maskedOptions = readMaskedOptionsTask.Result;

#if DEBUG
            stopwatch.Stop();
            Console.WriteLine("[Receiver] Computing e, sending d and reading masked options took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Restart();
#endif

            byte[][] selectedOptions = new byte[numberOfInvocations][];
            Parallel.For(0, numberOfInvocations, j =>
            {
                int i = selectionIndices[j];

                selectedOptions[j] = MaskOption(maskedOptions.GetMessageOption(j, i), listOfEs[j], j, i).ToArray();
            });

#if DEBUG
            stopwatch.Stop();
            Console.WriteLine("[Receiver] Unmasking result took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            
            return selectedOptions;
        }

        private (TSecret, CryptoGroupElement<TSecret, TCrypto>) GenerateGroupElement()
        {
            return _group.GenerateRandom(_randomNumberGenerator);
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

        private Task WriteOptions(IMessageChannel channel, ObliviousTransferOptions<byte> options)
        {
            MessageComposer message = new MessageComposer(options.NumberOfOptions * options.NumberOfInvocations);
            for (int j = 0; j < options.NumberOfInvocations; ++j)
            {
                for (int i = 0; i < options.NumberOfOptions; ++i)
                    message.Write(options.GetMessageOption(j, i).ToArray());
            }

            return channel.WriteMessageAsync(message.Compose());
        }

        private async Task<ObliviousTransferOptions<byte>> ReadOptions(
            IMessageChannel channel, int numberOfInvocations, int numberOfOptions, int numberOfMessageBits
        )
        {
            MessageDecomposer message = new MessageDecomposer(await channel.ReadMessageAsync());

            var options = new ObliviousTransferOptions<byte>(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            for (int j = 0; j < numberOfInvocations; ++j)
            {
                for (int i = 0; i < numberOfOptions; ++i)
                    options.SetMessageOption(j, i, message.ReadBuffer(NumberLength.FromBitLength(numberOfMessageBits).InBytes));
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
        private IEnumerable<byte> MaskOption(IEnumerable<byte> option, CryptoGroupElement<TSecret, TCrypto> groupElement, int invocationIndex, int optionIndex)
        {
            var query = BufferBuilder.From(groupElement.ToBytes()).With(invocationIndex).With(optionIndex).Create();
            return _randomOracle.Mask(option, query);
        }
        
    }
}
