// SPDX-FileCopyrightText: 2022 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Xunit;

namespace CompactOT
{
    public class ObliviousTransferUsageProjectionTests
    {
        [Fact]
        public void TestDefaults()
        {
            var projection = new ObliviousTransferUsageProjection();

            Assert.False(projection.HasMaxNumberOfBatches);
            Assert.False(projection.HasMaxNumberOfInvocations);
            Assert.False(projection.HasMaxNumberOfOptions);

            Assert.Throws<InvalidOperationException>(() => projection.MaxNumberOfBatches);
            Assert.Throws<InvalidOperationException>(() => projection.MaxNumberOfInvocations);
            Assert.Throws<InvalidOperationException>(() => projection.MaxNumberOfOptions);

            Assert.Equal(1, projection.AverageInvocationsPerBatch);
            Assert.Equal(128, projection.SecurityLevel);
            Assert.Equal(1, projection.AverageMessageBits);
            Assert.Equal(2, projection.AverageNumberOfOptions);
        }

        [Fact]
        public void TestSecurityLevel()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.SecurityLevel = 200;

            Assert.Throws<ArgumentOutOfRangeException>(() => projection.AverageMessageBits = -1);

            Assert.False(projection.HasMaxNumberOfBatches);
            Assert.False(projection.HasMaxNumberOfInvocations);
            Assert.False(projection.HasMaxNumberOfOptions);

            Assert.Equal(1, projection.AverageInvocationsPerBatch);
            Assert.Equal(200, projection.SecurityLevel);
            Assert.Equal(1, projection.AverageMessageBits);
            Assert.Equal(2, projection.AverageNumberOfOptions);
        }

        [Fact]
        public void TestAverageMessageBits()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.AverageMessageBits = 3532;

            Assert.Throws<ArgumentOutOfRangeException>(() => projection.AverageMessageBits = 0);

            Assert.False(projection.HasMaxNumberOfBatches);
            Assert.False(projection.HasMaxNumberOfInvocations);
            Assert.False(projection.HasMaxNumberOfOptions);

