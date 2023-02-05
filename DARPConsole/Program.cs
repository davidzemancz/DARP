using DARP.Models;
using DARP.Services;
using DARP.Solvers;
using DARP.Utils;
using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Windows.Controls;

namespace DARPConsole
{
    internal class Program
    {
        static Random _random = new((int)DateTime.Now.Ticks);

        static void Main(string[] args)
        {
            const int RUNS = 10;
            for (int i = 0; i < RUNS; i++)
            {
                Console.WriteLine($"----- Run {i} -----");
                RunSolvers();
            }
        }

        static void RunSolvers()
        {
            Time time = Time.Zero;

            SolverInputBase input = new()
            {
                Metric = XMath.ManhattanMetric,
                Time = time,
                VehicleChargePerTick = 1,
                Plan = new(),
            };

            List<Order> orders = new();
            const int MAP_SIZE = 20;
            const int PROFIT_PM = 3;
            const int ORDERS = 20;
            for (int o = 1; o <= ORDERS; o++)
            {
                Cords pickup = new Cords(_random.Next(0, MAP_SIZE), _random.Next(0, (int)MAP_SIZE));
                Cords delivery = new Cords(_random.Next(0, MAP_SIZE), _random.Next(0, (int)MAP_SIZE));

                double totalProfit = PROFIT_PM * XMath.ManhattanMetric(pickup, delivery).Ticks;

                Time maxDeliveryTime = new Time(time.ToDouble() + 60 + _random.Next(60));
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

            const int VEHICLES = 5;
            List<Vehicle> vehicles = new();
            for (int v = 1; v <= VEHICLES; v++)
            {
                Vehicle vehicle = new()
                {
                    Id = v,
                    Location = new Cords(_random.Next(0, MAP_SIZE), _random.Next(0, (int)MAP_SIZE))
                };
                vehicles.Add(vehicle);
                input.Plan.Routes.Add(new Route(vehicle, time));
            }
            input.Vehicles = vehicles;

            Stopwatch sw = new();
            
            sw.Start();
            EvolutionarySolver es = new();
            EvolutionarySolverOutput output = es.Run(new EvolutionarySolverInput(input));
            double eProfit = output.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            Console.WriteLine($"Evolution {eProfit}, time {sw.Elapsed}");

            //sw.Restart();
            //InsertionHeuristicsInput insHInput = new(input);
            //InsertionHeuristics insH = new();
            //InsertionHeuristicsOutput insHOutput = insH.RunGlobalBestFit(insHInput);
            //double iProfit = insHOutput.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerMinute);
            //Console.WriteLine($"Insertion heuristics (best fit) {iProfit}, time {sw.Elapsed}");

            //sw.Restart();
            //InsertionHeuristicsInput insHInput2 = new(input);
            //InsertionHeuristics insH2 = new();
            //InsertionHeuristicsOutput insHOutput2 = insH2.RunFirstFit(insHInput2);
            //double iProfit2 = insHOutput2.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerMinute);
            //Console.WriteLine($"Insertion heuristics (first fit) {iProfit2}, time {sw.Elapsed}");

            //sw.Restart();
            //MIPSolverInput mipInput = new(input);
            //mipInput.TimeLimit = 10_000;
            //MIPSolver ms = new();
            //MIPSolverOutput mipOutput = ms.Run(mipInput);
            //double mProfit = mipOutput.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerMinute);
            //Console.WriteLine($"MIP {mProfit}, time {sw.Elapsed}");
        }
    }
}