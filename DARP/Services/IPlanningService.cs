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
        Plan Plan { get; }
        public Plan InitPlan(Func<Cords, Cords, Time> travelTime, IReadOnlyList<Vehicle> vehicles);
        public void UpdatePlan(Time currentTime, IReadOnlyList<Order> newOrders);
    }
}
