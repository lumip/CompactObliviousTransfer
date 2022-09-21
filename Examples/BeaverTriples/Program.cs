using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

using CompactOT;
using CompactOT.DataStructures;


namespace CompactOT.Examples.BeaverTriples
{

    class TripleShareSet
    {
        public BitMatrix FirstFactorShare { get; }
        public BitMatrix SecondFactorShare { get; }
        public BitMatrix ProductShare { get; }

        public TripleShareSet(BitMatrix firstFactorShare, BitMatrix secondFactorShare, BitMatrix productShare)
        {
            if (firstFactorShare.Rows != secondFactorShare.Rows || secondFactorShare.Rows != productShare.Rows ||
                firstFactorShare.Cols != secondFactorShare.Cols || secondFactorShare.Cols != productShare.Cols)
            {
                throw new ArgumentException("All inputs must have same dimensions.");
            }

            FirstFactorShare = firstFactorShare;
            SecondFactorShare = secondFactorShare;
            ProductShare = productShare;
        }

        public int NumberOfTriples => FirstFactorShare.Rows;
        public int NumberOfTripleBits => FirstFactorShare.Cols;

        public (BitSequence, BitSequence, BitSequence) GetTripleShare(int i)
        {
            if (i < 0 || i >= NumberOfTriples)
                throw new ArgumentOutOfRangeException(nameof(i));

            return (FirstFactorShare.GetRow(i), SecondFactorShare.GetRow(i), ProductShare.GetRow(i));
        }
    }

    class Program
    {
        static readonly int Port = 8694;
        static readonly int NumberOfTriples = 10;

        static void Main(string[] args)
        {
            var channelBuilder = new ObliviousTransferChannelBuilder()
                .WithSecurityLevel(128)
                .WithMaximumNumberOfOptions(2)
                .WithAverageNumberOfOptions(2);

            var firstPartyInputs = new BitMatrix(NumberOfTriples, 1, ConstantBitArrayView.MakeOnes(NumberOfTriples));
            var secondPartyInputs = new BitMatrix(NumberOfTriples, 1, ConstantBitArrayView.MakeOnes(NumberOfTriples));

            var firstPartyTask = RunFirstParty(channelBuilder, firstPartyInputs);
            var secondPartyTask = RunSecondParty(channelBuilder, secondPartyInputs);

            Task.WaitAll(firstPartyTask, secondPartyTask);
            (var firstPartyTripleShares, var firstPartyOutputShares) = firstPartyTask.Result;
            (var secondPartyTripleShares, var secondPartyOutputShares) = secondPartyTask.Result;

            for (int i = 0; i < NumberOfTriples; ++i)
            {
                var firstPartyTripleShare = firstPartyTripleShares.GetTripleShare(i);
                var secondPartyTripleShare = secondPartyTripleShares.GetTripleShare(i);

                Console.WriteLine(
                    $"{i} Triple: ({firstPartyTripleShare.Item1} ^ {secondPartyTripleShare.Item1}) & "+
                    $"({firstPartyTripleShare.Item2} ^ {secondPartyTripleShare.Item2}) = " +
                    $"{(firstPartyTripleShare.Item1 ^ secondPartyTripleShare.Item1)} & " + 
                    $"{(firstPartyTripleShare.Item2 ^ secondPartyTripleShare.Item2)} = " + 
                    $"{(firstPartyTripleShare.Item3 ^ secondPartyTripleShare.Item3)} = " +
                    $"{firstPartyTripleShare.Item3} ^ {secondPartyTripleShare.Item3}" 
                );

                var firstPartyInput = firstPartyInputs.GetRow(i);
                var secondPartyInput = secondPartyInputs.GetRow(i);
                var firstPartyOutputShare = firstPartyOutputShares.GetRow(i);
                var secondPartyOutputShare = secondPartyOutputShares.GetRow(i);

                Console.WriteLine(
                    $"{i} Outputs: {firstPartyInput} & {secondPartyInput} = " +
                    $"{firstPartyOutputShare} ^ {secondPartyOutputShare} = " +
                    $"{(firstPartyOutputShare ^ secondPartyOutputShare)}"
                );
            }
        }

        static async Task<(TripleShareSet, BitMatrix)> RunFirstParty(ObliviousTransferChannelBuilder otChannelBuilder, BitMatrix inputs)
        {
            int numberOfTriples = inputs.Length;
            otChannelBuilder.WithMaximumNumberOfInvocations(2 * numberOfTriples);

            RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
            TcpListener tcpListener = new TcpListener(IPAddress.IPv6Loopback, Port);
            tcpListener.Start();
            using (TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync())
            {
                using (NetworkStream tcpStream = tcpClient.GetStream())
                {
                    var channel = new NetworkStreamMessageChannel(tcpStream);
                    var rotChannel = otChannelBuilder.MakeRandomObliviousTransferChannel(channel);

                    TripleShareSet tripleShares = await MakeTripleFirstParty(rotChannel, numberOfTriples, randomNumberGenerator);

                    BitMatrix outputs = await MultiplyWithTriplesFirstParty(inputs, tripleShares, channel);

                    return (tripleShares, outputs);
                }
            }
        }
        
