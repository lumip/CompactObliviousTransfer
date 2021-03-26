using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Linq;

namespace CompactOT
{

    public class RandomByteSequence : RandomNumberGenerator
    {

        private IEnumerator<byte> _randomnessEnumerator;

        public RandomByteSequence(IEnumerator<byte> randomnessEnumerator)
        {
            _randomnessEnumerator = randomnessEnumerator;
        }

        public RandomByteSequence(IEnumerable<byte> randomnessEnumerable)
            : this(randomnessEnumerable.GetEnumerator()) { }

        public override void GetBytes(byte[] data)
        {
            for (int i = 0; i < data.Length && Enumerator.MoveNext(); ++i)
            {
                data[i] = Enumerator.Current;
            }
        }

        public IEnumerator<byte> Enumerator => _randomnessEnumerator;

    }

}