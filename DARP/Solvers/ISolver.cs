using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public interface ISolverInput
    {
        Time Time { get; set; }
        Plan Plan { get; set; }
        IEnumerable<Vehicle> Vehicles { get; set; }
        IEnumerable<Order> Orders { get; set; }
        MetricFunc Metric { get; set; }
        double VehicleChargePerMinute { get; set; }
    }

    public class SolverInputBase : ISolverInput
    {
        public Time Time { get; set; }
        public Plan Plan { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; }
        public IEnumerable<Order> Orders { get; set; }
        public MetricFunc Metric { get; set; }
        public double VehicleChargePerMinute { get; set; }

        public SolverInputBase() { }

        public SolverInputBase(SolverInputBase solverInputBase)
        {
            Time = solverInputBase.Time;
            Plan = solverInputBase.Plan;
            Vehicles = solverInputBase.Vehicles;
            Orders = solverInputBase.Orders;
            Metric = solverInputBase.Metric;
            VehicleChargePerMinute = solverInputBase.VehicleChargePerMinute;
        }
    }

    public interface ISolverOutput
    {
        Plan Plan { get; }
        Status Status { get; }
    }

    public interface ISolver
    {
        ISolverOutput Run(ISolverInput input);
    }
}
