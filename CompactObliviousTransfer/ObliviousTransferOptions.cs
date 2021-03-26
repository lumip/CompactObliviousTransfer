using System;
using System.Collections.Generic;
using System.Security.Cryptography;
// using System.Linq;

namespace CompactOT
{

    public class ObliviousTransferOptions<T>
    {
        private T[,,] _options;

        public ObliviousTransferOptions(int numberOfInvocations, int numberOfOptions, int messageLength)
        {
            _options = new T[numberOfInvocations, numberOfOptions, messageLength];
        }

        public static ObliviousTransferOptions<T> MakeNewLike<TIn>(ObliviousTransferOptions<TIn> other)
        {
            return new ObliviousTransferOptions<T>(other.NumberOfInvocations, other.NumberOfOptions, other.MessageLength);
        }

        public int NumberOfInvocations => _options.GetLength(0);
        public int NumberOfOptions => _options.GetLength(1);
        public int MessageLength => _options.GetLength(2);

        public IEnumerable<T> GetMessageOption(int invocationIndex, int optionIndex)
        {
            for (int i = 0; i < MessageLength; ++i)
            {
                yield return _options[invocationIndex, optionIndex, i];
            }
        }

        public void SetMessageOption(int invocationIndex, int optionIndex, IEnumerable<T> messageOption)
        {
            IEnumerator<T> e = messageOption.GetEnumerator();

            for (int i = 0; i < MessageLength; ++i)
            {                
                if (!e.MoveNext()) throw new ArgumentException("Given message option is too short!");
                _options[invocationIndex, optionIndex, i] = e.Current;
            }
        }

        public void SetInvocationOptions(int invocationIndex, IEnumerable<IEnumerable<T>> invocationOptions)
        {
            var e = invocationOptions.GetEnumerator();
            for (int i = 0; i < NumberOfOptions; ++i)
            {
                if (!e.MoveNext()) throw new ArgumentException("Not enough option messages provided!");
                SetMessageOption(invocationIndex, i, e.Current);
            }
        }

        public static void FillWithRandom(ObliviousTransferOptions<byte> options, RandomNumberGenerator randomNumberGenerator)
        {
            var buffer = new byte[options.MessageLength];
            for (int j = 0; j < options.NumberOfInvocations; ++j)
            {
                for (int i = 0; i < options.NumberOfOptions; ++i)
                {
                    randomNumberGenerator.GetBytes(buffer);
                    options.SetMessageOption(j, i, buffer);
                }
            }
        }

        public T[] Buffer
        {
            get
            {
                T[] buffer = new T[NumberOfInvocations * NumberOfOptions * MessageLength];
                Array.Copy(_options, buffer, buffer.Length);
                return buffer;
            }
        }
    }

}