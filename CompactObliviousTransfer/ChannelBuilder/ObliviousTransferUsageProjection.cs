using System;

namespace CompactOT
{
    public class ObliviousTransferUsageProjection
    {
        private int? _maxNumberOfOptions;

        public bool HasMaxNumberOfOptions => _maxNumberOfOptions.HasValue;

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

        private int _securityLevel;
        public int SecurityLevel
        {
            get => _securityLevel;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"The security level must not be negative, was {value}."
                    );
                }
                _securityLevel = value;
            }
        }

        private int _avgMessageBits;
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

        public ObliviousTransferUsageProjection()
        {
            _maxNumberOfOptions = null;
            _avgNumberOfOptions = null;
            _maxNumberOfInvocations = null;
            _maxNumberOfBatches = null;
            _avgInvocationsPerBatch = null;
            SecurityLevel = 128;
            AverageMessageBits = 1;
        }

        public ObliviousTransferUsageProjection(ObliviousTransferUsageProjection toClone)
        {
            _maxNumberOfOptions = toClone._maxNumberOfOptions;
            _avgNumberOfOptions = toClone._avgNumberOfOptions;
            _maxNumberOfInvocations = toClone._maxNumberOfInvocations;
            _maxNumberOfBatches = toClone._maxNumberOfBatches;
            _avgInvocationsPerBatch = toClone._avgInvocationsPerBatch;
            SecurityLevel = toClone.SecurityLevel;
            AverageMessageBits = toClone.AverageMessageBits; 
        }
    }

}
