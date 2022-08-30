using System;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace CompactOT
{
    /// <summary>
    /// A message channel based on a <see cref="System.Net.Sockets.NetworkStream" />.
    /// </summary>
    /// <remarks>
    /// For implementers: Note that this implementation relies on the stream to block
    /// upon reading until data is available. It can therefore not be used as is for
    /// generic <see cref="System.IO.Stream" />s, which may not behave in this way.
    /// </remarks>
    public class NetworkStreamMessageChannel : IMessageChannel
    {

        NetworkStream _stream;

        public NetworkStreamMessageChannel(NetworkStream stream)
        {
            if (!stream.CanRead || !stream.CanWrite)
                throw new ArgumentException("Stream must be read- and writable.", nameof(stream));

            _stream = stream;
        }

        public async Task<byte[]> ReadMessageAsync()
        {
            byte[] messageLengthBuffer = new byte[4];
            
            await _stream.ReadAsync(messageLengthBuffer, 0, messageLengthBuffer.Length);
            int messageLength = BitConverter.ToInt32(messageLengthBuffer, 0);

            byte[] messageBuffer = new byte[messageLength];
            await _stream.ReadAsync(messageBuffer, 0, messageLength);
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
