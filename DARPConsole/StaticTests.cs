using DARP.Models;
using DARP.Solvers;
using DARP.Utils;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARPConsole
{
    internal class StaticTests
    {
        static Random _random = new(666);


        public static void Run()
        {
            const int RUNS = 10;
            for (int i = 0; i < RUNS; i++)
            {
                Console.WriteLine($"----- Run {i} -----");
                RunOnce();
            }
        }

        public static void RunOnce()
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
            const int PROFIT_PM = 5;
            const int ORDERS = 40;
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
            EvolutionarySolverInput esInput = new(input);
            esInput.Generations = 5000;
            esInput.MaxPopulationSize = 100;
            esInput.BestfitOrderInsertMutProb = 1;
            esInput.RandomOrderInsertMutProb = 1;
            esInput.RandomOrderRemoveMutProb = 0.4;
            esInput.EnviromentalSelection = EnviromentalSelection.Tournament;
            esInput.FitnessLog = (g, f) => { if (g % 50 == 0) Console.WriteLine($"{g}> [{string.Join(";",f)}]"); };
            EvolutionarySolver es = new();
            EvolutionarySolverOutput output = es.Run(esInput);
            double eProfit = output.Plan.GetTotalProfit(esInput.Metric, esInput.VehicleChargePerTick);
            Console.WriteLine($"Evolution {eProfit}, time {sw.Elapsed}");

            sw.Restart();
            InsertionHeuristicsInput insHInput2 = new(input);
            InsertionHeuristics insH2 = new();
            InsertionHeuristicsOutput insHOutput2 = insH2.RunFirstFit(insHInput2);
            double iProfit2 = insHOutput2.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            Console.WriteLine($"Insertion heuristics (first fit) {iProfit2}, time {sw.Elapsed}");

            sw.Restart();
            InsertionHeuristicsInput insHInput = new(input);
            InsertionHeuristics insH = new();
            InsertionHeuristicsOutput insHOutput = insH.RunLocalBestFit(insHInput);
            double iProfit = insHOutput.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            Console.WriteLine($"Insertion heuristics (local best fit) {iProfit}, time {sw.Elapsed}");

            sw.Restart();
            InsertionHeuristicsInput insHInput3 = new(input);
            InsertionHeuristics insH3 = new();
            InsertionHeuristicsOutput insHOutput3 = insH3.RunGlobalBestFit(insHInput3);
            double iProfit3 = insHOutput3.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            Console.WriteLine($"Insertion heuristics (global first fit) {iProfit3}, time {sw.Elapsed}");

            //sw.Restart();
            //MIPSolverInput mipInput = new(input);
            //mipInput.Solver = "SCIP";
            //mipInput.TimeLimit = 30_000;
            //MIPSolver ms = new();
            //MIPSolverOutput mipOutput = ms.Run(mipInput);
            //double mProfit = mipOutput.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            //Console.WriteLine($"MIP {mipInput.Solver} {mProfit}, time {sw.Elapsed}");

            //sw.Restart();
            //MIPSolverInput mipInput2 = new(input);
            //mipInput2.Solver = "CP-SAT";
            //mipInput2.TimeLimit = 60_000;
            //MIPSolver ms2 = new();
            //MIPSolverOutput mipOutput2 = ms2.Run(mipInput2);
            //double mProfit2 = mipOutput2.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            //Console.WriteLine($"MIP {mipInput2.Solver} {mProfit2}, time {sw.Elapsed}");
        }
    }
}
