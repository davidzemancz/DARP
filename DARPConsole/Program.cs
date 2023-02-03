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
            SolverInputBase input = new()
            {
                Metric = XMath.ManhattanMetric,
                Time = time,
                Vehicles = new[]
                {
                    new Vehicle {Id = 1, Location = new Cords(0,9) },
                    new Vehicle {Id = 2, Location = new Cords(1,4) },
                    new Vehicle {Id = 3, Location = new Cords(5,5) },
                    new Vehicle {Id = 4, Location = new Cords(7,2) },
                    new Vehicle {Id = 5, Location = new Cords(17,2) },
                    new Vehicle {Id = 6, Location = new Cords(4,12) },
                    new Vehicle {Id = 7, Location = new Cords(6,18) },
                    new Vehicle {Id = 8, Location = new Cords(13,11) },
                },
                VehicleChargePerMinute = 2,
                Plan = new(),
            };
            foreach (var vehicle in input.Vehicles)
            {
                input.Plan.Routes.Add(new Route(vehicle, time));
            }

            List<Order> orders = new();
            Random random = new((int)DateTime.Now.Ticks);
            const int MAP_SIZE = 20;
            const int PROFIT_PM = 5;
            const int ORDERS = 40;
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

            LoggerBase.Instance.TextWriters.Add(Console.Out);

            Stopwatch sw = new();
            sw.Start();
            EvolutionarySolver es = new();
            es.Run(new EvolutionarySolverInput(input));
            sw.Stop();
            Console.WriteLine(sw.Elapsed);
            sw.Reset();

            sw.Start();
            MIPSolver ms = new();
            ms.Run(new MIPSolverInput(input) { Multithreading = true, TimeLimit = 5 * 60_000 });
            sw.Stop();
            Console.WriteLine(sw.Elapsed);
        }
    }
}