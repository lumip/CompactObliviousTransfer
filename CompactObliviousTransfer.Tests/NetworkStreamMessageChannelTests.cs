// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Moq;
using Xunit;

namespace CompactOT.DataStructures
{
    public class NetworkStreamMessageChannelTests
    {

        [Fact]
        public void TestConstructorUnwritableStream()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(s => s.CanWrite).Returns(false);
            streamMock.Setup(s => s.CanRead).Returns(true);

            Assert.Throws<ArgumentException>(
                () => new NetworkStreamMessageChannel(streamMock.Object)
            );
        }

        [Fact]
        public void TestConstructorUnreadableStream()
        {
            var streamMock = new Mock<Stream>();
            streamMock.Setup(s => s.CanWrite).Returns(true);
            streamMock.Setup(s => s.CanRead).Returns(false);

            Assert.Throws<ArgumentException>(
                () => new NetworkStreamMessageChannel(streamMock.Object)
            );
        }

        [Fact]
        public async void TestReadAsync()
        {
            byte[] buffer = new byte[] { 3, 0, 0, 0, 0xaa, 0xbb, 0xcc };
            byte[] expectedMessage = new byte[] { 0xaa, 0xbb, 0xcc };
            var stream = new MemoryStream(buffer);

            var channel = new NetworkStreamMessageChannel(stream);
            byte[] message = await channel.ReadMessageAsync();

            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        public async void TestReadAsyncEmptyMessage()
        {
            byte[] buffer = new byte[] { 0, 0, 0, 0 };
            byte[] expectedMessage = new byte[] { };
            var stream = new MemoryStream(buffer);

            var channel = new NetworkStreamMessageChannel(stream);
            byte[] message = await channel.ReadMessageAsync();

            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        public async void TestReadAsyncInvalidLength()
        {
            byte[] buffer = new byte[] { 3, 0, 0, 0xa0, 0xaa, 0xbb, 0xcc };
            var stream = new MemoryStream(buffer);

            IMessageChannel channel = new NetworkStreamMessageChannel(stream);

            await Assert.ThrowsAsync<ProtocolException>(
                async () => await channel.ReadMessageAsync()
            );
        }

        [Fact]
        public async void TestReadAsyncHeaderInterrupted()
        {
            var messageParts = new Queue<byte[]>(new byte[][] {
                new byte[] { 3, 0, 0 },
                new byte[] { },
                new byte[] { 0 },
                new byte[] { 0xaa, 0xbb, 0xcc },
            });
            byte[] expectedMessage = new byte[] { 0xaa, 0xbb, 0xcc };

            var streamMock = new Mock<Stream>() { CallBase = true };
            streamMock.Setup(s => s.CanWrite).Returns(true);
            streamMock.Setup(s => s.CanRead).Returns(true);
            streamMock.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((byte[] b, int offset, int length) =>
                    {
                        byte[] nextPart = messageParts.Dequeue();
                        int actualLength = Math.Min(nextPart.Length, b.Length - offset);
                        Array.Copy(nextPart, 0, b, offset, actualLength);
                        return actualLength;
                    }
                );

            var channel = new NetworkStreamMessageChannel(streamMock.Object);

            var message = await channel.ReadMessageAsync();

            Assert.Equal(expectedMessage, message);
        }


        [Fact]
        public async void TestReadAsyncBodyInterrupted()
        {
            var messageParts = new Queue<byte[]>(new byte[][] {
                new byte[] { 3, 0, 0, 0 },
                new byte[] { },
                new byte[] { 0xaa, 0xbb },
                new byte[] { },
                new byte[] { 0xcc },
            });
            byte[] expectedMessage = new byte[] { 0xaa, 0xbb, 0xcc };

            var streamMock = new Mock<Stream>() { CallBase = true };
            streamMock.Setup(s => s.CanWrite).Returns(true);
            streamMock.Setup(s => s.CanRead).Returns(true);
            streamMock.Setup(s => s.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((byte[] b, int offset, int length) =>
                    {
                        byte[] nextPart = messageParts.Dequeue();
                        int actualLength = Math.Min(nextPart.Length, b.Length - offset);
                        Array.Copy(nextPart, 0, b, offset, actualLength);
                        return actualLength;
                    }
                );

            var channel = new NetworkStreamMessageChannel(streamMock.Object);

            var message = await channel.ReadMessageAsync();

            Assert.Equal(expectedMessage, message);
        }

        [Fact]
        public async void TestReadAsyncInterrupted()
        {
            var tokenSource = new CancellationTokenSource();
            var streamMock = new Mock<Stream>() { CallBase = true };
            streamMock.Setup(s => s.CanWrite).Returns(true);
            streamMock.Setup(s => s.CanRead).Returns(true);
            streamMock
                .Setup(s => s.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback(() => tokenSource.Cancel());

            var channel = new NetworkStreamMessageChannel(streamMock.Object);

            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await channel.ReadMessageAsync(tokenSource.Token)
            );

            streamMock
                .Verify(s => s.ReadAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.Is<CancellationToken>(ct => ct == tokenSource.Token))
                );
        }

        [Fact]
        public async void TestWriteAsync()
        {
            var stream = new MemoryStream();

            var channel = new NetworkStreamMessageChannel(stream);
            byte[] message = new byte[] { 0xaa, 0xbb, 0xcc };
            await channel.WriteMessageAsync(message);

            stream.Position = 0;
            byte[] streamContents = new byte[7];
            byte[] expectedContents = new byte[] { 3, 0, 0, 0, 0xaa, 0xbb, 0xcc };

            stream.Read(streamContents, 0, streamContents.Length);

            Assert.Equal(expectedContents, streamContents);
        }

        [Fact]
        public async void TestWriteAsyncCancelled()
        {
            var tokenSource = new CancellationTokenSource();

            var streamMock = new Mock<Stream>() { CallBase = true };
            streamMock.Setup(s => s.CanWrite).Returns(true);
            streamMock.Setup(s => s.CanRead).Returns(true);
            streamMock
                .Setup(s => s.WriteAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback(() => tokenSource.Cancel());

            var channel = new NetworkStreamMessageChannel(streamMock.Object);

            byte[] message = new byte[] { 3, 4, 5 };

            await Assert.ThrowsAsync<TaskCanceledException>(
                async () => await channel.WriteMessageAsync(message, tokenSource.Token)
            );

            streamMock
                .Verify(s => s.WriteAsync(
                    It.IsAny<byte[]>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.Is<CancellationToken>(ct => ct == tokenSource.Token))
                );
        }
    }

    public class NetworkStreamMessageChannelNetworkTests
    {

        private static async Task RunListener(TcpListener listener, (byte[], byte[])[] queriesAndResponses)
        {
            using (var tcpClient = await listener.AcceptTcpClientAsync())
            {
                using (var tcpStream = tcpClient.GetStream())
                {
                    var channel = new NetworkStreamMessageChannel(tcpStream);
                    foreach ((byte[] query, byte[] expectedResponse) in queriesAndResponses)
                    {
                        await channel.WriteMessageAsync(query);
                        byte[] response = await channel.ReadMessageAsync();
                        Assert.Equal(expectedResponse, response);
                    }
                }
            }
        }

        private static async Task RunClient(IPEndPoint endpoint, (byte[], byte[])[] queriesAndResponses)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(endpoint);
                using (NetworkStream tcpStream = tcpClient.GetStream())
                {
                    var channel = new NetworkStreamMessageChannel(tcpStream);
                    foreach ((byte[] expectedQuery, byte[] response) in queriesAndResponses)
                    {
                        byte[] query = await channel.ReadMessageAsync();
                        Assert.Equal(expectedQuery, query);
                        await channel.WriteMessageAsync(response);
                    }
                }
            }
        }

        [Fact]
        public async void TestReadAndWriteAsync()
        {
            var queriesAndResponses = new (byte[], byte[])[] {
                (new byte[] { 0, 1, 2, 3 }, new byte[] { 9, 8 }),
                (new byte[] { }, new byte[] { 0xff, 0x11 }),
            };

            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                tcpListener.Start();
                var endpoint = tcpListener.LocalEndpoint as IPEndPoint;
                Assert.NotNull(endpoint);

                Task listenerTask = RunListener(tcpListener, queriesAndResponses);
                Task clientTask = RunClient(endpoint!, queriesAndResponses);

                await TestUtils.WhenAllOrFail(clientTask, listenerTask);
            }
            finally
            {
                tcpListener.Stop();
            }

        }

