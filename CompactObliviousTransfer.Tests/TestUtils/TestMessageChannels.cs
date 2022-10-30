using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace CompactOT
{
    /// <summary>
    /// Provides message channels for testing, backed by local queues of byte arrays.
    /// </summary>
    public class TestMessageChannels
    {

        public class Channel : IMessageChannel
        {

            ConcurrentQueue<byte[]> _inQueue;
            ConcurrentQueue<byte[]> _outQueue;

            AutoResetEvent _inEvent;
            AutoResetEvent _outEvent;

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

                        byte[] value;
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

        ConcurrentQueue<byte[]> _firstToSecond;
        ConcurrentQueue<byte[]> _secondToFirst;

        AutoResetEvent _firstToSecondEvent;
        AutoResetEvent _secondToFirstEvent;

        

        public TestMessageChannels()
        {
            _firstToSecond = new ConcurrentQueue<byte[]>();
            _firstToSecondEvent = new AutoResetEvent(false);
            _secondToFirst = new ConcurrentQueue<byte[]>();
            _secondToFirstEvent = new AutoResetEvent(false);
        }

        public IMessageChannel FirstPartyChannel => new Channel(_secondToFirst, _secondToFirstEvent, _firstToSecond, _firstToSecondEvent);
        public IMessageChannel SecondPartyChannel => new Channel(_firstToSecond, _firstToSecondEvent, _secondToFirst, _secondToFirstEvent);
    }
}
