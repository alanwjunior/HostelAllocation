using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HostelAllocationOptimization
{
    public class HostelAllocationDTO
    {
        public int NumDays { get; set; }
        public int NumRooms { get; set; }
        public int[] GroupsSizes { get; set; }
        public int[] RoomCapacity { get; set; }
        public List<Tuple<int, int>> GroupsDemands { get; set; }
        public List<Tuple<int, int>> InitialAllocation { get; set; }

        public HostelAllocationDTO()
        {

        }
    }
}
