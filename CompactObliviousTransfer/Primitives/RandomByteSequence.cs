// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using CompactOT.DataStructures;

namespace CompactOT
{

    public class RandomByteSequence
    {

        private IEnumerator<byte> _randomnessEnumerator;

        private class RandomByteEnumerator : BaseEnumerator<byte>
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

            public override byte Current => _buffer[_index];

            public override bool MoveNext()
            {
                _index += 1;
                if (_index >= _buffer.Length)
                {
                    _randomNumberGenerator.GetBytes(_buffer);
                    _index = 0;
                }
                return true;
            }

            public override void Reset()
            {
                throw new NotSupportedException();
            }

        }

        public RandomByteSequence(IEnumerator<byte> randomnessEnumerator)
        {
            _randomnessEnumerator = randomnessEnumerator;
        }

        public RandomByteSequence(RandomNumberGenerator randomNumberGenerator)
            : this(new RandomByteEnumerator(randomNumberGenerator, bufferSize: 32)) { }

        public RandomByteSequence(IEnumerable<byte> randomnessEnumerable)
            : this(randomnessEnumerable.GetEnumerator()) { }

        public DataStructures.BitArray GetBits(int amount)
        {
            return DataStructures.BitArray.FromBytes(Enumerator, amount);
        }

        public IEnumerator<byte> Enumerator => _randomnessEnumerator;

    }

}
