// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Moq;

using Xunit;

namespace CompactOT.DataStructures
{
    public class NetworkStreamMessageChannelTests
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
        public void TestReadAndWriteAsync()
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

                TestUtils.WaitAllOrFail(listenerTask, clientTask);
            }
            finally
            {
                tcpListener.Stop();
            }

        }

        [Fact]
        public async Task TestUnreadableStream()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                tcpListener.Start();
                var endpoint = tcpListener.LocalEndpoint as IPEndPoint;
                Assert.NotNull(endpoint);

                Task listenerTask = RunListener(tcpListener, new (byte[], byte[])[] {});
                
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(endpoint!);
                    using (var tcpStream = tcpClient.GetStream())
                    {
                        using (var stream = new NetworkStream(tcpStream.Socket, System.IO.FileAccess.Write))
                        {
                            Assert.Throws<ArgumentException>(() => new NetworkStreamMessageChannel(stream));
                        }
                    }
                }

                await listenerTask;
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        [Fact]
        public async Task TestUnwritableStream()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                tcpListener.Start();
                var endpoint = tcpListener.LocalEndpoint as IPEndPoint;
                Assert.NotNull(endpoint);

                Task listenerTask = RunListener(tcpListener, new (byte[], byte[])[] {});
                
                using (var tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(endpoint!);
                    using (var tcpStream = tcpClient.GetStream())
                    {
                        using (var stream = new NetworkStream(tcpStream.Socket, System.IO.FileAccess.Read))
                        {
                            Assert.Throws<ArgumentException>(() => new NetworkStreamMessageChannel(stream));
                        }
                    }
                }

                await listenerTask;
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        [Fact]
        public void TestInvalidLengthReceived()
        {
            var tcpListener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                tcpListener.Start();
                var endpoint = tcpListener.LocalEndpoint as IPEndPoint;
                Assert.NotNull(endpoint);

                Task listenerTask = RunListenerInvalidLength(tcpListener);
                Task clientTask = RunClientInvalidLength(endpoint!);

                TestUtils.WaitAllOrFail(listenerTask, clientTask);
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
                    var channel = new NetworkStreamMessageChannel(tcpStream);
                    await Assert.ThrowsAsync<ProtocolException>(
                        channel.ReadMessageAsync
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
