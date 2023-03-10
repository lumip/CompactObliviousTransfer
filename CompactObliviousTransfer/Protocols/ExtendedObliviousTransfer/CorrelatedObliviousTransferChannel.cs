// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Threading.Tasks;
using System.Diagnostics;

using CompactOT.DataStructures;
using CompactOT.Codes;

namespace CompactOT
{
    /// <remarks>
    /// References:
    /// Asharov, Lindell, Schneider, Zohner: More Efficient Oblivious Transfer and Extensions for Faster Secure Computation. 2013. https://thomaschneider.de/papers/ALSZ13.pdf
    /// describes the protocol for 1oo2-OT; adapted for the 1ooN case in this implementation.
    /// </remarks>
    public class CorrelatedObliviousTransferChannel : ExtendedObliviousTransferChannelBase, ICorrelatedObliviousTransferChannel
    {

        public CorrelatedObliviousTransferChannel(IObliviousTransferChannel baseOT, int securityParameter, CryptoContext cryptoContext, IBinaryCode code)
            : base(baseOT, securityParameter, cryptoContext, code)
        {
        }

        public async Task<ObliviousTransferResult> ReceiveAsync(int[] selectionIndices, int numberOfOptions, int numberOfMessageBits)
        {
            var t0 = await ReceiverComputeAndSendU(selectionIndices, numberOfOptions, numberOfMessageBits);
            int numberOfInvocations = selectionIndices.Length;
            int numberOfCorrelations = numberOfOptions - 1;
            Debug.Assert(_receiverState != null);
            Debug.Assert(t0.Rows == CodeLength);
            Debug.Assert(t0.Cols == numberOfInvocations);

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            var results = new ObliviousTransferResult(numberOfInvocations, numberOfMessageBits);
            ObliviousTransferOptions maskedOptions = await ReceiveMaskedOptions(numberOfInvocations, numberOfCorrelations, numberOfMessageBits);
#if DEBUG
            DebugUtils.WriteLineReceiver("CorrelatedOT", "Receiving masked options took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif
            Debug.Assert(maskedOptions.NumberOfInvocations == numberOfInvocations);

            int totalNumberOfInvocationsOffset = TotalNumberOfInvocations - numberOfInvocations;
            Debug.Assert(totalNumberOfInvocationsOffset >= 0);

            for (int i = 0; i < numberOfInvocations; ++i)
            {
                int s = selectionIndices[i];
                BitSequence q;
                if (s == 0)
                {
                    q = ConstantBitArrayView.MakeZeros(numberOfMessageBits);
                }
                else
                {
                    q = maskedOptions.GetMessage(i, s - 1);
                }
                Debug.Assert(q.Length == numberOfMessageBits);
                var t0Col = t0.GetColumn(i);
                var unmaskedOption = MaskOption(q, t0Col, totalNumberOfInvocationsOffset + i);
                results.SetRow(i, unmaskedOption);
            }
#if DEBUG
            DebugUtils.WriteLineReceiver("CorrelatedOT", "Unmasking received options took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            return results;
        }

        public async Task<ObliviousTransferResult> SendAsync(ObliviousTransferOptions correlations)
        {
            int numberOfCorrelations = correlations.NumberOfOptions;
            int numberOfOptions = numberOfCorrelations + 1;
            var qs = await SenderReceiveUAndComputeQ(correlations.NumberOfInvocations, numberOfOptions, correlations.NumberOfMessageBits);
            Debug.Assert(_senderState != null);
            Debug.Assert(qs.Rows == correlations.NumberOfInvocations);
            Debug.Assert(qs.Cols == CodeLength);

            var numberOfMessageBits = correlations.NumberOfMessageBits;

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif
            var firstOptions = new ObliviousTransferResult(correlations.NumberOfInvocations, numberOfMessageBits);
            var maskedOptions = new ObliviousTransferOptions(correlations.NumberOfInvocations, numberOfCorrelations, numberOfMessageBits);

            int totalNumberOfInvocationsOffset = TotalNumberOfInvocations - correlations.NumberOfInvocations;
            Debug.Assert(totalNumberOfInvocationsOffset >= 0);
            
            for (int j = 0; j < numberOfOptions; ++j)
            {
                var selectionCode = _code.Encode(j);
                var queryMask = selectionCode & _senderState!.RandomChoices;
                Debug.Assert(queryMask.Length == CodeLength);

                for (int i = 0; i < correlations.NumberOfInvocations; ++i)
                {
                    var query = queryMask ^ qs.GetRow(i);

                    if (j == 0)
                    {
                        var maskedOption = MakeRandomOption(correlations.NumberOfMessageBits, query, totalNumberOfInvocationsOffset + i);
                        Debug.Assert(maskedOption.Length == numberOfMessageBits);
                        firstOptions.SetRow(i, maskedOption);
                    }
                    else
                    {
                        var option = firstOptions.GetRow(i) ^ correlations.GetMessage(i, j - 1);
                        var maskedOption = MaskOption(option, query, totalNumberOfInvocationsOffset + i);
                        Debug.Assert(maskedOption.Length == numberOfMessageBits);

                        maskedOptions.SetMessage(i, j - 1, maskedOption);
                    }
                }
            }

#if DEBUG
            DebugUtils.WriteLineSender("CorrelatedOT", "Masking options took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif
            await SendMaskedOptions(maskedOptions);

            return firstOptions;
        }

        public override double EstimateCost(ObliviousTransferUsageProjection usageProjection)
        {
            // TODO: currently ignoring computation cost

            if (!usageProjection.HasMaxNumberOfInvocations)
                return double.PositiveInfinity;

            Debug.Assert(usageProjection.HasMaxNumberOfBatches);

            double baseOtAndSecurityExchangeCost = base.EstimateCost(usageProjection);

            // bandwidth cost of exchanging masked correlations
            double maxNumberOfInvocations = usageProjection.MaxNumberOfInvocations;
            double averageNumberOfOptions = usageProjection.AverageNumberOfOptions;
            double averageMessageBits = usageProjection.AverageMessageBits;
            double correlationsExchangeCost = maxNumberOfInvocations * (averageNumberOfOptions - 1.0) * usageProjection.AverageMessageBits;

            return baseOtAndSecurityExchangeCost + correlationsExchangeCost;
        }

    }
}
