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
        public List<Vehicle> Vehicles { get; set; }
        public List<Order> Orders { get; set; }
        public Func<Cords, Cords, double> Metric { get; set; }
    }
}
