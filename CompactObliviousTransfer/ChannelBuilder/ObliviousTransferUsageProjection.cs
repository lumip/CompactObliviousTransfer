// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;

namespace CompactOT
{
    /// <summary>
    /// A projection/forecast of the oblivious transfer (OT) usage for a channel
    /// in terms of the number of OT invocations, the number of message
    /// options/choices in each OT invocation and the bit-length of messages.
    /// 
    /// Invocations refer to single instance of the oblivious transfer protocol, i.e.,
    /// an exchange resulting in a single message retrieved by the client. Invocations
    /// can occur in batches, that is, several messages can be retrieved by the client
    /// simultaneously in a single round-trip of the protocol implement (i.e., a
    /// call to the <see cref="IObliviousTransfer"/> interface). 
    /// 
    /// Primarily used in <see cref="ObliviousTransferChannelBuilder"/> to determine
    /// an optimal (respective communication cost) channel implementation for the projected
    /// usage.
    /// </summary>
    public class ObliviousTransferUsageProjection
    {
        private int? _maxNumberOfOptions;

        /// <summary>
        /// True if a <see cref="MaxNumberOfOptions"/> has been set. 
        /// </summary>
        public bool HasMaxNumberOfOptions => _maxNumberOfOptions.HasValue;

        /// <summary>
        /// The maximum number of message options that is estimated to occur in any
        /// of the oblivious transfer invocations.
        /// 
        /// While some of the invocations may involve fewer options, none is estimated
        /// to exceed this number.
        /// </summary>
        public int MaxNumberOfOptions
        {
            get
            {
                if (_maxNumberOfOptions.HasValue)
                {
                    return _maxNumberOfOptions.Value;
                }
                throw new InvalidOperationException(
                    "The maximum number of options is unspecified."
                );
            }
            set
            {
                if (_avgNumberOfOptions.HasValue && value < _avgNumberOfOptions.Value)
                {
                    throw new ArgumentException(
                        $"Cannot specify a maximum number of options {value} less than the average number of options {_avgNumberOfOptions.Value}"
                    );
                }
                if (value < 2)
                {
                    throw new ArgumentOutOfRangeException(
                        $"The maximum number of options must not be less than 2, was {value}."
                    );
                }
                _maxNumberOfOptions = value;
            }
        }

        private int? _avgNumberOfOptions;

        /// <summary>
        /// The estimated average number of message options over all
        /// oblivious transfer invocations.
        /// 
        /// If not set explicitly it is equal to <see cref="MaxNumberOfOptions"/>,
        /// if that is set; otherwise 2.
        /// </summary>
        public int AverageNumberOfOptions
        {
            get
            {
                if (_avgNumberOfOptions.HasValue)
                {
                    return _avgNumberOfOptions.Value;
                }

                if (_maxNumberOfOptions.HasValue)
                {
                    return _maxNumberOfOptions.Value;
                }

                return 2;
            }
            set
            {
                if (_maxNumberOfOptions.HasValue && value > _maxNumberOfOptions.Value)
                {
                    throw new ArgumentException(
                        $"Cannot specify an average number of options {value} larger than the maximum number of options {_maxNumberOfOptions.Value}"
                    );
                }
                if (value < 2)
                {
                    throw new ArgumentOutOfRangeException(
                        $"The average number of options must not be less than 2, was {value}."
                    );
                }
                _avgNumberOfOptions = value;
            }
        }

        private int? _maxNumberOfInvocations;

        public bool HasMaxNumberOfInvocations => (_maxNumberOfInvocations.HasValue || _avgInvocationsPerBatch.HasValue);

