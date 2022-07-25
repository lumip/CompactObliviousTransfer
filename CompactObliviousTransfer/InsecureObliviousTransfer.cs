using System;
using System.Threading.Tasks;
using System.Linq;

using CompactOT.Buffers;
using CompactOT.DataStructures;

namespace CompactOT
{

    public class InsecureObliviousTransfer : StatelessObliviousTransfer
    {

        public override int SecurityLevel => 0;

        public override async Task<BitMatrix> ReceiveAsync(IMessageChannel channel, int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            var message = new MessageComposer();
            foreach (int index in selectionIndices)
            {
                message.Write(index);
            }
            await channel.WriteMessageAsync(message.Compose());

            var response = new MessageDecomposer(await channel.ReadMessageAsync());
            BitMatrix receivedOptions = new BitMatrix(selectionIndices.Length, numberOfMessageBits);
            for (int j = 0; j < selectionIndices.Length; ++j)
            {
                receivedOptions.SetRow(j, new EnumeratedBitArrayView(response.ReadBuffer(numberOfMessageBits / 8), numberOfMessageBits));
            }
            return receivedOptions;
        }

        public override async Task SendAsync(IMessageChannel channel, ObliviousTransferOptions options)
        {
            var indexMessage = new MessageDecomposer(await channel.ReadMessageAsync());

            var transferMessage = new MessageComposer(options.NumberOfInvocations);
            for (int j = 0; j < options.NumberOfInvocations; ++j)
            {
                int index = indexMessage.ReadInt();
                transferMessage.Write(options.GetMessage(j, index));
            }
            await channel.WriteMessageAsync(transferMessage.Compose());
        }
    }

}