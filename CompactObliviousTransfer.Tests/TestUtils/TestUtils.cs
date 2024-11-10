// SPDX-FileCopyrightText: 2024 Lukas Prediger <lumip@lumip.de>
// SPDX-License-Identifier: GPL-3.0-or-later

using System.Linq;
using System.Threading.Tasks;

using CompactOT.DataStructures;
using Moq;

namespace CompactOT
{
    public static class TestUtils
    {
        /// <summary>
        /// Waits until all tasks are completed or at least one failed.
        ///
        /// Unlike Task.WhenAll, this will immediately complete if any
        /// task failed and throw the exception thrown within that task.
        /// </summary>
        /// <param name="tasks"></param>
        public static async Task WhenAllOrFail(params Task[] tasks)
        {
            var taskSet = new System.Collections.Generic.HashSet<Task>(tasks);
            while (taskSet.Count > 0)
            {
                var task = await Task.WhenAny(taskSet);
                if (task.Status == TaskStatus.Faulted)
                    throw task.Exception!;
                taskSet.Remove(task);
            }
        }

        public static readonly BitArray[] TestCorrelations = {
            BitArray.FromBinaryString("000111"),
            BitArray.FromBinaryString("111000"),
            BitArray.FromBinaryString("100001"),
            BitArray.FromBinaryString("010010"),
            BitArray.FromBinaryString("101101"),
        };

        public static readonly string[] TestOptions = {
            "Alicia", "Briann", "Charly", "Dennis", "Elenor", "Frieda"
        };

        public static IObliviousTransferChannel GetBaseTransferChannel(IMessageChannel messageChannel)
        {
            var otMock = new Mock<InsecureObliviousTransferChannel>(messageChannel) { CallBase = true };
            otMock.Setup(ot => ot.SecurityLevel).Returns(10000000);
            return otMock.Object;
        }

    }
}
