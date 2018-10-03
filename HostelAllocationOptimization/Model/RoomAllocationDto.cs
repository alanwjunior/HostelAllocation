using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HostelAllocationOptimization.Model
{
    public class RoomAllocationDto
    {
        public Dictionary<int, string> RoomAllocation { get; set; }

        public RoomAllocationDto()
        {
            RoomAllocation = new Dictionary<int, string>();
        }
    }
}
