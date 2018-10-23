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
                "simulation20.json",
                "simulation21.json",
                "simulation22.json",
                "simulation23.json",
                "simulation24.json",
                "simulation25.json"
            };
            Optimizer.HostelAllocationOptimizer.OptimizeWithGroupSplit(jsonFiles);
        }
    }
}
