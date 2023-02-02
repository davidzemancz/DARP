using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class MIPSolverInput : ISolverInput
    {
        public bool Multithreading { get; set; }
        public long TimeLimit { get; set; }
        public OptimizationObjective Objective { get; set; }
        public Time Time { get; set; }
        public Plan Plan { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; }
        public IEnumerable<Order> Orders { get; set; }
        public MetricFunc Metric { get; set; }
        public double VehicleChargePerMinute { get; set; }
    }
}
