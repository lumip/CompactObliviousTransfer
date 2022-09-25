using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

using CompactOT.DataStructures;

namespace CompactOT.Examples.GarbledCircuit
{
    class Program
    {
        static readonly int Port = 8694;
        static void Main(string[] args)
        {
            var channelBuilder = new ObliviousTransferChannelBuilder()
                .WithSecurityLevel(128)
                .WithMaximumNumberOfOptions(2)
                .WithAverageNumberOfOptions(2)
                .WithMaximumNumberOfInvocations(2);

            var firstPartyInput = BitArray.FromBinaryString("11");
            var secondPartyInput = BitArray.FromBinaryString("10");
            var firstPartyTask = RunFirstParty(channelBuilder, firstPartyInput);
            var secondPartyTask = RunSecondParty(channelBuilder, secondPartyInput);

            Task.WaitAll(firstPartyTask, secondPartyTask);

            Console.WriteLine($"{firstPartyInput} + {secondPartyInput} = {secondPartyTask.Result}");
        }
        
        static async Task RunFirstParty(ObliviousTransferChannelBuilder otChannelBuilder, BitArray input)
        {
            RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();

            TcpListener tcpListener = new TcpListener(IPAddress.IPv6Loopback, Port);
            tcpListener.Start();
            using (TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync())
            {
                using (NetworkStream tcpStream = tcpClient.GetStream())
                {
                    var channel = new NetworkStreamMessageChannel(tcpStream);
                    var cotChannel = otChannelBuilder.MakeCorrelatedObliviousTransferChannel(channel);

                    int wireValueLength = cotChannel.SecurityLevel;
                    BitArray wireDelta;
                    do
                    {
                        wireDelta = randomNumberGenerator.GetBits(wireValueLength);
                    } while (wireDelta.IsZero);
                    
                    var correlations = new ObliviousTransferOptions(2, 1, wireValueLength);
                    correlations.SetMessage(0, 0, wireDelta);
                    correlations.SetMessage(1, 0, wireDelta);
                    var secondPartyInputWireZeroValues = await cotChannel.SendAsync(correlations);

                    var wireZeroValueSecondPartyInputLsb = secondPartyInputWireZeroValues.GetRow(0);
                    var wireZeroValueSecondPartyInputMsb = secondPartyInputWireZeroValues.GetRow(1);

                    var wireZeroValueFirstPartyInputLsb = randomNumberGenerator.GetBits(wireValueLength);
                    var wireZeroValueFirstPartyInputMsb = randomNumberGenerator.GetBits(wireValueLength);

                    var wireValueFirstPartyInputLsb = wireZeroValueFirstPartyInputLsb ^ (input[0] & wireDelta);
                    var wireValueFirstPartyInputMsb = wireZeroValueFirstPartyInputMsb ^ (input[1] & wireDelta);

                    var gateLsbXorLsb = new FreeXorGate();
                    var wireZeroValueLsbXorLsb = gateLsbXorLsb.Apply(
                        wireZeroValueFirstPartyInputLsb, wireZeroValueSecondPartyInputLsb
                    );

                    var gateMsbXorMsb = new FreeXorGate();
                    var wireZeroValueMsbXorMsb = gateMsbXorMsb.Apply(
                        wireZeroValueFirstPartyInputMsb, wireZeroValueSecondPartyInputMsb
                    );

                    var gateLsbAndLsb = new SenderAndGate(randomNumberGenerator, wireDelta);

                    var wireZeroValueLsbAndLsb = gateLsbAndLsb.Apply(
                        wireZeroValueFirstPartyInputLsb, wireZeroValueSecondPartyInputLsb
                    );
                    var wireZeroValueCarryLsb = wireZeroValueLsbAndLsb;

                    var gateMsbXorMsbXorCarryLsb = new FreeXorGate();
                    var wireZeroValueMsbXorMsbXorCarryLsb = gateMsbXorMsbXorCarryLsb.Apply(
                        wireZeroValueMsbXorMsb, wireZeroValueCarryLsb
                    );

                    var gateMsbAndMsb = new SenderAndGate(randomNumberGenerator, wireDelta);
                    var wireZeroValueMsbAndMsb = gateMsbAndMsb.Apply(
                        wireZeroValueFirstPartyInputMsb, wireZeroValueSecondPartyInputMsb
                    );

                    var gateMsbXorMsbAndCarryLsb = new SenderAndGate(randomNumberGenerator, wireDelta);
                    var wireZeroValueMsbXorMsbAndCarryLsb = gateMsbXorMsbAndCarryLsb.Apply(
                        wireZeroValueMsbXorMsb, wireZeroValueCarryLsb
                    );

                    var gateCarryMsb = new FreeXorGate();
                    var wireZeroValueCarryMsb = gateCarryMsb.Apply(
                        wireZeroValueMsbXorMsbAndCarryLsb, wireZeroValueMsbAndMsb
                    );

                    var wireZeroValueOutputLsb = wireZeroValueLsbXorLsb;
                    var wireZeroValueOutputMsb = wireZeroValueMsbXorMsbXorCarryLsb;
                    var wireZeroValueOutputCarry = wireZeroValueCarryMsb;

                    var gateLsbAndLsbSerialized = gateLsbAndLsb.SerializeToBytes(randomNumberGenerator);
                    await channel.WriteMessageAsync(gateLsbAndLsbSerialized);

                    var gateMsbAndMsbSerialized = gateMsbAndMsb.SerializeToBytes(randomNumberGenerator);
                    await channel.WriteMessageAsync(gateMsbAndMsbSerialized);

                    var gateMsbXorMsbAndCarryLsbSerialized = gateMsbXorMsbAndCarryLsb.SerializeToBytes(randomNumberGenerator);
                    await channel.WriteMessageAsync(gateMsbXorMsbAndCarryLsbSerialized);

                    var circuitSerialization = BitSequence.Empty
                                            .Concatenate(wireValueFirstPartyInputLsb)
                                            .Concatenate(wireValueFirstPartyInputMsb)
                                            .Concatenate(wireZeroValueOutputLsb)
                                            .Concatenate(wireZeroValueOutputMsb)
                                            .Concatenate(wireZeroValueOutputCarry);
                    await channel.WriteMessageAsync(circuitSerialization.ToBytes());
                }
            }
        }

