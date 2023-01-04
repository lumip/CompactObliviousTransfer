// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Threading.Tasks;
using System.Diagnostics;

using CompactOT.Buffers;

namespace CompactOT
{

    public class InsecureObliviousTransfer : IStatelessObliviousTransfer
    {

        public virtual int SecurityLevel => 0;
        // TODO: Only virtual because it is used for base OTs during tests of OT extension protocols.
        //              Should arguably not be virtual for regular use. However, do not want to duplicate all
        //              this code in tests project again.. What to do?

        public async Task<ObliviousTransferResult> ReceiveAsync(IMessageChannel channel, int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
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

            var response = new MessageDecomposer(await channel.ReadMessageAsync());
            var receivedOptions = new ObliviousTransferResult(selectionIndices.Length, numberOfMessageBits);
            for (int j = 0; j < selectionIndices.Length; ++j)
            {
                receivedOptions.SetRow(j, response.ReadBitArray(numberOfMessageBits));
            }
            return receivedOptions;
        }

        public async Task SendAsync(IMessageChannel channel, ObliviousTransferOptions options)
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