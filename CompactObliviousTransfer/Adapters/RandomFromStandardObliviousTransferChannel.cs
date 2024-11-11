// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace CompactOT.Adapters
{

    public class RandomFromStandardObliviousTransferChannel : IRandomObliviousTransferChannel
    {
        private IObliviousTransferChannel _otChannel;
        private ThreadsafeRandomNumberGenerator _randomNumberGenerator;

        public RandomFromStandardObliviousTransferChannel(
            IObliviousTransferChannel obliviousTransferChannel, RandomNumberGenerator randomNumberGenerator
        )
        {
            _otChannel = obliviousTransferChannel;
            _randomNumberGenerator = new ThreadsafeRandomNumberGenerator(randomNumberGenerator);
        }

        public IMessageChannel Channel => _otChannel.Channel;

        public int SecurityLevel => _otChannel.SecurityLevel;

        public Task<ObliviousTransferResult> ReceiveAsync(
            int[] selectionIndices,
            int numberOfOptions,
            int numberOfMessageBits,
            CancellationToken cancellationToken
        )
        {
            return _otChannel.ReceiveAsync(selectionIndices, numberOfOptions, numberOfMessageBits, cancellationToken);
        }

        public Task<ObliviousTransferOptions> SendAsync(
            int numberOfInvocations,
            int numberOfOptions,
            int numberOfMessageBits,
            CancellationToken cancellationToken
        )
        {
            var options = new ObliviousTransferOptions(
                numberOfInvocations, numberOfOptions, numberOfMessageBits
            );
            for (int i = 0; i < numberOfInvocations; ++i)
            {
                for (int j = 0; j < numberOfOptions; ++j)
                {
                    var option = _randomNumberGenerator.GetBits(numberOfMessageBits);
                    options.SetMessage(i, j, option);
                }
            }
            return _otChannel.SendAsync(options, cancellationToken).ContinueWith(t => options);
        }

        public double EstimateCost(ObliviousTransferUsageProjection usageProjection)
        {
            return _otChannel.EstimateCost(usageProjection);
        }

    }
}
