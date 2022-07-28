using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Diagnostics;
// using System.Linq;

using CompactOT.DataStructures;
namespace CompactOT
{

    public class ObliviousTransferOptions
    {
        private BitArray _values;

        public int NumberOfInvocations { get; }
        public int NumberOfOptions { get; }
        public int NumberOfMessageBits { get; }

        public ObliviousTransferOptions(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            NumberOfInvocations = numberOfInvocations;
            NumberOfOptions = numberOfOptions;
            NumberOfMessageBits = numberOfMessageBits;

            _values = new BitArray(NumberOfInvocations * NumberOfOptions * NumberOfMessageBits);
        }

        private ObliviousTransferOptions(BitArray values, int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            NumberOfInvocations = numberOfInvocations;
            NumberOfOptions = numberOfOptions;
            NumberOfMessageBits = numberOfMessageBits;

            int expectedSize = NumberOfInvocations * NumberOfOptions * NumberOfMessageBits;
            if (values.Length != expectedSize)
                throw new ArgumentException($"Value buffer does not have the correct size: {values.Length}; expected {expectedSize}", nameof(values));

            _values = values;
        }

        public static ObliviousTransferOptions FromBitArray(BitSequence values, int numberOfInvocations, int numberOfOptions, int numberOfMessageBits)
        {
            return new ObliviousTransferOptions(new BitArray(values), numberOfInvocations, numberOfOptions, numberOfMessageBits);
        }

        public static ObliviousTransferOptions CreateLike(ObliviousTransferOptions other)
        {
            return new ObliviousTransferOptions(other.NumberOfInvocations, other.NumberOfOptions, other.NumberOfMessageBits);
        }

        public static ObliviousTransferOptions CreateRandom(int numberOfInvocations, int numberOfOptions, int numberOfMessageBits, RandomNumberGenerator randomNumberGenerator)
        {
            int numberOfBits = numberOfInvocations * numberOfOptions * numberOfMessageBits;
            BitArray randomBits = randomNumberGenerator.GetBits(numberOfBits);
            return new ObliviousTransferOptions(randomBits, numberOfInvocations, numberOfOptions, numberOfMessageBits);
        }

        public static ObliviousTransferOptions FromCorrelatedTransfer(BitMatrix firstOptions, ObliviousTransferOptions correlations)
        {
            if (firstOptions.Rows != correlations.NumberOfInvocations)
            {
                throw new ArgumentException(
                    $"Number of invocations in both arguments must be identical, but was {firstOptions.Rows} and {correlations.NumberOfInvocations}"
                );
            }
            if (firstOptions.Cols != correlations.NumberOfMessageBits)
            {
                throw new ArgumentException(
                    $"Number of message bits in both arguments must be identical, but was {firstOptions.Cols} and {correlations.NumberOfMessageBits}"
                );
            }

            var correlatedOptions = new ObliviousTransferOptions(
                correlations.NumberOfInvocations, correlations.NumberOfOptions + 1, correlations.NumberOfMessageBits
            );
            for (int i = 0; i < correlatedOptions.NumberOfInvocations; ++i)
            {
                var firstOption = firstOptions.GetRow(i);
                correlatedOptions.SetMessage(i, 0, firstOption);
                for (int j = 0; j < correlations.NumberOfOptions; ++j)
                {
                    correlatedOptions.SetMessage(i, j + 1, firstOption ^ correlations.GetMessage(i, j));
                }
            }
            return correlatedOptions;
        }

        private int GetMessageOffset(int invocation, int option)
        {
            if (invocation < 0 || invocation >= NumberOfInvocations)
                throw new ArgumentOutOfRangeException("Invocation index out of range!", nameof(invocation));
            if (option < 0 || option >= NumberOfOptions)
                throw new ArgumentOutOfRangeException("Option index out of range!", nameof(option));
            return (invocation * NumberOfOptions + option) * NumberOfMessageBits;
        }

        public BitSequence GetMessage(int invocation, int option)
        {
            int offset = GetMessageOffset(invocation, option);
            int end = offset + NumberOfMessageBits;
            Debug.Assert(end <= _values.Length);
            return new BitArraySlice(_values, offset, end);
        }

        public void SetMessage(int invocation, int option, BitSequence message)
        {
            if (message.Length != NumberOfMessageBits)
                throw new ArgumentException("Length of given message must match oblivious transfer message length.", nameof(message));

            int offset = GetMessageOffset(invocation, option);
            int end = offset + NumberOfMessageBits;
            Debug.Assert(end <= _values.Length);

            // todo: ideally not work in Bit level here
            foreach ((int i, Bit b) in message.Enumerate())
            {
                _values[offset + i] = b;
                Debug.Assert(offset + i < end);
            }
        }

        public BitSequence GetInvocation(int invocation)
        {
            int offset = GetMessageOffset(invocation, 0);
            int end = offset + NumberOfOptions * NumberOfMessageBits;
            Debug.Assert(end <= _values.Length);

            return new BitArraySlice(_values, offset, end);
        }

        /// <summary>
        /// Sets all messages for an invocation at once.
        /// </summary>
        /// <param name="message">Sequence of bits for all messages, concatenated starting with the first message.</param>
        public void SetInvocation(int invocation, BitSequence messages)
        {
            if (messages.Length != NumberOfOptions * NumberOfMessageBits)
                throw new ArgumentException("Length of given messages must match oblivious transfer message length times the number of options.", nameof(messages));

            int offset = GetMessageOffset(invocation, 0);
            int end = offset + NumberOfOptions * NumberOfMessageBits;
            Debug.Assert(end <= _values.Length);

            // todo: ideally not work in Bit level here
            foreach ((int i, Bit b) in messages.Enumerate())
            {
                _values[offset + i] = b;
                Debug.Assert(offset + i < end);
            }
        }

        public void SetInvocation(int invocation, BitSequence[] messages)
        {
            if (messages.Length != NumberOfOptions)
                throw new ArgumentException("Number of given messages must match oblivious transfer option count.", nameof(messages));

            foreach ((int i, var message) in messages.Enumerate())
            {
                SetMessage(invocation, i, message);
            }
        }

        public void SetInvocation(int invocation, byte[][] messages)
        {
            if (messages.Length != NumberOfOptions)
                throw new ArgumentException("Number of given messages must match oblivious transfer option count.", nameof(messages));

            foreach ((int i, var message) in messages.Enumerate())
            {
                SetMessage(invocation, i, new EnumeratedBitArrayView(message, message.Length * 8));
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            ObliviousTransferOptions? other = obj as ObliviousTransferOptions;
            if (other == null) return false;
            if (other.NumberOfInvocations != NumberOfInvocations ||
                other.NumberOfOptions != NumberOfOptions ||
                other.NumberOfMessageBits != NumberOfMessageBits)
            {
                return false;
            }

            return other._values.Equals(_values);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _values.GetHashCode();
        }
    }

}