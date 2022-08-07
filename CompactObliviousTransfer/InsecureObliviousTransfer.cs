using System;
using System.Threading.Tasks;
using System.Diagnostics;

using CompactOT.Buffers;
using CompactOT.DataStructures;

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
#if DEBUG
            DebugUtils.WriteLineReceiver("Insecure", "Sending selection indices took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif

            var response = new MessageDecomposer(await channel.ReadMessageAsync());
#if DEBUG
            DebugUtils.WriteLineReceiver("Insecure", "Response received after {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            var receivedOptions = new ObliviousTransferResult(selectionIndices.Length, numberOfMessageBits);
            for (int j = 0; j < selectionIndices.Length; ++j)
            {
                receivedOptions.SetRow(j, response.ReadBitArray(numberOfMessageBits));
            }
            return receivedOptions;
        }

        public async Task SendAsync(IMessageChannel channel, ObliviousTransferOptions options)
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

        public static double EstimateCost(
            ObliviousTransferUsageProjection usageProjection
        )
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