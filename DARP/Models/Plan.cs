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
    /// <summary>
    /// Plan is a collection of routes for fixed set of vehicles. For each vehicle there exists exactly one route.
    /// </summary>
    public class Plan
    {
        /// <summary>
        /// Collection of routes
        /// </summary>
        public List<Route> Routes { get; set; } = new();

        /// <summary>
        /// Collection of order that are scheduled on routes. Iterates over Routes and yields orders.
        /// </summary>
        public IEnumerable<Order> Orders
        {
            get
            {
                foreach (var route in Routes)
                    foreach(var order in route.Orders)
                        yield return order;
                yield break;
            }
        }
        
        /// <summary>
        /// Returns sum of total profits of all routes
        /// </summary>
        /// <param name="metric">Metric</param>
        /// <param name="vehicleChargePerTick">Vehicle's charge per tick</param>
        public double GetTotalProfit(MetricFunc metric, double vehicleChargePerTick)
        {
            double totalProfit = 0;
            foreach (Route route in Routes)
            {
                totalProfit += route.GetTotalProfit(metric, vehicleChargePerTick);
            }
            return totalProfit;
        }

        /// <summary>
        /// Update vehicles location for each route
        /// </summary>
        /// <param name="time">Current time</param>
        /// <param name="metric">Metric</param>
        /// <param name="vehicleChargePerTick">Vehicle's charge per tick</param>
        public (double profit, List<Order> removedOrders) UpdateVehiclesLocation(Time time, MetricFunc metric, double vehicleChargePerTick)
        {
            double profit = 0;
            List<Order> removedOrders = new();
            foreach (Route route in Routes)
            {
                (double routeProfit, List<Order> removedOrdersFromRoute) = route.UpdateVehiclesLocation(time, metric, vehicleChargePerTick);
                removedOrders.AddRange(removedOrdersFromRoute);
                profit += routeProfit;
            }
            return (profit, removedOrders);
        }

        /// <summary>
        /// Check whether the plan contains an order
        /// </summary>
        /// <param name="order">The order</param>
        public bool Contains(Order order)
        {
            foreach (Route route in Routes)
            {
                bool found = route.Contains(order);
                if (found) return true;
            }
            return false;
        }

        /// <summary>
        /// Clone the plane and all its routes
        /// </summary>
        public Plan Clone()
        {
            Plan plan = new Plan();
            plan.Routes = new(Routes.Select(r => r.Clone()));
            return plan;
        }
       
    }
}
