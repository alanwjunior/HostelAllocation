using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HostelAllocationOptimization.Model
{
    public class ModelResultDTO
    {
        public string JsonFile { get; set; }
        public List<DailyRoomAllocation> RoomsAllocation { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public int NumVariables { get; set; }
        public int NumConstr { get; set; }

        public ModelResultDTO()
        {
            RoomsAllocation = new List<DailyRoomAllocation>();
        }

        public ModelResultDTO(string jsonFile, List<DailyRoomAllocation> roomsAllocation, TimeSpan executionTime, int numVariables, int numConstr)
        {
            JsonFile = jsonFile;
            RoomsAllocation = roomsAllocation;
            ExecutionTime = executionTime;
            NumVariables = numVariables;
            NumConstr = numConstr;
        }
    }
}
