using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class PlanningService
    {
        public Plan Plan { get; protected set; }

        public Plan InitPlan()
        {
            Plan = new Plan();
            return Plan;
        }

        public void UpdatePlan(Time currentTime, List<Order> newOrders)
        {
            // Update vehicles locations
            // ...

            // Remove handled and timeouted orders
            // ...

            // Try insertion heuristics
            // ...

            // Try greedy procedure
            // ...

            // Run optimization
            // ...

        }
    }
}
