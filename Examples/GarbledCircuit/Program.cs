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

    static class GateBuilder
    {
        
        public static SingleInputGate MakeConstant(BitSequence in0, BitSequence out_, BitSequence delta)
        {
            return new SingleInputGate(new Dictionary<BitSequence, BitSequence>()
                {
                    { in0, out_ },
                    { in0 ^ delta, out_ }
                }
            );
        }

        public static SingleInputGate MakeIdentity(BitSequence in0, BitSequence out0, BitSequence delta)
        {
            return new SingleInputGate(new Dictionary<BitSequence, BitSequence>()
                {
                    { in0, out0 },
                    { in0 ^ delta, out0 ^ delta }
                }
            );
        }

        public static SingleInputGate MakeInverter(BitSequence in0, BitSequence out0, BitSequence delta)
        {
            return MakeIdentity(in0, out0 ^ delta, delta);
        }

        public static BitSequence MakeFreeXor(BitSequence firstIn0, BitSequence secondIn0)
        {
            return firstIn0 ^ secondIn0;
        }

        public static GenericDoubleInputGate MakeAnd(
            BitSequence firstIn0, BitSequence secondIn0,
            BitSequence out0, BitSequence delta
        )
        {
            return new GenericDoubleInputGate(new Dictionary<BitSequence, SingleInputGate>()
                {
                    { firstIn0, MakeConstant(secondIn0, out0, delta) },
                    { firstIn0 ^ delta, MakeIdentity(secondIn0, out0, delta) }
                }
            );
        }

    }

    class SingleInputGate
    {
        private Dictionary<BitSequence, BitSequence> _gateLookup;

        public SingleInputGate(Dictionary<BitSequence, BitSequence> gateLookup)
        {
            _gateLookup = gateLookup;
        }

        public BitSequence Apply(BitSequence input)
        {
            return _gateLookup[input];
        }

        public BitSequence SerializeToBits(RandomNumberGenerator randomNumberGenerator)
        {
            BitSequence[] lookupSequences = _gateLookup.Select(item => item.Key.Concatenate(item.Value)).ToArray();

            int flip = (int)randomNumberGenerator.GetBits(1)[0];
            return lookupSequences[flip].Concatenate(lookupSequences[1 - flip]);
        }

        public byte[] SerializeToBytes(RandomNumberGenerator randomNumberGenerator)
        {
            var bits = SerializeToBits(randomNumberGenerator);
            
            var wireValueLength = bits.Length / 4;
            var wireValueLengthBits = BitArray.FromBytes(BitConverter.GetBytes(wireValueLength), 32);

            return BitSequence.Empty.Concatenate(wireValueLengthBits).Concatenate(bits).ToBytes();
        }

        public static SingleInputGate Deserialize(byte[] bytes)
        {
            int wireValueLength = BitConverter.ToInt32(bytes, 0);
            var wireValues = BitArray.FromBytes(bytes, 4 * wireValueLength, 4);
            return Deserialize(wireValues);
        }

        public static SingleInputGate Deserialize(BitSequence bits)
        {
            var wireValues = bits;
            int wireValueLength = bits.Length / 4;
            var firstKey  = new BitArraySlice(wireValues, 0 * wireValueLength, 1 * wireValueLength);
            var firstVal  = new BitArraySlice(wireValues, 1 * wireValueLength, 2 * wireValueLength);
            var secondKey = new BitArraySlice(wireValues, 2 * wireValueLength, 3 * wireValueLength);
            var secondVal = new BitArraySlice(wireValues, 3 * wireValueLength, 4 * wireValueLength);

            return new SingleInputGate(new Dictionary<BitSequence, BitSequence>
                {
                    { firstKey, firstVal },
                    { secondKey, secondVal }
                }
            );
        }

    }

    interface DoubleInputGate
    {
        public BitSequence Apply(BitSequence firstInput, BitSequence secondInput);
    }

    class FreeXorGate : DoubleInputGate
    {
        public BitSequence Apply(BitSequence firstInput, BitSequence secondInput)
        {
            return firstInput ^ secondInput;
        }

    }

    class GenericDoubleInputGate : DoubleInputGate
    {
        private Dictionary<BitSequence, SingleInputGate> _gateLookup;

        public GenericDoubleInputGate(Dictionary<BitSequence, SingleInputGate> gateLookup)
        {
            _gateLookup = gateLookup;
        }

        public BitSequence Apply(BitSequence firstInput, BitSequence secondInput)
        {
            return _gateLookup[firstInput].Apply(secondInput);
        }

        
        public BitSequence SerializeToBits(RandomNumberGenerator randomNumberGenerator)
        {
            BitSequence[] lookupSequences = _gateLookup.Select(
                item => item.Key.Concatenate(item.Value.SerializeToBits(randomNumberGenerator))
            ).ToArray();

            int flip = (int)randomNumberGenerator.GetBits(1)[0];
            return lookupSequences[flip].Concatenate(lookupSequences[1 - flip]);
        }

        public byte[] SerializeToBytes(RandomNumberGenerator randomNumberGenerator)
        {
            var bits = SerializeToBits(randomNumberGenerator);
            
            int numWireValues = 10;
            int wireValueLength = bits.Length / numWireValues;
            var wireValueLengthBits = BitArray.FromBytes(BitConverter.GetBytes(wireValueLength), 32);

            return BitSequence.Empty.Concatenate(wireValueLengthBits).Concatenate(bits).ToBytes();
        }

        public static GenericDoubleInputGate Deserialize(BitSequence bits)
        {
            int numWireValues = 10;
            int wireValueLength = bits.Length / numWireValues;

            var firstKey = new BitArraySlice(bits, 0 * wireValueLength, 1 * wireValueLength);
            var firstSubGateBits = new BitArraySlice(bits, 1 * wireValueLength, 5 * wireValueLength);
            var firstSubGate = SingleInputGate.Deserialize(firstSubGateBits);

            var secondKey = new BitArraySlice(bits, 5 * wireValueLength, 6 * wireValueLength);
            var secondSubGateBits = new BitArraySlice(bits, 6 * wireValueLength, 10 * wireValueLength);
            var secondSubGate = SingleInputGate.Deserialize(secondSubGateBits);

            return new GenericDoubleInputGate(new Dictionary<BitSequence, SingleInputGate>
                {
                    { firstKey, firstSubGate },
                    { secondKey, secondSubGate },
                }
            );
        }

        public static GenericDoubleInputGate Deserialize(byte[] bytes)
        {
            int wireValueLength = BitConverter.ToInt32(bytes, 0);
            var bits = BitArray.FromBytes(bytes, wireValueLength * 10, 4);
            return Deserialize(bits);
        }

    }

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

                    var wireZeroValueLsbXorLsb = GateBuilder.MakeFreeXor(
                        wireZeroValueFirstPartyInputLsb, wireZeroValueSecondPartyInputLsb
                    );

                    var wireZeroValueMsbXorMsb = GateBuilder.MakeFreeXor(
                        wireZeroValueFirstPartyInputMsb, wireZeroValueSecondPartyInputMsb
                    );

                    var wireZeroValueLsbAndLsb = randomNumberGenerator.GetBits(wireValueLength);
                    var gateLsbAndLsb = GateBuilder.MakeAnd(
                        wireZeroValueFirstPartyInputLsb, wireZeroValueSecondPartyInputLsb,
                        wireZeroValueLsbAndLsb, wireDelta
                    );
                    var wireZeroValueCarryLsb = wireZeroValueLsbAndLsb;

                    var wireZeroValueMsbXorMsbXorCarryLsb = GateBuilder.MakeFreeXor(
                        wireZeroValueMsbXorMsb, wireZeroValueCarryLsb
                    );

                    var wireZeroValueMsbAndMsb = randomNumberGenerator.GetBits(wireValueLength);
                    var gateMsbAndMasb = GateBuilder.MakeAnd(
                        wireZeroValueFirstPartyInputMsb, wireZeroValueSecondPartyInputMsb,
                        wireZeroValueMsbAndMsb, wireDelta
                    );

                    var wireZeroValueMsbXorMsbAndCarryLsb = randomNumberGenerator.GetBits(wireValueLength);
                    var gateMsbXorMsbAndCarryLsb = GateBuilder.MakeAnd(
                        wireZeroValueMsbXorMsb, wireZeroValueCarryLsb,
                        wireZeroValueMsbXorMsbAndCarryLsb, wireDelta
                    );

                    var wireZeroValueCarryMsb = GateBuilder.MakeFreeXor(
                        wireZeroValueMsbXorMsbAndCarryLsb, wireZeroValueMsbAndMsb
                    );

                    var wireZeroValueOutputLsb = wireZeroValueLsbXorLsb;
                    var wireZeroValueOutputMsb = wireZeroValueMsbXorMsbXorCarryLsb;
                    var wireZeroValueOutputCarry = wireZeroValueCarryMsb;

                    var gateLsbAndLsbSerialized = gateLsbAndLsb.SerializeToBytes(randomNumberGenerator);
                    await channel.WriteMessageAsync(gateLsbAndLsbSerialized);

                    var gateMsbAndMsbSerialized = gateMsbAndMasb.SerializeToBytes(randomNumberGenerator);
                    await channel.WriteMessageAsync(gateMsbAndMsbSerialized);

                    var gateMsbXorMsbAndCarryLsbSerialized = gateMsbXorMsbAndCarryLsb.SerializeToBytes(randomNumberGenerator);
                    await channel.WriteMessageAsync(gateMsbXorMsbAndCarryLsbSerialized);

                    var circuitWireMap = BitSequence.Empty
                                            .Concatenate(wireValueFirstPartyInputLsb)
                                            .Concatenate(wireValueFirstPartyInputMsb)
                                            .Concatenate(wireZeroValueOutputLsb)
                                            .Concatenate(wireZeroValueOutputMsb)
                                            .Concatenate(wireZeroValueOutputCarry);
                    await channel.WriteMessageAsync(circuitWireMap.ToBytes());
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
                    var gateLsbAndLsb = GenericDoubleInputGate.Deserialize(gateLsbAndLsbSerialized);

                    var gateMsbAndMsbSerialized = await channel.ReadMessageAsync();
                    var gateMsbAndMsb = GenericDoubleInputGate.Deserialize(gateMsbAndMsbSerialized);

                    var gateMsbXorMsbAndCarryLsbSerialized = await channel.ReadMessageAsync();
                    var gateMsbXorMsbAndCarryLsb = GenericDoubleInputGate.Deserialize(gateMsbXorMsbAndCarryLsbSerialized);

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
