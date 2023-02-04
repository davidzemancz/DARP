using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace DARP.Models
{
    public class Plan
    {
        public List<Route> Routes { get; set; } = new();
        
        public Plan()
        {

        }
        
        public double GetTotalProfit(MetricFunc metric, double vehicleCharge)
        {
            double totalProfit = 0;
            foreach (Route route in Routes)
            {
                totalProfit += route.GetTotalProfit(metric, vehicleCharge);
            }
            return totalProfit;
        }

        public double GetTotalTimeTraveled()
        {
            double totalTime = 0;
            foreach (Route route in Routes)
            {
                totalTime += route.GetTotalTimeTraveled();
            }
            return totalTime;
        }

        public (double profit, List<Order> removedOrders) UpdateVehiclesLocation(Time time, MetricFunc metric, double vehicleCharge)
        {
            double profit = 0;
            List<Order> removedOrders = new();
            foreach (Route route in Routes)
            {
                (double routeProfit, List<Order> removedOrdersFromRoute) = route.UpdateVehiclesLocation(time, metric, vehicleCharge);
                removedOrders.AddRange(removedOrdersFromRoute);
                profit += routeProfit;
            }
            return (profit, removedOrders);
        }

        public bool Contains(Order order)
        {
            foreach (Route route in Routes)
            {
                bool found = route.Contains(order);
                if (found) return true;
            }
            return false;
        }

        public Plan Clone()
        {
            Plan plan = new Plan();
            plan.Routes = new(Routes.Select(r => r.Clone()));
            return plan;
        }
       
    }
}
