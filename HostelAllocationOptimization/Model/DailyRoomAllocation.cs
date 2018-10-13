using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HostelAllocationOptimization.Model
{
    public class DailyRoomAllocation
    {
        public List<Tuple<int, int>> RoomGroupsAllocated { get; set; }

        public Dictionary<int, string> RoomAllocation { get; set; }

        public double NumGroupsSplits { get; set; }

        public double NumGroupsChanges { get; set; }

        public int[] GroupsIn { get; set; }

        public int[] GroupsOut { get; set; }

        public double FuncObj { get; set; }

        public DailyRoomAllocation()
        {
            RoomGroupsAllocated = new List<Tuple<int, int>>();
            RoomAllocation = new Dictionary<int, string>();
        }

        public void FillRoomAllocation(int numRooms)
        {
            for (int room = 0; room < numRooms; room++)
            {
                var groups = RoomGroupsAllocated.Where(r => r.Item1 == room).Select(r => r.Item2).ToList();
                var groupsStr = groups.Count > 0 ? string.Join("; ", groups) : "";
                RoomAllocation.Add(room, groupsStr);
            }
        }
    }
}
