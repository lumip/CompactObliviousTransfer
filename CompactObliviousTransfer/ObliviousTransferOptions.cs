using System;
using System.Collections.Generic;
// using System.Linq;

namespace CompactOT
{

    public class ObliviousTransferOptions<T>
    {
        private T[,,] _options;

        public ObliviousTransferOptions(int numberOfInvocations, int numberOfOptions, int numberOfMessageBytes)
        {
            _options = new T[numberOfInvocations, numberOfOptions, numberOfMessageBytes];
        }

        public static ObliviousTransferOptions<T> MakeNewLike<TIn>(ObliviousTransferOptions<TIn> other)
        {
            return new ObliviousTransferOptions<T>(other.NumberOfInvocations, other.NumberOfOptions, other.NumberOfMessageBytes);
        }

        public int NumberOfInvocations => _options.GetLength(0);
        public int NumberOfOptions => _options.GetLength(1);
        public int NumberOfMessageBytes => _options.GetLength(2);

        public IEnumerable<T> GetMessageOption(int invocationIndex, int optionIndex)
        {
            for (int i = 0; i < NumberOfMessageBytes; ++i)
            {
                yield return _options[invocationIndex, optionIndex, i];
            }
        }

        public void SetMessageOption(int invocationIndex, int optionIndex, IEnumerable<T> messageOption)
        {
            IEnumerator<T> e = messageOption.GetEnumerator();

            for (int i = 0; i < NumberOfMessageBytes; ++i)
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
    }

}