        /// <summary>
        /// The maximum number of invocations, i.e., oblivious transfer instances,
        /// that is estimated to occur.
        /// 
        /// If both, <see cref="MaxNumberOfBatches"/> and  <see cref="AverageInvocationsPerBatch"/>,
        /// are set but <see cref="MaxNumberOfInvocations"/> is not, it is derived as the product
        /// of the former.
        /// </summary>
        public int MaxNumberOfInvocations
        {
            get
            {
                if (_maxNumberOfInvocations.HasValue)
                {
                    return _maxNumberOfInvocations.Value;
                }

                if (_avgInvocationsPerBatch.HasValue)
                {
                    if (_maxNumberOfBatches.HasValue)
                    {
                        return _avgInvocationsPerBatch.Value * _maxNumberOfBatches.Value;
                    }
                    return _avgInvocationsPerBatch.Value;
                }
                throw new InvalidOperationException(
                    "The maximum number of invocations is unspecified."
                );
            }
            set
            {
                if (_avgInvocationsPerBatch.HasValue && _maxNumberOfBatches.HasValue)
                {
                    throw new InvalidOperationException(
                        "Cannot specify maximum number of invocations if maximum number of batches and average invocations per batch are already specified."
                    );
                }
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"The maximum number of invocations must not be less than 1, was {value}."
                    );
                }
                _maxNumberOfInvocations = value;
            }
        }

        private int? _maxNumberOfBatches;

        public bool HasMaxNumberOfBatches => _maxNumberOfBatches.HasValue || _maxNumberOfInvocations.HasValue;

        /// <summary>
        /// The estimated maximum number of batches, i.e., protocol round-trips.
        /// 
        /// If both, <see cref="MaxNumberOfInvocations"/> and  <see cref="AverageInvocationsPerBatch"/>,
        /// are set but <see cref="MaxNumberOfBatches"/> is not, it is derived as the quotient
        /// of the former.
        /// </summary>
        public int MaxNumberOfBatches
        {
            get
            {
                if (_maxNumberOfBatches.HasValue)
                {
                    return _maxNumberOfBatches.Value;
                }

                if (_maxNumberOfInvocations.HasValue)
                {
                    if (_avgInvocationsPerBatch.HasValue)
                    {
                        return MathUtil.DivideAndCeiling(_maxNumberOfInvocations.Value, _avgInvocationsPerBatch.Value);
                    }
                    return _maxNumberOfInvocations.Value;
                }
                throw new InvalidOperationException(
                    "The maximum number of batches is unspecified."
                );
            }
            set
            {
                if (_maxNumberOfInvocations.HasValue && _avgInvocationsPerBatch.HasValue)
                {
                    throw new InvalidOperationException(
                        "Cannot specify maximum number of batches if maximum number of invocations and average invocations per batch are already specified."
                    );
                }
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"The maximum number of batches must not be less than 1, was {value}."
                    );
                }
                _maxNumberOfBatches = value;
            }
        }

        private int? _avgInvocationsPerBatch;

        /// <summary>
        /// The estimated average number of invocations over all batches, i.e., protocol round-trips.
        /// 
        /// If both, <see cref="MaxNumberOfInvocations"/> and  <see cref="MaxNumberOfBatches"/>,
        /// are set but <see cref="AverageInvocationsPerBatch"/> is not, it is derived as the quotient
        /// of the former. If either of the former is not set, a worst-case estimate of 1 is returned.
        /// </summary>
        public int AverageInvocationsPerBatch
        {
            get
            {
                if (_avgInvocationsPerBatch.HasValue)
                {
                    return _avgInvocationsPerBatch.Value;
                }

                if (_maxNumberOfInvocations.HasValue && _maxNumberOfBatches.HasValue)
                {
                    return MathUtil.DivideAndCeiling(_maxNumberOfInvocations.Value, _maxNumberOfBatches.Value);
                }
                return 1;
            }
            set
            {
                if (_maxNumberOfInvocations.HasValue && _maxNumberOfBatches.HasValue)
                {
                    throw new InvalidOperationException(
                        "Cannot specify average invocations per batch if maximum number of invocations and maximum number of batches are already specified."
                    );
                }
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"The average number of invocations per batch must not be less than 1, was {value}."
                    );
                }
                _avgInvocationsPerBatch = value;
            }
        }

        private int _avgMessageBits;

        /// <summary>
        /// The estimated average number of bits in a message over all invocations.
        /// </summary>
        public int AverageMessageBits
        {
            get => _avgMessageBits;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"The average number of bits in a message must not be less than 1, was {value}."
                    );
                }
                _avgMessageBits = value;
            }
        }

        /// <summary>
        /// Instantiates an unconfigured <see cref="ObliviousTransferUsageProjection"/> object
        /// with all properties unspecified, except <see cref="AverageMessageBits"/>, which defaults
        /// to 1.
        /// </summary>
        public ObliviousTransferUsageProjection()
        {
            _maxNumberOfOptions = null;
            _avgNumberOfOptions = null;
            _maxNumberOfInvocations = null;
            _maxNumberOfBatches = null;
            _avgInvocationsPerBatch = null;
            AverageMessageBits = 1;
        }

        /// <summary>
        /// Instantiates a <see cref="ObliviousTransferUsageProjection" /> object that is an
        /// exact copy of the provided one.
        /// </summary>
        /// <param name="toClone">The usage projection of which a copy will be made.</param>
        public ObliviousTransferUsageProjection(ObliviousTransferUsageProjection toClone)
        {
            _maxNumberOfOptions = toClone._maxNumberOfOptions;
            _avgNumberOfOptions = toClone._avgNumberOfOptions;
            _maxNumberOfInvocations = toClone._maxNumberOfInvocations;
            _maxNumberOfBatches = toClone._maxNumberOfBatches;
            _avgInvocationsPerBatch = toClone._avgInvocationsPerBatch;
            AverageMessageBits = toClone.AverageMessageBits;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            var other = obj as ObliviousTransferUsageProjection;
            if (other == null)
                return false;

            return _maxNumberOfOptions == other._maxNumberOfOptions &&
                _avgNumberOfOptions == other._avgNumberOfOptions &&
                _maxNumberOfInvocations == other._maxNumberOfInvocations &&
                _maxNumberOfBatches == other._maxNumberOfBatches &&
                _avgInvocationsPerBatch == other._avgInvocationsPerBatch &&
                _avgMessageBits == other._avgMessageBits;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return
                586541 * (_maxNumberOfOptions ?? 0) +
                587813 * (_maxNumberOfInvocations ?? 0) +
                960863 * (_maxNumberOfBatches ?? 0) +
                486179 * (_avgNumberOfOptions ?? 0) +
                944233 * (_avgInvocationsPerBatch ?? 0) +
                666871 * _avgMessageBits;
        }

    }

}
