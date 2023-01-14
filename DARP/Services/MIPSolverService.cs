using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class MIPSolverService : IMIPSolverService
    {
        public Plan Plan { get; set; }

        public void Solve(Time currentTime, IEnumerable<Order> newOrders)
        {
            
        }
    }
}
