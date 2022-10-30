// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Threading.Tasks;
using System.Diagnostics;

using CompactOT.Codes;

namespace CompactOT
{
    /// <summary>
    /// Implementation of the OT extension protocol that enables extending a small number of expensive oblivious transfer
    /// invocation (so called "base OTs") into a larger number of usable oblivious transfer invocations using only cheap
    /// symmetric cryptography via a random oracle instantiation.
    /// </summary>
    /// <remarks>
    /// References:
    /// Main: Vladimir Kolesnikov, Ranjit Kumaresan: Improved OT Extension for Transferring Short Secrets. 2013. https://www.microsoft.com/en-us/research/wp-content/uploads/2017/03/otext_crypto13.pdf
    /// Summary explanation of above in: Michele Orrù, Emmanuela Orsini, Peter Scholl: Actively Secure 1-out-of-N OT Extension with Application to Private Set Intersection. 2017. https://hal.archives-ouvertes.fr/hal-01401005/file/933.pdf
    /// Original 1oo2 OT extension: Yuval Ishai, Joe Kilian, Kobbi Nissim and Erez Petrank: Extending Oblivious Transfers Efficiently. 2003. https://link.springer.com/content/pdf/10.1007/978-3-540-45146-4_9.pdf
    /// </remarks>
    public class ExtendedObliviousTransferChannel : ExtendedObliviousTransferChannelBase, IObliviousTransferChannel
    {

        public ExtendedObliviousTransferChannel(IObliviousTransferChannel baseOT, int securityParameter, CryptoContext cryptoContext, IBinaryCode code)
            : base(baseOT, securityParameter, cryptoContext, code)
        {
        }


        public async Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            var t0 = await base.ReceiverComputeAndSendU(selectionIndices, numberOfOptions, numberOfMessageBits);
            int numberOfInvocations = selectionIndices.Length;
            Debug.Assert(_receiverState != null);
            Debug.Assert(t0.Rows == CodeLength);
            Debug.Assert(t0.Cols == numberOfInvocations);

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif

            var results = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);
            ObliviousTransferOptions maskedOptions = await ReceiveMaskedOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);
#if DEBUG
            DebugUtils.WriteLineReceiver("ExtendedOT", "Receiving masked options took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif
            Debug.Assert(maskedOptions.NumberOfInvocations == numberOfInvocations);

            int totalNumberOfInvocationsOffset = TotalNumberOfInvocations - numberOfInvocations;
            for (int i = 0; i < numberOfInvocations; ++i)
            {
                int s = selectionIndices[i];
                var q = maskedOptions.GetMessage(i, s);
                Debug.Assert(q.Length == numberOfMessageBits);
                var t0Col = t0.GetColumn(i);
                var unmaskedOption = MaskOption(q, t0Col, totalNumberOfInvocationsOffset + i);
                results.SetRow(i, unmaskedOption);
            }
#if DEBUG
            DebugUtils.WriteLineReceiver("ExtendedOT", "Unmasking received options took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            return results;
        }

        public async Task SendAsync(ObliviousTransferOptions options)
        {
            var qs = await base.SenderReceiveUAndComputeQ(options.NumberOfInvocations, options.NumberOfOptions, options.NumberOfMessageBits);
            Debug.Assert(_senderState != null);
            Debug.Assert(qs.Rows == options.NumberOfInvocations);
            Debug.Assert(qs.Cols == CodeLength);

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif

            var numberOfMessageBits = options.NumberOfMessageBits;

            var maskedOptions = new ObliviousTransferOptions(options.NumberOfInvocations, options.NumberOfOptions, numberOfMessageBits);

            int totalNumberOfInvocationsOffset = TotalNumberOfInvocations - options.NumberOfInvocations;
            for (int j = 0; j < options.NumberOfOptions; ++j)
            {
                var selectionCode = _code.Encode(j);
                var queryMask = selectionCode & _senderState!.RandomChoices;
                Debug.Assert(queryMask.Length == CodeLength);

                for (int i = 0; i < options.NumberOfInvocations; ++i)
                {
                    var option = options.GetMessage(i, j);
                    
                    var query = queryMask ^ qs.GetRow(i);
                    var maskedOption = MaskOption(option, query, totalNumberOfInvocationsOffset + i);
                    Debug.Assert(maskedOption.Length == numberOfMessageBits);

                    maskedOptions.SetMessage(i, j, maskedOption);
                }
            }

#if DEBUG
            DebugUtils.WriteLineSender("ExtendedOT", "Masking options took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            await SendMaskedOptions(maskedOptions);
        }

        public override double EstimateCost(ObliviousTransferUsageProjection usageProjection)
        {
            // TODO: currently ignoring computation cost

            if (!usageProjection.HasMaxNumberOfInvocations)
                return double.PositiveInfinity;

            Debug.Assert(usageProjection.HasMaxNumberOfBatches);

            double baseOtAndSecurityExchangeCost = base.EstimateCost(usageProjection);

            // bandwidth cost of exchanging masked options
            double maxNumberOfInvocations = usageProjection.MaxNumberOfInvocations;
            double averageNumberOfOptions = usageProjection.AverageNumberOfOptions;
            double averageMessageBits = usageProjection.AverageMessageBits;
            double optionsExchangeCost = maxNumberOfInvocations * averageNumberOfOptions * usageProjection.AverageMessageBits;

            return baseOtAndSecurityExchangeCost + optionsExchangeCost;
        }

    }
}
