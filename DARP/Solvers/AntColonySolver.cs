using DARP.Models;
using DARP.Utils;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DARP.Solvers
{
    /// <summary>
    /// Ant colony solver output
    /// </summary>
    public class AntColonySolverOutput : ISolverOutput
    {
        /// <summary>
        /// Plan
        /// </summary>
        public Plan Plan { get; }

        /// <summary>
        /// Status
        /// </summary>
        public Status Status { get; }

        /// <summary>
        /// Initialize
        /// </summary>
        public AntColonySolverOutput()
        {
        }
    }

    /// <summary>
    /// Ant colony solver output
    /// </summary>
    public class AntColonySolverInput : SolverInputBase
    {
        public int Ants { get; set; } = 500;

        public int Runs { get; set; } = 100;

        public double Alpha { get; set; } = 1;

        public double Beta { get; set; } = 1;

        public double EvaporationCoeficient { get; set; } = 0.3;

        

        /// <summary>
        /// Initialize
        /// </summary>
        public AntColonySolverInput() { }

        /// <summary>
        /// Initialize AntColonySolverInput base on SolverInputBase instance
        /// </summary>
        /// <param name="solverInputBase">Instance</param>
        public AntColonySolverInput(SolverInputBase solverInputBase) : base(solverInputBase) { }
    }

    /// <summary>
    /// Ant colony solver
    /// </summary>
    public class AntColonySolver : ISolver
    {
        private AntColonySolverInput _input;

        /// <summary>
        /// Run ant colony solver
        /// </summary>
        /// <param name="input">Input</param>
        public ISolverOutput Run(ISolverInput input)
        {
            return Run((AntColonySolverInput)input);
        }

        /// <summary>
        /// Run ant colony solver
        /// </summary>
        /// <param name="input">Input</param>
        public AntColonySolverOutput Run(AntColonySolverInput input)
        {
            _input = input;

            Route[] emptyRoutes = _input.Plan.Routes.ToArray();
            Vehicle[] vehicles = emptyRoutes.Select(x => x.Vehicle).ToArray();
            Order[] orders = _input.Orders.OrderBy(o => o.DeliveryTime.From).ToArray();

            double[][] vehiclesPheromoneG = new double[vehicles.Length][];
            double[][] vehiclesAttractivnessG = new double[vehicles.Length][];
            Order[][] vehiclesSuccessorsG = new Order[vehicles.Length][];

            double[][] ordersPheromoneG = new double[orders.Length][];
            double[][] ordersAttractivnessG = new double[orders.Length][];
            Order[][] ordersSuccessorsG = new Order[orders.Length][];

            double maxProfitG = double.MinValue;

            // Initialize successors and pheromone for vehicles
            for (int i = 0; i < emptyRoutes.Length; i++)
            {
                VehicleRoutePoint vrp = (VehicleRoutePoint)emptyRoutes[i].Points[0];
                vehiclesPheromoneG[i] = new double[orders.Length];
                vehiclesAttractivnessG[i] = new double[orders.Length];
                vehiclesSuccessorsG[i] = new Order[0];
                for (int j = 0; j < orders.Length; j++)
                {
                    Order o1 = orders[j];
                    Time o1DeliveryTime = vrp.Time + _input.Metric(vrp.Location, o1.PickupLocation) + _input.Metric(o1.PickupLocation, o1.DeliveryLocation);

                    // Can deliver
                    if (o1DeliveryTime <= o1.DeliveryTime.To)
                    {
                        vehiclesSuccessorsG[i] = vehiclesSuccessorsG[i].Append(o1).ToArray();
                        double finalTime = Math.Abs(((vrp.Time + _input.Metric(vrp.Location, o1.PickupLocation) + _input.Metric(o1.PickupLocation, o1.DeliveryLocation))).ToDouble());
                        vehiclesAttractivnessG[i][j] = 1; // 1 / finalTime;
                        vehiclesPheromoneG[i][j] = 1;//vehiclesAttractivnessG[i][j];
                    }
                }
            }

            // Initialize successors and pheromone for orders
            for (int i = 0; i < orders.Length; i++)
            {
                Order o1 = orders[i];
                ordersPheromoneG[i] = new double[orders.Length];
                ordersAttractivnessG[i] = new double[orders.Length];
                ordersSuccessorsG[i] = new Order[0];
                for (int j = 0; j < orders.Length; j++)
                {
                    Order o2 = orders[j];
                    Time o2LeastDeliveryTime = o1.DeliveryTime.From + _input.Metric(o1.DeliveryLocation, o2.PickupLocation) + _input.Metric(o2.PickupLocation, o2.DeliveryLocation);

                    // Can deliver - need to be checked since we assume that previous order was delivered at o1.DeliveryTime.From
                    if (o2LeastDeliveryTime <= o2.DeliveryTime.To)
                    {
                        ordersSuccessorsG[i] = ordersSuccessorsG[i].Append(o2).ToArray();
                        double finalTime = Math.Abs(((o1.DeliveryTime.From + _input.Metric(o1.DeliveryLocation, o2.PickupLocation) + _input.Metric(o2.PickupLocation, o2.DeliveryLocation))).ToDouble());
                        ordersAttractivnessG[i][j] = 1; // 1 / finalTime;
                        ordersPheromoneG[i][j] = 1; // ordersAttractivnessG[i][j];
                        
                    }
                }
            }

            // Run
            for (int run = 0; run < _input.Runs; run++)
            {
                Route[][] plans = new Route[_input.Ants][];
                List<int>[][] plansOrdersIndicies = new List<int>[plans.Length][];
                double[] totalProfits = new double[plans.Length];

                // Ants
                for (int ant = 0; ant < _input.Ants; ant++)
                {
                    // Copy weights arrays
                    double[][] vehiclesWeights = new double[vehiclesPheromoneG.Length][];
                    for (int i = 0; i < vehiclesWeights.Length; i++)
                    {
                        vehiclesWeights[i] = new double[vehiclesPheromoneG[i].Length];
                        for (int j = 0; j < vehiclesWeights[i].Length; j++)
                            vehiclesWeights[i][j] = Math.Pow(vehiclesPheromoneG[i][j], _input.Alpha) * Math.Pow(vehiclesAttractivnessG[i][j], _input.Beta);
                    }
                    double[][] ordersWeights = new double[ordersPheromoneG.Length][];
                    for (int i = 0; i < ordersWeights.Length; i++)
                    {
                        ordersWeights[i] = new double[ordersPheromoneG[i].Length];
                        for (int j = 0; j < ordersWeights[i].Length; j++)
                            ordersWeights[i][j] = Math.Pow(ordersPheromoneG[i][j], _input.Alpha) * Math.Pow(ordersAttractivnessG[i][j], _input.Beta);;
                    }


                    // Build routes sequentially like DFS
                    Route[] routes = emptyRoutes.Select(er => er.Clone()).ToArray();
                    List<int>[] routesOrdersIndicies = new List<int>[routes.Length];
                    for (int r = 0; r < routes.Length; r++) // An index of the route coresponds to an index of its vehicle
                    {
                        Route route = routes[r];
                        routesOrdersIndicies[r] = new();

                        // Select first order
                        int orderIndex = XMath.RandomIndexByWeight(vehiclesSuccessorsG[r], vehiclesWeights[r]);
                        if (orderIndex < 0) continue;
                        Order order = orders[orderIndex];
                        routesOrdersIndicies[r].Add(orderIndex);

                        // Set pheromone on edge to the order to 0 so it will not be selected twice
                        for (int i = 0; i < vehiclesWeights.Length; i++)
                            vehiclesWeights[i][orderIndex] = 0;
                        for (int i = 0; i < ordersWeights.Length; i++)
                            ordersWeights[i][orderIndex] = 0;

                        // Add order to route
                        Time pickupTime = route.Points[0].Time;
                        Time deliveryTime = pickupTime + _input.Metric(order.PickupLocation, order.DeliveryLocation);
                        deliveryTime = XMath.Max(deliveryTime, order.DeliveryTime.From);

                        route.Points.Add(new OrderPickupRoutePoint(order) { Time = pickupTime });
                        route.Points.Add(new OrderDeliveryRoutePoint(order) { Time = deliveryTime });

                        // Select following orde
                        while (ordersSuccessorsG[orderIndex].Length > 0)
                        {
                            int prevOrderIndex = orderIndex;

                            orderIndex = XMath.RandomIndexByWeight(ordersSuccessorsG[orderIndex], ordersWeights[orderIndex]);
                            if (orderIndex < 0) break;
                            order = orders[orderIndex];

                            // Check whether the order can be delivered
                            int routePointsCount = route.Points.Count;
                            pickupTime = route.Points[routePointsCount - 1].Time + _input.Metric(route.Points[routePointsCount - 1].Location, order.PickupLocation);
                            deliveryTime = pickupTime + _input.Metric(order.PickupLocation, order.DeliveryLocation);
                            deliveryTime = XMath.Max(deliveryTime, order.DeliveryTime.From);
                            if (deliveryTime > order.DeliveryTime.To) // Cannot be delivered
                            {
                                ordersWeights[prevOrderIndex][orderIndex] = 0;
                                continue;
                            }

                            // Add index
                            routesOrdersIndicies[r].Add(orderIndex);

                            // Set pheromone on edge to the order to 0 so it will not be selected twice
                            for (int i = 0; i < vehiclesWeights.Length; i++)
                                vehiclesWeights[i][orderIndex] = 0;
                            for (int i = 0; i < ordersWeights.Length; i++)
                                ordersWeights[i][orderIndex] = 0; 

                            // Add order to route
                            route.Points.Add(new OrderPickupRoutePoint(order) { Time = pickupTime });
                            route.Points.Add(new OrderDeliveryRoutePoint(order) { Time = deliveryTime });
                        }
                    }

                    // Store plan & indicies
                    plans[ant] = routes;
                    plansOrdersIndicies[ant] = routesOrdersIndicies;
                    totalProfits[ant] = routes.Sum(r => r.GetTotalProfit(_input.Metric, _input.VehicleChargePerTick));
                }

                // Vaporize pheromone
                for (int i = 0; i < vehiclesPheromoneG.Length; i++)
                    for (int j = 0; j < vehiclesPheromoneG[i].Length; j++)
                        vehiclesPheromoneG[i][j] *= (1 - _input.EvaporationCoeficient);

                for (int i = 0; i < ordersPheromoneG.Length; i++)
                    for (int j = 0; j < ordersPheromoneG[i].Length; j++)
                        ordersPheromoneG[i][j] *= (1 - _input.EvaporationCoeficient);

                // Update pheromone in global matrices
                double maxProfit = totalProfits.Max();
                if( maxProfit > maxProfitG) maxProfitG = maxProfit;
                double[] relativeProfits = totalProfits.Select(tp => tp / maxProfit).ToArray();
                for (int p = 0; p < plans.Length; p++)
                {
                    //Plan plan = plans[p];
                    List<int>[] routesOrdersIndicies = plansOrdersIndicies[p];
                    double relativeProfit = relativeProfits[p];
                    if (relativeProfit < 0.8) continue;
                    relativeProfit = Math.Pow(relativeProfit, 2);

                    for (int r = 0; r < routesOrdersIndicies.Length; r++)
                    {
                        List<int> routeOrdersIndicies = routesOrdersIndicies[r];

                        // Vehicle - order edge
                        if (routeOrdersIndicies.Count > 0)
                        {
                            int o = routeOrdersIndicies[0];
                            vehiclesPheromoneG[r][o] += (10.0 / _input.Ants) * relativeProfit; // * vehiclesAttractivnessG[r][o];
                            
                            //vehiclesPheromoneG[r][o] = Math.Min(vehiclesPheromoneG[r][o], 5);
                        }

                        // Orders
                        for (int i = 0; i < routeOrdersIndicies.Count - 1; i++)
                        {
                            int o1 = routeOrdersIndicies[i];
                            int o2 = routeOrdersIndicies[i + 1];
                            ordersPheromoneG[o1][o2] += (10.0 / _input.Ants) * relativeProfit; // * ordersAttractivnessG[o1][o2];

                            //ordersPheromoneG[o1][o2] = Math.Min(ordersPheromoneG[o1][o2], 5);
                        }
                    }

                    if (run % 10 == 0)
                    {
                        //Console.WriteLine($"Run {run}, plan {p}: total profit {totalProfits[p]}");
                    }
                    //break;
                }
            }

            Console.WriteLine($"ACO: {maxProfitG}");
            return new AntColonySolverOutput();
        }
    }
}
