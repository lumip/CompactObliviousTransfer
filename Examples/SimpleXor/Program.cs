using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;

using CompactOT;
using CompactOT.DataStructures;

namespace CompactOT.Examples.SimpleXor
{
    class Program
    {
        static readonly int Port = 8693;

        static void Main(string[] args)
        {
            var channelBuilder = new ObliviousTransferChannelBuilder()
                .WithSecurityLevel(128)
                .WithMaximumNumberOfOptions(4)
                .WithAverageNumberOfOptions(4)
                .WithMaximumNumberOfInvocations(1);

            BitArray senderInput = BitArray.FromBinaryString("01");
            BitArray receiverInput = BitArray.FromBinaryString("11");
            Task senderTask = ExecuteSender(channelBuilder, senderInput);
            Task<BitSequence> receiverTask = ExecuteReceiver(channelBuilder, receiverInput);
            Task.WaitAll(senderTask, receiverTask);
            BitSequence receiverResult = receiverTask.Result;
            Console.WriteLine($"Sender input {senderInput.ToBinaryString()} and receiver input" + 
                $" {receiverInput.ToBinaryString()}. Receiver received: {receiverResult.ToBinaryString()}.");
        }

        static async Task ExecuteSender(ObliviousTransferChannelBuilder otChannelBuilder, BitSequence senderInput)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.IPv6Loopback, Port);
            tcpListener.Start();
            using (TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync())
            {
                using (NetworkStream tcpStream = tcpClient.GetStream())
                {
                    var channel = new NetworkStreamMessageChannel(tcpStream);
                    var otChannel = otChannelBuilder.MakeObliviousTransferChannel(channel);

                    var options = new ObliviousTransferOptions(numberOfInvocations: 1, numberOfOptions: 4, numberOfMessageBits: 2);
                    
                    // var receiverInputCandidates = new BitArray[] {
                    //     BitArray.FromBinaryString("00"), BitArray.FromBinaryString("01"),
                    //     BitArray.FromBinaryString("10"), BitArray.FromBinaryString("11"),
                    // };
                    // foreach ((int index, var receiverInputCandidate) in receiverInputCandidates.Enumerate())
                    // {
                    //     options.SetMessage(0, index, receiverInputCandidate ^ senderInput);
                    // }

                    var receiverInputCandidates = BitArray.FromBinaryString("00011011");
                    var loopedSenderInput = new EnumeratedBitArrayView(senderInput.AsEnumerable().Tile(4), 8);
                    var messageOptions = receiverInputCandidates ^ loopedSenderInput;
                    options.SetInvocation(0, messageOptions);

                    await otChannel.SendAsync(options);
                }
            }
        }

        static async Task<BitSequence> ExecuteReceiver(ObliviousTransferChannelBuilder otChannelBuilder, BitSequence receiverInput)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(IPAddress.IPv6Loopback, Port);
                using (NetworkStream tcpStream = tcpClient.GetStream())
                {
                    var channel = new NetworkStreamMessageChannel(tcpStream);
                    var otChannel = otChannelBuilder.MakeObliviousTransferChannel(channel);

                    int selectionIndex = (int)receiverInput.AsByteEnumerable().First();
                    var otResult = await otChannel.ReceiveAsync(new int[] { selectionIndex }, numberOfOptions: 4, numberOfMessageBits: 2);
                    return otResult.GetInvocationResult(0);
                }
            }
        }
    }
}
