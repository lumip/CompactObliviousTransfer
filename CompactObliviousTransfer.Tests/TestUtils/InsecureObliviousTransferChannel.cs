// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using CompactOT.Buffers;

namespace CompactOT
{

    public class InsecureObliviousTransferChannel : IObliviousTransferChannel
    {

        public virtual IMessageChannel Channel { get; set; }

        public InsecureObliviousTransferChannel(IMessageChannel channel)
        {
            Channel = channel;
        }

        public virtual int SecurityLevel => 0;

        public async Task<ObliviousTransferResult> ReceiveAsync(
            int[] selectionIndices, int numberOfOptions, int numberOfMessageBits, CancellationToken cancellationToken
        )
        {
#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            var message = new MessageComposer();
            foreach (int index in selectionIndices)
            {
                message.Write(index);
            }
            await Channel.WriteMessageAsync(message.Compose(), cancellationToken);

            var response = new MessageDecomposer(await Channel.ReadMessageAsync(cancellationToken));
            var receivedOptions = new ObliviousTransferResult(selectionIndices.Length, numberOfMessageBits);
            for (int j = 0; j < selectionIndices.Length; ++j)
            {
                receivedOptions.SetRow(j, response.ReadBitArray(numberOfMessageBits));
            }
            return receivedOptions;
        }

        public async Task SendAsync(ObliviousTransferOptions options, CancellationToken cancellationToken)
        {
            if (Channel == null)
                throw new InvalidOperationException("SendAsync cannot be executed while Channel is null.");
            var indexMessage = new MessageDecomposer(await Channel.ReadMessageAsync(cancellationToken));
            var transferMessage = new MessageComposer(options.NumberOfInvocations);
            for (int j = 0; j < options.NumberOfInvocations; ++j)
            {
                int index = indexMessage.ReadInt();
                transferMessage.Write(options.GetMessage(j, index));
            }
            await Channel.WriteMessageAsync(transferMessage.Compose(), cancellationToken);
        }

        public double EstimateCost(ObliviousTransferUsageProjection usageProjection)
        {
            if (!usageProjection.HasMaxNumberOfInvocations)
                return double.PositiveInfinity;

            double numberOfOptions = usageProjection.AverageNumberOfOptions;
            double maxNumberOfInvocations = usageProjection.MaxNumberOfInvocations;
            double averageMessageBits = usageProjection.AverageMessageBits;

            // bandwidth cost of exchanging selection choices
            double bitsPerChoice = 32.0;
            double selectionExchangeCost = numberOfOptions * bitsPerChoice;

            // bandwidth cost of exchanging masked options
            double optionsExchangeCost = maxNumberOfInvocations * averageMessageBits;

            return selectionExchangeCost + optionsExchangeCost;
        }

    }

}
