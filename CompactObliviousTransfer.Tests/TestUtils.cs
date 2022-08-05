using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CompactOT.DataStructures;

namespace CompactOT
{
    public static class TestUtils
    {
        /// <summary>
        /// Waits until all tasks are completed or at least one failed.
        /// 
        /// Unlike Task.WaitAll, this will immediately unblock if any
        /// task failed and throw the exception thrown within that task.
        /// </summary>
        /// <param name="tasks"></param>
        public static void WaitAllOrFail(params Task[] tasks)
        {
            var taskList = new System.Collections.Generic.LinkedList<Task>(tasks);
            while (taskList.Count > 0)
            {
                int which = Task.WaitAny(taskList.ToArray());
                var task = taskList.ElementAt(which);
                if (task.Status == TaskStatus.Faulted)
                    throw task.Exception;
                taskList.Remove(taskList.ElementAt(which));
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
    }
}
