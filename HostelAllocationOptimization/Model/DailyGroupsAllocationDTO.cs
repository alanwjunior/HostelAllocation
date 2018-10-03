using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HostelAllocationOptimization.Model
{
    public class DailyGroupsAllocationDTO
    {
        public double FuncObjValue { get; set; }
        public double[,] DailyGroupAllocation { get; set; } // Group vs Room
        //public int[,] DailyGroupReallocation { get; set; } // Group vs Day

        public DailyGroupsAllocationDTO()
        {

        }

        public DailyGroupsAllocationDTO(double[,] dailyGroupAllocation, double funcObjValue)
        {
            DailyGroupAllocation = dailyGroupAllocation;
            FuncObjValue = funcObjValue;
        }
    }
}
