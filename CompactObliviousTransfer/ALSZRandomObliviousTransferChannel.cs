using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;

using CompactOT.Codes;

namespace CompactOT
{
    /// <remarks>
    /// References:
    /// Asharov, Lindell, Schneider, Zohner: More Efficient Oblivious Transfer and Extensions for Faster Secure Computation. 2013. https://thomaschneider.de/papers/ALSZ13.pdf
    /// describes the protocol for 1oo2-OT; adapted for the 1ooN case in this implementation.
    /// </remarks>
    public class ALSZRandomObliviousTransferChannel : ExtendedObliviousTransferChannelBase, IRandomObliviousTransferChannel
    {

        public ALSZRandomObliviousTransferChannel(IObliviousTransferChannel baseOT, int securityParameter, CryptoContext cryptoContext, IBinaryCode code)
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

            int totalNumberOfInvocationsOffset = TotalNumberOfInvocations - numberOfInvocations;
            Debug.Assert(totalNumberOfInvocationsOffset >= 0);

            for (int i = 0; i < numberOfInvocations; ++i)
            {
                var t0Col = t0.GetColumn(i);
                var unmaskedOption = MakeRandomOption(numberOfMessageBits, t0Col, totalNumberOfInvocationsOffset + i);
                results.SetRow(i, unmaskedOption);
            }
#if DEBUG
            DebugUtils.WriteLineReceiver("RandomOT", "Computing outputs took {0} ms.", stopwatch.ElapsedMilliseconds);
            stopwatch.Reset();
#endif
            return results;
        }

        public async Task<ObliviousTransferOptions> SendAsync(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            var qs = await base.SenderReceiveUAndComputeQ(numberOfInvocations, numberOfOptions, numberOfMessageBits);
            Debug.Assert(_senderState != null);
            Debug.Assert(qs.Rows == numberOfInvocations);
            Debug.Assert(qs.Cols == CodeLength);

#if DEBUG
            Stopwatch stopwatch = Stopwatch.StartNew();
#endif

            var randomOptions = new ObliviousTransferOptions(numberOfInvocations, numberOfOptions, numberOfMessageBits);

            int totalNumberOfInvocationsOffset = TotalNumberOfInvocations - numberOfInvocations;
            for (int j = 0; j < numberOfOptions; ++j)
            {
                var selectionCode = _code.Encode(j);
                var queryMask = selectionCode & _senderState!.RandomChoices;
                Debug.Assert(queryMask.Length == CodeLength);

                for (int i = 0; i < numberOfInvocations; ++i)
                {
                    var query = queryMask ^ qs.GetRow(i);

                    var option = MakeRandomOption(numberOfMessageBits, query, totalNumberOfInvocationsOffset + i);
                    Debug.Assert(option.Length == numberOfMessageBits);

                    randomOptions.SetMessage(i, j, option);
                }
            }

#if DEBUG
            DebugUtils.WriteLineSender("RandomOT", "Computing outputs took {0} ms.", stopwatch.ElapsedMilliseconds);
#endif

            return randomOptions;
        }

    }
}
