// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace CompactOT
{
    /// <summary>
    /// A message channel based on a <see cref="System.Net.Sockets.NetworkStream" />.
    /// </summary>
    public class NetworkStreamMessageChannel : IMessageChannel
    {

        NetworkStream _stream;

        public NetworkStreamMessageChannel(NetworkStream stream)
        {
            if (!stream.CanRead || !stream.CanWrite)
                throw new ArgumentException("Stream must be read- and writable.", nameof(stream));

            _stream = stream;
        }

        private async Task ReadAllAsync(byte[] buffer)
        {
            int bytesToRead = buffer.Length;
            int bytesRead = 0;

            while (bytesToRead > 0)
            {
                int bytesJustRead = await _stream.ReadAsync(buffer, bytesRead, bytesToRead);
                bytesRead += bytesJustRead;
                bytesToRead = buffer.Length - bytesRead;
            }
        }

        public async Task<byte[]> ReadMessageAsync()
        {
            byte[] messageLengthBuffer = new byte[4];
            
            await ReadAllAsync(messageLengthBuffer);
            int messageLength = BitConverter.ToInt32(messageLengthBuffer, 0);

            if (messageLength < 0)
                throw new ProtocolException("Received a message with negative length");

            byte[] messageBuffer = new byte[messageLength];

            if (messageLength > 0)
                await ReadAllAsync(messageBuffer);

            return messageBuffer;
        }

        public async Task WriteMessageAsync(byte[] message)
        {
            byte[] messageLengthBuffer = BitConverter.GetBytes(message.Length);
            await _stream.WriteAsync(messageLengthBuffer, 0, messageLengthBuffer.Length);
            await _stream.WriteAsync(message, 0, message.Length);
        }

    }
}
