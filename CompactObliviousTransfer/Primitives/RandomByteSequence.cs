using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;

namespace CompactOT
{

    public class RandomByteSequence
    {

        private IEnumerator<byte> _randomnessEnumerator;

        private class RandomByteEnumerator : IEnumerator<byte>
        {

            byte[] _buffer;
            int _index;

            private RandomNumberGenerator _randomNumberGenerator;

            public RandomByteEnumerator(RandomNumberGenerator randomNumberGenerator, int bufferSize)
            {
                _randomNumberGenerator = randomNumberGenerator;
                _buffer = new byte[bufferSize];
                _index = bufferSize;
            }

            public byte Current => _buffer[_index];

            object IEnumerator.Current => ((IEnumerator<byte>)this).Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _index += 1;
                if (_index >= _buffer.Length)
                {
                    _randomNumberGenerator.GetBytes(_buffer);
                    _index = 0;
                }
                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }

        public RandomByteSequence(IEnumerator<byte> randomnessEnumerator)
        {
            _randomnessEnumerator = randomnessEnumerator;
        }

        public RandomByteSequence(RandomNumberGenerator randomNumberGenerator)
        {
            _randomnessEnumerator = new RandomByteEnumerator(randomNumberGenerator, bufferSize: 32);
        }

        public RandomByteSequence(IEnumerable<byte> randomnessEnumerable)
            : this(randomnessEnumerable.GetEnumerator()) { }

        public DataStructures.BitArray GetBits(int amount)
        {
            return DataStructures.BitArray.FromBytes(Enumerator, amount);
        }

        public IEnumerator<byte> Enumerator => _randomnessEnumerator;

    }

}