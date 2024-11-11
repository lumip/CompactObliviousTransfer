// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CompactOT
{
    /// <summary>
    /// Provides message channels for testing, backed by System.Threading.Channels.Channel.
    /// </summary>
    public class TestMessageChannels
    {

        public class MessageChannel : IMessageChannel
        {

            private readonly Channel<byte[]> _inChannel;
            private readonly Channel<byte[]> _outChannel;

            public MessageChannel(Channel<byte[]> inChannel, Channel<byte[]> outChannel)
            {
                _inChannel = inChannel;
                _outChannel = outChannel;
            }

            public async Task<byte[]> ReadMessageAsync(CancellationToken cancellationToken)
            {
                return await _inChannel.Reader.ReadAsync(cancellationToken);
            }

            public async Task WriteMessageAsync(byte[] message, CancellationToken cancellationToken)
            {
                await _outChannel.Writer.WriteAsync(message, cancellationToken);
            }
        }

        private readonly Channel<byte[]> _firstToSecondChannel;
        private readonly Channel<byte[]> _secondToFirstChannel;


        public TestMessageChannels()
        {
            _firstToSecondChannel = Channel.CreateUnbounded<byte[]>();
            _secondToFirstChannel = Channel.CreateUnbounded<byte[]>();
        }

        public IMessageChannel FirstPartyChannel => new MessageChannel(_secondToFirstChannel, _firstToSecondChannel);
        public IMessageChannel SecondPartyChannel => new MessageChannel(_firstToSecondChannel, _secondToFirstChannel);

    }
}