        static async Task<BitArray> RunSecondParty(ObliviousTransferChannelBuilder otChannelBuilder, BitArray input)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(IPAddress.IPv6Loopback, Port);
                using (NetworkStream tcpStream = tcpClient.GetStream())
                {
                    var channel = new NetworkStreamMessageChannel(tcpStream);
                    var cotChannel = otChannelBuilder.MakeCorrelatedObliviousTransferChannel(channel);
                    int wireValueLength = cotChannel.SecurityLevel;

                    var wireValuesInputs = await cotChannel.ReceiveAsync(input.ToSelectionIndices().ToArray(), 2, wireValueLength);
                    var wireValueSecondPartyInputLsb = wireValuesInputs.GetRow(0);
                    var wireValueSecondPartyInputMsb = wireValuesInputs.GetRow(1);

                    var gateLsbAndLsbSerialized = await channel.ReadMessageAsync();
                    var gateLsbAndLsb = ReceiverAndGate.Deserialize(gateLsbAndLsbSerialized);

                    var gateMsbAndMsbSerialized = await channel.ReadMessageAsync();
                    var gateMsbAndMsb = ReceiverAndGate.Deserialize(gateMsbAndMsbSerialized);

                    var gateMsbXorMsbAndCarryLsbSerialized = await channel.ReadMessageAsync();
                    var gateMsbXorMsbAndCarryLsb = ReceiverAndGate.Deserialize(gateMsbXorMsbAndCarryLsbSerialized);

                    var circuitWireMapBytes = await channel.ReadMessageAsync();
                    var circuitWireMap = BitArray.FromBytes(circuitWireMapBytes, 5 * wireValueLength);

                    var wireValueFirstPartyInputLsb = new BitArraySlice(circuitWireMap, 0 * wireValueLength, 1 * wireValueLength);
                    var wireValueFirstPartyInputMsb = new BitArraySlice(circuitWireMap, 1 * wireValueLength, 2 * wireValueLength);
                    var wireZeroValueOutputLsb = new BitArraySlice(circuitWireMap, 2 * wireValueLength, 3 * wireValueLength);
                    var wireZeroValueOutputMsb = new BitArraySlice(circuitWireMap, 3 * wireValueLength, 4 * wireValueLength);
                    var wireZeroValueOutputCarry = new BitArraySlice(circuitWireMap, 4 * wireValueLength, 5 * wireValueLength);

                    var wireValueLsbXorLsb = new FreeXorGate().Apply(wireValueFirstPartyInputLsb, wireValueSecondPartyInputLsb);
                    var wireValueOutputLsb = wireValueLsbXorLsb;

                    var wireValueLsbAndLsb = gateLsbAndLsb.Apply(wireValueFirstPartyInputLsb, wireValueSecondPartyInputLsb);
                    var wireValueCarryLsb = wireValueLsbAndLsb;

                    var wireValueMsbXorMsb = new FreeXorGate().Apply(wireValueFirstPartyInputMsb, wireValueSecondPartyInputMsb);
                    var wireValueMsbXorMsbXorCarryLsb = new FreeXorGate().Apply(
                        wireValueMsbXorMsb, wireValueCarryLsb
                    );
                    var wireValueOutputMsb = wireValueMsbXorMsbXorCarryLsb;

                    var wireValueMsbXorMsbAndCarryLsb = gateMsbXorMsbAndCarryLsb.Apply(
                        wireValueMsbXorMsb, wireValueCarryLsb
                    );
                    
                    var wireValueMsbAndMsb = gateMsbAndMsb.Apply(wireValueFirstPartyInputMsb, wireValueSecondPartyInputMsb);

                    var wireValueOutputCarry = new FreeXorGate().Apply(
                        wireValueMsbXorMsbAndCarryLsb, wireValueMsbAndMsb
                    );

                    BitArray output = new BitArray(new Bit[] {
                        new Bit(!wireValueOutputLsb.Equals(wireZeroValueOutputLsb)),
                        new Bit(!wireValueOutputMsb.Equals(wireZeroValueOutputMsb)),
                        new Bit(!wireValueOutputCarry.Equals(wireZeroValueOutputCarry)),
                    });
                    return output;
                }
            }
        }
    }
}
