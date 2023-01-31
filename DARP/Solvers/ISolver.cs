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
        List<Vehicle> Vehicles { get; set; }
        List<Order> Orders { get; set; }
        Func<Cords, Cords, double> Metric { get; set; }
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
