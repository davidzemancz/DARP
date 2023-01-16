using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.OrTools.LinearSolver;
using System.Windows.Controls;

namespace DARP.Services
{
    public class MIPSolverService : IMIPSolverService
    {
        public Plan Plan { get; set; }

        private Solver _solver;

        public void Solve(Time currentTime, IEnumerable<Order> newOrders)
        {
            // Union orders
            List<Order> orders = new List<Order>(Plan.Orders);
            orders.AddRange(newOrders);

            // Solver
            _solver = Solver.CreateSolver("SCIP");

            // Variables for traveling between orders (and vehicles locations)
            Dictionary<string, Variable> travelVariables = new();
            foreach (Order orderTo in orders)
            {
                foreach (Order orderFrom in orders)
                {
                    Variable travelVar = _solver.MakeBoolVar($"O-{orderFrom.Id}-{orderTo.Id}");
                    travelVariables.Add(travelVar.Name(), travelVar);
                }
                foreach(Vehicle vehicle in Plan.Vehicles)
                {
                    Variable travelVar = _solver.MakeBoolVar($"V-{vehicle.Id}-{orderTo.Id}");
                    travelVariables.Add(travelVar.Name(), travelVar);
                }
            }
            
            // Constraints
            // 1) Route must be continuous
            foreach((_, Variable travelFrom) in travelVariables)
            {
                foreach ((_, Variable travelTo) in travelVariables)
                {
                    //_solver.Add(new SumVarArray(travelVariables.W) == travelTo);
                    // TODO
                }
            }
            

        }
    }
}
