using DARP.Models;
using DARP.Providers;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public interface ITSPEvolutionarySolverService
    {
        Plan Plan { get; set; }
        public Status Solve(Time currentTime, IEnumerable<Order> newOrders);
    }
}
