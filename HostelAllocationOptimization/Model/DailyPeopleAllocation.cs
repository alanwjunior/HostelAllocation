using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HostelAllocationOptimization.Model
{
    public class DailyPeopleAllocation
    {
        public List<Tuple<int, int>> RoomPeopleAllocation { get; set; }

        public DailyPeopleAllocation()
        {
            RoomPeopleAllocation = new List<Tuple<int, int>>();
        }
    }
}
