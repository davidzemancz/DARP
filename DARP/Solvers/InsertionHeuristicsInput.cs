using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class InsertionHeuristicsInput : ISolverInput
    {
        public InsertionHeuristicsMode Mode { get; set; }
        public InsertionObjective Objective { get; set; }
        public Time Time { get; set; }
        public Plan Plan { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; }
        public IEnumerable<Order> Orders { get; set; }
        public Func<Cords, Cords, double> Metric { get; set; }
    }
}
