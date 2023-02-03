using DARP.Models;
using DARP.Services;
using DARP.Solvers;
using DARP.Utils;
using System;
using System.Diagnostics;

namespace DARPConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Time time = Time.Zero;
            Random random = new((int)DateTime.Now.Ticks);

            SolverInputBase input = new()
            {
                Metric = XMath.ManhattanMetric,
                Time = time,
                VehicleChargePerMinute = 2,
                Plan = new(),
            };

            List<Order> orders = new();
            const int MAP_SIZE = 20;
            const int PROFIT_PM = 5;
            const int ORDERS = 80;
            for (int o = 1; o <= ORDERS; o++)
            {
                Cords pickup = new Cords(random.Next(0, MAP_SIZE), random.Next(0, (int)MAP_SIZE));
                Cords delivery = new Cords(random.Next(0, MAP_SIZE), random.Next(0, (int)MAP_SIZE));

                double totalProfit = PROFIT_PM * XMath.ManhattanMetric(pickup, delivery).Minutes;

                Time maxDeliveryTime = new Time(time.ToDouble() + 60 + random.Next(60));
                orders.Add(new Order()
                {
                    Id = o,
                    PickupLocation = pickup,
                    DeliveryLocation = delivery,
                    MaxDeliveryTime = maxDeliveryTime,
                    TotalProfit = totalProfit
                });
            };
            input.Orders = orders;

            const int VEHICLES = 20;
            List<Vehicle> vehicles = new();
            for (int v = 0; v < VEHICLES; v++)
            {
                Vehicle vehicle = new()
                {
                    Id = v,
                    Location = new Cords(random.Next(0, MAP_SIZE), random.Next(0, (int)MAP_SIZE))
                };
                vehicles.Add(vehicle);
                input.Plan.Routes.Add(new Route(vehicle, time));
            }
            input.Vehicles = vehicles;

            LoggerBase.Instance.TextWriters.Add(Console.Out);

            Stopwatch sw = new();
            sw.Start();
            EvolutionarySolver es = new();
            es.Run(new EvolutionarySolverInput(input));
            sw.Stop();
            Console.WriteLine(sw.Elapsed);
            sw.Reset();

            //sw.Start();
            //MIPSolver ms = new();
            //ms.Run(new MIPSolverInput(input) { Multithreading = true, TimeLimit = 5 * 60_000 });
            //sw.Stop();
            //Console.WriteLine(sw.Elapsed);
        }
    }
}