        static async Task<(TripleShareSet, BitMatrix)> RunSecondParty(ObliviousTransferChannelBuilder otChannelBuilder, BitMatrix inputs)
        {
            int numberOfTriples = inputs.Rows;
            otChannelBuilder.WithMaximumNumberOfInvocations(2 * numberOfTriples);

            RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(IPAddress.IPv6Loopback, Port);
                using (NetworkStream tcpStream = tcpClient.GetStream())
                {
                    var channel = new NetworkStreamMessageChannel(tcpStream);
                    var rotChannel = otChannelBuilder.MakeRandomObliviousTransferChannel(channel);

                    TripleShareSet tripleShares = await MakeTripleSecondParty(rotChannel, numberOfTriples, randomNumberGenerator);

                    BitMatrix outputs = await MultiplyWithTriplesSecondParty(inputs, tripleShares, channel);

                    return (tripleShares, outputs);
                }
            }
        }

#region Generating beaver triples using the random oblivious transfer protocol paradigm

        static async Task<TripleShareSet> MakeTripleFirstParty(IRandomObliviousTransferChannel rotChannel, int numberOfTriples, RandomNumberGenerator randomNumberGenerator)
        {
            (var a, var u) = await MakeHalfTripleFromCOTReceiver(rotChannel, numberOfTriples, randomNumberGenerator);
            (var b, var v) = await MakeHalfTripleFromCOTSender(rotChannel, numberOfTriples);
            var c = (a & b) ^ u ^ v;

            return new TripleShareSet(a, b, c);
        }

        static async Task<TripleShareSet> MakeTripleSecondParty(IRandomObliviousTransferChannel rotChannel, int numberOfTriples, RandomNumberGenerator randomNumberGenerator)
        {
            (var b, var v) = await MakeHalfTripleFromCOTSender(rotChannel, numberOfTriples);
            (var a, var u) = await MakeHalfTripleFromCOTReceiver(rotChannel, numberOfTriples, randomNumberGenerator);
            var c = (a & b) ^ u ^ v;

            return new TripleShareSet(a, b, c);
        }

        static async Task<(BitMatrix, BitMatrix)> MakeHalfTripleFromCOTSender(IRandomObliviousTransferChannel rotChannel, int numberOfTriples)
        {
            var options = await rotChannel.SendAsync(numberOfInvocations: numberOfTriples, numberOfOptions: 2, numberOfMessageBits: 1);
            var v = options.GetOptions(0);
            var b = options.GetOptions(1) ^ v;
            return (b, v);
        }

        static async Task<(BitMatrix, BitMatrix)> MakeHalfTripleFromCOTReceiver(IRandomObliviousTransferChannel rotChannel, int numberOfTriples, RandomNumberGenerator randomNumberGenerator)
        {
            var randomBits = randomNumberGenerator.GetBits(numberOfTriples);

            int[] selectionIndices = randomBits.AsEnumerable().Select(b => (int)b).ToArray();
            var result = await rotChannel.ReceiveAsync(selectionIndices, numberOfOptions: 2, numberOfMessageBits: 1);

            var a = new BitMatrix(numberOfTriples, 1, randomBits);
            return (a, result);
        }

#endregion

#region Secure multi-party And(/binary multiplication) using beaver triples

        static async Task<BitMatrix> OpenShares(BitMatrix partyShares, IMessageChannel channel)
        {
            _ = channel.WriteMessageAsync(partyShares.AsFlat().AsByteEnumerable().ToArray());
            var otherPartyShares = new BitMatrix(
                partyShares.Rows, partyShares.Cols,
                new EnumeratedBitArrayView(await channel.ReadMessageAsync(), partyShares.Rows * partyShares.Cols)
            );

            var opened = partyShares ^ otherPartyShares;
            return opened;
        }

        static async Task<BitMatrix> MultiplyWithTriples(BitMatrix firstInputs, BitMatrix secondInputs, TripleShareSet tripleShares, IMessageChannel channel)
        {
            var alphaShares = firstInputs ^ tripleShares.FirstFactorShare;
            var betaShares = secondInputs ^ tripleShares.SecondFactorShare;
            
            var alphas = await OpenShares(alphaShares, channel);
            var betas = await OpenShares(betaShares, channel);

            var outputs = tripleShares.ProductShare ^ (tripleShares.FirstFactorShare & betas) ^ (tripleShares.SecondFactorShare & alphas) ^ (alphas & betas);
            return outputs;
        }

        static Task<BitMatrix> MultiplyWithTriplesFirstParty(BitMatrix inputs, TripleShareSet tripleShares, IMessageChannel channel)
        {
            return MultiplyWithTriples(inputs, BitMatrix.Zeros(inputs.Rows, inputs.Cols), tripleShares, channel);
        }

        static Task<BitMatrix> MultiplyWithTriplesSecondParty(BitMatrix inputs, TripleShareSet tripleShares, IMessageChannel channel)
        {
            return MultiplyWithTriples(BitMatrix.Zeros(inputs.Rows, inputs.Cols), inputs, tripleShares, channel);
        }

#endregion

    }
}
