using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HostelAllocationOptimization.Model;
using HostelAllocationOptimization.Optimizer;
using Microsoft.AspNetCore.Mvc;

namespace HostelAllocationOptimization.Controllers
{
    [Route("api/[controller]")]
    public class OptimizerController : Controller
    {
        [HttpPost]
        public List<DailyRoomAllocation> Post([FromBody] HostelAllocationDTO hostelAllocation)
        {
            return HostelAllocationOptimizer.Optimize(hostelAllocation);
        }

        [HttpGet]
        public List<DailyRoomAllocation> Post(string jsonFile)
        {
            return HostelAllocationOptimizer.Optimize(jsonFile);
        }

        [Route("Breakgroups")]
        [HttpGet]
        public List<DailyRoomAllocation> OptimizeWithBreakGroups(string jsonFile)
        {
            return HostelAllocationOptimizer.OptimizeWithGroupSplit(jsonFile);
        }
    }
}