using DARP.Providers;
using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public interface IPlanningService
    {
        public Plan Plan { get; }
        public IMIPSolverService MIPSolverService { get; }
        public IInsertionHeuristicsService InsertionHeuristicsService { get; }
        public PlanningParamsProvider ParamsProvider { get; }
        public Plan Init(Plan plan);
        public void AddVehicle(Time currentTime, Vehicle vehicle);
        public void UpdatePlan(Time currentTime, IEnumerable<Order> newOrders);
        public double GetTotalDistance();
    }
}
