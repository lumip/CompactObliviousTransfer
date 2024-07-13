// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CompactOT
{
    /// <summary>
    /// Provides message channels for testing, backed by local queues of byte arrays.
    /// </summary>
    public class TestMessageChannels : IDisposable
    {

        public class Channel : IMessageChannel
        {

            private readonly ConcurrentQueue<byte[]> _inQueue;
            private readonly ConcurrentQueue<byte[]> _outQueue;

            private readonly AutoResetEvent _inEvent;
            private readonly AutoResetEvent _outEvent;

            public Channel(ConcurrentQueue<byte[]> inQueue, AutoResetEvent inEvent, ConcurrentQueue<byte[]> outQueue, AutoResetEvent outEvent)
            {
                _inQueue = inQueue;
                _inEvent = inEvent;
                _outQueue = outQueue;
                _outEvent = outEvent;
            }

            public async Task<byte[]> ReadMessageAsync()
            {
                return await Task.Run(() =>
                {
                    while (true)
                    {
                        _inEvent.WaitOne();

                        byte[]? value;
                        if (_inQueue.TryDequeue(out value))
                            return value;
                    }
                });
            }

            public async Task WriteMessageAsync(byte[] message)
            {
                await Task.Run(() =>
                {
                    _outQueue.Enqueue(message);
                    _outEvent.Set();
                });
            }
        }

        private readonly ConcurrentQueue<byte[]> _firstToSecond;
        private readonly ConcurrentQueue<byte[]> _secondToFirst;

        private readonly AutoResetEvent _firstToSecondEvent;
        private readonly AutoResetEvent _secondToFirstEvent;

        private bool _disposed;



        public TestMessageChannels()
        {
            _firstToSecond = new ConcurrentQueue<byte[]>();
            _firstToSecondEvent = new AutoResetEvent(false);
            _secondToFirst = new ConcurrentQueue<byte[]>();
            _secondToFirstEvent = new AutoResetEvent(false);
            _disposed = false;
        }

        public IMessageChannel FirstPartyChannel => new Channel(_secondToFirst, _secondToFirstEvent, _firstToSecond, _firstToSecondEvent);
        public IMessageChannel SecondPartyChannel => new Channel(_firstToSecond, _firstToSecondEvent, _secondToFirst, _secondToFirstEvent);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _firstToSecondEvent.Dispose();
                _secondToFirstEvent.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
