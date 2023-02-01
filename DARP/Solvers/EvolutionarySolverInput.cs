using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class EvolutionarySolverInput : ISolverInput
    {
        public Time Time { get; set; }
        public Plan Plan { get; set; }
        public IEnumerable<Vehicle> Vehicles { get; set; }
        public IEnumerable<Order> Orders { get; set; }
        public Func<Cords, Cords, Time> Metric { get; set; }
        public double VehicleChargePerMinute { get; set; }
    }
}
