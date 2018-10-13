using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HostelAllocationOptimization.Model;
using Microsoft.AspNetCore.Mvc;

namespace HostelAllocationOptimization.Controllers
{
    [Route("api/[controller]")]
    public class TestFactoryController : Controller
    {
        [HttpGet]
        public HostelAllocationDTO CreateTest(string fileName)
        {
            return TestFactory.TestFactory.CreateRandomTest(fileName);
        }

        [HttpPost]
        public void RunTests()
        {
            List<string> jsonFiles = new List<string>
            {
                "simulation4.json",
                "simulation5.json",
                "simulation6.json",
                "simulation11.json",
                "simulation12.json",
                "simulation13.json",
                "simulation14.json",
                "simulation15.json",
                "simulation16.json",
                "simulation17.json"
            };
            Optimizer.HostelAllocationOptimizer.OptimizeWithGroupSplit(jsonFiles);
        }
    }
}