            Assert.Equal(1, projection.AverageInvocationsPerBatch);
            Assert.Equal(128, projection.SecurityLevel);
            Assert.Equal(3532, projection.AverageMessageBits);
            Assert.Equal(2, projection.AverageNumberOfOptions);
        }

        [Fact]
        public void TestAverageInvocationsPerBatch()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.AverageInvocationsPerBatch = 7;

            Assert.Throws<ArgumentOutOfRangeException>(() => projection.AverageInvocationsPerBatch = 0);

            Assert.False(projection.HasMaxNumberOfBatches);
            Assert.True(projection.HasMaxNumberOfInvocations);
            Assert.Equal(7, projection.MaxNumberOfInvocations);
            Assert.False(projection.HasMaxNumberOfOptions);

            Assert.Equal(7, projection.AverageInvocationsPerBatch);
            Assert.Equal(128, projection.SecurityLevel);
            Assert.Equal(1, projection.AverageMessageBits);
            Assert.Equal(2, projection.AverageNumberOfOptions);
        }

        [Fact]
        public void TestAverageInvocationsPerBatchDerivedFromOthers()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.MaxNumberOfInvocations = 10;
            projection.MaxNumberOfBatches = 4;

            Assert.Equal(10, projection.MaxNumberOfInvocations);
            Assert.Equal(4, projection.MaxNumberOfBatches);
            Assert.Equal(3, projection.AverageInvocationsPerBatch);

            Assert.Throws<InvalidOperationException>(() => projection.AverageInvocationsPerBatch = 8);
        }

        [Fact]
        public void TestMaxNumberOfBatches()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.MaxNumberOfBatches = 8;

            Assert.Throws<ArgumentOutOfRangeException>(() => projection.MaxNumberOfBatches = 0);

            Assert.True(projection.HasMaxNumberOfBatches);
            Assert.Equal(8, projection.MaxNumberOfBatches);
            Assert.False(projection.HasMaxNumberOfInvocations);
            Assert.False(projection.HasMaxNumberOfOptions);

            Assert.Equal(1, projection.AverageInvocationsPerBatch);
            Assert.Equal(128, projection.SecurityLevel);
            Assert.Equal(1, projection.AverageMessageBits);
            Assert.Equal(2, projection.AverageNumberOfOptions);
        }

        [Fact]
        public void TestMaxNumberOfBatchesDerivedFromOthers()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.MaxNumberOfInvocations = 11;
            projection.AverageInvocationsPerBatch = 3;

            Assert.Equal(11, projection.MaxNumberOfInvocations);
            Assert.Equal(3, projection.AverageInvocationsPerBatch);
            Assert.Equal(4, projection.MaxNumberOfBatches);

            Assert.Throws<InvalidOperationException>(() => projection.MaxNumberOfBatches = 8);
        }

        [Fact]
        public void TestMaxNumberOfInvocations()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.MaxNumberOfInvocations = 13;

            Assert.Throws<ArgumentOutOfRangeException>(() => projection.MaxNumberOfInvocations = 0);

            Assert.True(projection.HasMaxNumberOfBatches);
            Assert.Equal(13, projection.MaxNumberOfBatches);
            Assert.True(projection.HasMaxNumberOfInvocations);
            Assert.Equal(13, projection.MaxNumberOfInvocations);
            Assert.False(projection.HasMaxNumberOfOptions);

            Assert.Equal(1, projection.AverageInvocationsPerBatch);
            Assert.Equal(128, projection.SecurityLevel);
            Assert.Equal(1, projection.AverageMessageBits);
            Assert.Equal(2, projection.AverageNumberOfOptions);
        }

        [Fact]
        public void TestMaxNumberOfInvocationsDerivedFromOthers()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.MaxNumberOfBatches = 7;
            projection.AverageInvocationsPerBatch = 3;

            Assert.Equal(7, projection.MaxNumberOfBatches);
            Assert.Equal(3, projection.AverageInvocationsPerBatch);
            Assert.Equal(21, projection.MaxNumberOfInvocations);

            Assert.Throws<InvalidOperationException>(() => projection.MaxNumberOfInvocations = 8);
        }

        [Fact]
        public void TestMaxNumberOfOptions()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.MaxNumberOfOptions = 5;

            Assert.Throws<ArgumentOutOfRangeException>(() => projection.MaxNumberOfOptions = 1);

            Assert.False(projection.HasMaxNumberOfBatches);
            Assert.False(projection.HasMaxNumberOfInvocations);
            Assert.True(projection.HasMaxNumberOfOptions);
            Assert.Equal(5, projection.MaxNumberOfOptions);

            Assert.Equal(1, projection.AverageInvocationsPerBatch);
            Assert.Equal(128, projection.SecurityLevel);
            Assert.Equal(1, projection.AverageMessageBits);
            Assert.Equal(5, projection.AverageNumberOfOptions);
        }

        [Fact]
        public void TestAverageNumberOfOptions()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.AverageNumberOfOptions = 7;

            Assert.Throws<ArgumentOutOfRangeException>(() => projection.AverageNumberOfOptions = 1);

            Assert.False(projection.HasMaxNumberOfBatches);
            Assert.False(projection.HasMaxNumberOfInvocations);
            Assert.False(projection.HasMaxNumberOfOptions);

            Assert.Equal(1, projection.AverageInvocationsPerBatch);
            Assert.Equal(128, projection.SecurityLevel);
            Assert.Equal(1, projection.AverageMessageBits);
            Assert.Equal(7, projection.AverageNumberOfOptions);
        }

        [Fact]
        public void TestAverageAndMaxNumberOfOptions()
        {
            var projection = new ObliviousTransferUsageProjection();
            projection.MaxNumberOfOptions = 7;
            
            Assert.Throws<ArgumentException>(() => projection.AverageNumberOfOptions = 8);

            projection.AverageNumberOfOptions = 5;
            Assert.Throws<ArgumentException>(() => projection.MaxNumberOfOptions = 4);

            Assert.Equal(7, projection.MaxNumberOfOptions);
            Assert.Equal(5, projection.AverageNumberOfOptions);
        }

    }
}