        [Fact]
        public async void TestInvalidLengthReceived()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                tcpListener.Start();
                var endpoint = tcpListener.LocalEndpoint as IPEndPoint;
                Assert.NotNull(endpoint);

                Task listenerTask = RunListenerInvalidLength(tcpListener);
                Task clientTask = RunClientInvalidLength(endpoint!);

                await TestUtils.WhenAllOrFail(listenerTask, clientTask);
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        private static async Task RunListenerInvalidLength(TcpListener listener)
        {
            using (var tcpClient = await listener.AcceptTcpClientAsync())
            {
                using (var tcpStream = tcpClient.GetStream())
                {
                    IMessageChannel channel = new NetworkStreamMessageChannel(tcpStream);
                    await Assert.ThrowsAsync<ProtocolException>(
                        async () => await channel.ReadMessageAsync()
                    );
                }
            }
        }

        private static async Task RunClientInvalidLength(IPEndPoint endpoint)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(endpoint);
                using (NetworkStream tcpStream = tcpClient.GetStream())
                {
                    int invalidLength = -3;
                    byte[] bytes = BitConverter.GetBytes(invalidLength);
                    await tcpStream.WriteAsync(bytes);
                }
            }
        }

        [Fact]
        public async void TestMessageBodyInterrupted()
        {
            byte[] expectedMessage = new byte[] { 0xaa, 0xbb, 1, 2, 3 };
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                tcpListener.Start();
                var endpoint = tcpListener.LocalEndpoint as IPEndPoint;
                Assert.NotNull(endpoint);

                Task listenerTask = RunListenerSendBodyInterrupted(tcpListener, expectedMessage);

                using (TcpClient tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(endpoint!);
                    using (NetworkStream tcpStream = tcpClient.GetStream())
                    {
                        var channel = new NetworkStreamMessageChannel(tcpStream);
                        var messsage = await channel.ReadMessageAsync();
                        Assert.Equal(expectedMessage, messsage);
                    }
                }

                await listenerTask;
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        private static async Task RunListenerSendBodyInterrupted(TcpListener listener, byte[] message)
        {
            const int initialSendLength = 2;

            using (var tcpClient = await listener.AcceptTcpClientAsync())
            {
                using (var tcpStream = tcpClient.GetStream())
                {
                    byte[] lengthBuffer = BitConverter.GetBytes(message.Length);
                    await tcpStream.WriteAsync(lengthBuffer);

                    await tcpStream.WriteAsync(message, 0, initialSendLength);
                    await Task.Delay(100);
                    await tcpStream.WriteAsync(message, initialSendLength, message.Length - initialSendLength);
                }
            }
        }


        [Fact]
        public async void TestMessageHeaderInterrupted()
        {
            byte[] expectedMessage = new byte[] { 0xaa, 0xbb, 1, 2, 3 };
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                tcpListener.Start();
                var endpoint = tcpListener.LocalEndpoint as IPEndPoint;
                Assert.NotNull(endpoint);

                Task listenerTask = RunListenerSendHeaderInterrupted(tcpListener, expectedMessage);

                using (TcpClient tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(endpoint!);
                    using (NetworkStream tcpStream = tcpClient.GetStream())
                    {
                        var channel = new NetworkStreamMessageChannel(tcpStream);
                        var messsage = await channel.ReadMessageAsync();
                        Assert.Equal(expectedMessage, messsage);
                    }
                }

                await listenerTask;
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        private static async Task RunListenerSendHeaderInterrupted(TcpListener listener, byte[] message)
        {
            const int initialSendLength = 2;

            using (var tcpClient = await listener.AcceptTcpClientAsync())
            {
                using (var tcpStream = tcpClient.GetStream())
                {
                    byte[] lengthBuffer = BitConverter.GetBytes(message.Length);
                    await tcpStream.WriteAsync(lengthBuffer, 0, initialSendLength);
                    await Task.Delay(100);
                    await tcpStream.WriteAsync(lengthBuffer, initialSendLength, lengthBuffer.Length - initialSendLength);

                    await tcpStream.WriteAsync(message);
                }
            }
        }

    }

}
