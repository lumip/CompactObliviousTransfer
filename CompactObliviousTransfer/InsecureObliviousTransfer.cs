using System;
using System.Threading.Tasks;
using System.Linq;

using CompactOT.Buffers;

namespace CompactOT
{

    public class InsecureObliviousTransfer : StatelessObliviousTransfer
    {

        public override int SecurityLevel => 0;

        public override async Task<byte[][]> ReceiveAsync(IMessageChannel channel, int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            var message = new MessageComposer();
            foreach (int index in selectionIndices)
            {
                message.Write(index);
            }
            await channel.WriteMessageAsync(message.Compose());

            var response = new MessageDecomposer(await channel.ReadMessageAsync());
            byte[][] receivedOptions = new byte[selectionIndices.Length][];
            for (int j = 0; j < selectionIndices.Length; ++j)
            {
                receivedOptions[j] = response.ReadBuffer(numberOfMessageBits * 8);
            }
            return receivedOptions;
        }

        public override async Task SendAsync(IMessageChannel channel, ObliviousTransferOptions<byte> options)
        {
            var indexMessage = new MessageDecomposer(await channel.ReadMessageAsync());

            var transferMessage = new MessageComposer(options.NumberOfInvocations);
            for (int j = 0; j < options.NumberOfInvocations; ++j)
            {
                int index = indexMessage.ReadInt();
                transferMessage.Write(options.GetMessageOption(j, index).ToArray());
            }
            await channel.WriteMessageAsync(transferMessage.Compose());
        }
    }

}