using System;
using System.Threading.Tasks;
using System.Diagnostics;

using CompactOT.Buffers;
using CompactOT.DataStructures;

namespace CompactOT
{

    public class InsecureObliviousTransfer : StatelessObliviousTransfer
    {

        public override int SecurityLevel => 0;

        public override async Task<BitMatrix> ReceiveAsync(IMessageChannel channel, int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            var message = new MessageComposer();
            foreach (int index in selectionIndices)
            {
                message.Write(index);
            }
            await channel.WriteMessageAsync(message.Compose());
#if DEBUG
            DebugUtils.WriteLineReceiver("Insecure", "Sending selection indices took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif

            var response = new MessageDecomposer(await channel.ReadMessageAsync());
#if DEBUG
            DebugUtils.WriteLineReceiver("Insecure", "Response received after {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            BitMatrix receivedOptions = new BitMatrix(selectionIndices.Length, numberOfMessageBits);
            for (int j = 0; j < selectionIndices.Length; ++j)
            {
                receivedOptions.SetRow(j, new EnumeratedBitArrayView(response.ReadBuffer(numberOfMessageBits / 8), numberOfMessageBits));
            }
            return receivedOptions;
        }

        public override async Task SendAsync(IMessageChannel channel, ObliviousTransferOptions options)
        {
#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            var indexMessage = new MessageDecomposer(await channel.ReadMessageAsync());
#if DEBUG
            DebugUtils.WriteLineSender("Insecure", "Selection indices message received after {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif

            var transferMessage = new MessageComposer(options.NumberOfInvocations);
            for (int j = 0; j < options.NumberOfInvocations; ++j)
            {
                int index = indexMessage.ReadInt();
                transferMessage.Write(options.GetMessage(j, index));
            }
            await channel.WriteMessageAsync(transferMessage.Compose());
#if DEBUG
            DebugUtils.WriteLineSender("Insecure", "Sending selected messages took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
        }
    }

}