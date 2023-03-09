using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    /// <summary>
    /// Solver input
    /// </summary>
    public interface ISolverInput
    {
        /// <summary>
        /// Time
        /// </summary>
        Time Time { get; set; }

        /// <summary>
        /// Plan
        /// </summary>
        Plan Plan { get; set; }

        /// <summary>
        /// Collection of vehicles
        /// </summary>
        IEnumerable<Vehicle> Vehicles { get; set; }

        /// <summary>
        /// Collection of orders
        /// </summary>
        IEnumerable<Order> Orders { get; set; }

        /// <summary>
        /// Metric
        /// </summary>
        MetricFunc Metric { get; set; }

        /// <summary>
        /// Vehicle charge per tick
        /// </summary>
        double VehicleChargePerTick { get; set; }
    }

    /// <summary>
    /// Default basic implementation of ISolverInput
    /// </summary>
    public class SolverInputBase : ISolverInput
    {
        public Time Time { get; set; }
        public Plan Plan { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; }
        public IEnumerable<Order> Orders { get; set; }
        public MetricFunc Metric { get; set; }
        public double VehicleChargePerTick { get; set; }

        public SolverInputBase() { }

        /// <summary>
        /// Initialize SolverInputBase based on another instance
        /// </summary>
        /// <param name="solverInputBase">Instance</param>
        public SolverInputBase(SolverInputBase solverInputBase)
        {
            Time = solverInputBase.Time;
            Plan = solverInputBase.Plan;
            Vehicles = solverInputBase.Vehicles;
            Orders = solverInputBase.Orders;
            Metric = solverInputBase.Metric;
            VehicleChargePerTick = solverInputBase.VehicleChargePerTick;
        }
    }

    /// <summary>
    /// Solver output
    /// </summary>
    public interface ISolverOutput
    {
        /// <summary>
        /// Plan
        /// </summary>
        Plan Plan { get; }

        /// <summary>
        /// Status
        /// </summary>
        Status Status { get; }
    }

    /// <summary>
    /// Solver
    /// </summary>
    public interface ISolver
    {
        /// <summary>
        /// Run solver
        /// </summary>
        /// <param name="input">Input</param>
        /// <returns></returns>
        ISolverOutput Run(ISolverInput input);
    }
}
