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
    public interface IMIPSolverService
    {
        Plan Plan { get; set; }
        public MIPSolverParamsProvider ParamsProvider { get; }
        public Status Run(Time currentTime, IEnumerable<Order> newOrders);
    }
}
