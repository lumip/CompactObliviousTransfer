using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace CompactOT
{
    /// <summary>
    /// Provides message channels for testing, backed by local queues of byte arrays.
    /// </summary>
    public class TestMessageChannels
    {

        public class Channel : IMessageChannel
        {

            Queue<byte[]> _inQueue;
            Queue<byte[]> _outQueue;

            AutoResetEvent _inEvent;
            AutoResetEvent _outEvent;

            public Channel(Queue<byte[]> inQueue, AutoResetEvent inEvent, Queue<byte[]> outQueue, AutoResetEvent outEvent)
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
                        lock (_inQueue)
                        {
                            if (_inQueue.Count > 0)
                                return _inQueue.Dequeue();
                        }
                        _inEvent.WaitOne();
                    }
                });
            }

            public async Task WriteMessageAsync(byte[] message)
            {
                await Task.Run(() =>
                {
                    lock(_outQueue)
                    {
                        _outQueue.Enqueue(message);
                        _outEvent.Set();
                    }
                });
            }
        }

        Queue<byte[]> _firstToSecond;
        Queue<byte[]> _secondToFirst;

        AutoResetEvent _firstToSecondEvent;
        AutoResetEvent _secondToFirstEvent;

        

        public TestMessageChannels(int capacity = 1)
        {
            _firstToSecond = new Queue<byte[]>(capacity);
            _firstToSecondEvent = new AutoResetEvent(false);
            _secondToFirst = new Queue<byte[]>(capacity);
            _secondToFirstEvent = new AutoResetEvent(false);
        }

        public IMessageChannel FirstPartyChannel => new Channel(_secondToFirst, _secondToFirstEvent, _firstToSecond, _firstToSecondEvent);
        public IMessageChannel SecondPartyChannel => new Channel(_firstToSecond, _firstToSecondEvent, _secondToFirst, _secondToFirstEvent);
    }
}
