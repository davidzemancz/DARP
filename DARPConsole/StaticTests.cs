﻿using DARP.Models;
using DARP.Solvers;
using DARP.Utils;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace DARPConsole
{
    internal class StaticTests
    {
        static Random _random = new(666);


        public static void Run()
        {
            const int RUNS = 20;
            for (int i = 0; i < RUNS; i++)
            {
                RunOnce();
            }
        }

        private static void BuildGraph()
        {

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
            const int ORDERS = 60;
            for (int o = 1; o <= ORDERS; o++)
            {
                Cords pickup = new Cords(_random.Next(0, MAP_SIZE), _random.Next(0, (int)MAP_SIZE));
                Cords delivery = new Cords(_random.Next(0, MAP_SIZE), _random.Next(0, (int)MAP_SIZE));

                double totalProfit = PROFIT_PM * XMath.ManhattanMetric(pickup, delivery).Ticks;

                Time maxDeliveryTimeFrom = new Time(time.ToDouble() + 60 + _random.Next(60));
                Time maxDeliveryTimeTo = maxDeliveryTimeFrom + new Time(20);
                orders.Add(new Order()
                {
                    Id = o,
                    PickupLocation = pickup,
                    DeliveryLocation = delivery,
                    DeliveryTime = new TimeWindow(maxDeliveryTimeFrom, maxDeliveryTimeTo),
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

            sw.Restart();
            InsertionHeuristicsInput insHInput2 = new(input);
            InsertionHeuristics insH2 = new();
            InsertionHeuristicsOutput insHOutput2 = insH2.RunFirstFit(insHInput2);
            double iProfit2LBF = insHOutput2.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            //Console.WriteLine($"Insertion heuristics (first fit) {iProfit2}, time {sw.Elapsed}");

            sw.Restart();
            InsertionHeuristicsInput insHInput = new(input);
            InsertionHeuristics insH = new();
            InsertionHeuristicsOutput insHOutput = insH.RunLocalBestFit(insHInput);
            double iProfitFF = insHOutput.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            //Console.WriteLine($"Insertion heuristics (local best fit) {iProfit}, time {sw.Elapsed}");

            sw.Restart();
            InsertionHeuristicsInput insHInput3 = new(input);
            InsertionHeuristics insH3 = new();
            InsertionHeuristicsOutput insHOutput3 = insH3.RunGlobalBestFit(insHInput3);
            double iProfit3GBF = insHOutput3.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            //Console.WriteLine($"Insertion heuristics (global first fit) {iProfit3}, time {sw.Elapsed}");

            sw.Restart();
            InsertionHeuristicsInput insHInput4 = new(input);
            InsertionHeuristics insH4 = new();
            insHInput4.Epsilon = 0.1;
            double iProfit4Max = double.MinValue;
            for (int i = 0; i < 100; i++)
            {
                InsertionHeuristicsOutput insHOutput4 = insH4.RunRandomizedGlobalBestFit(insHInput4);
                double iProfit4 = insHOutput4.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                if (iProfit4 > iProfit4Max) iProfit4Max = iProfit4;
            }
            //Console.WriteLine($"Insertion heuristics (randomized global first fit) {iProfit4Max}, time {sw.Elapsed}");

            sw.Restart();
            InsertionHeuristicsInput insHInput5 = new(input);
            InsertionHeuristics insH5 = new();
            insHInput5.Epsilon = 0.2;
            double iProfit5Max = double.MinValue;
            for (int i = 0; i < 20; i++)
            {
                InsertionHeuristicsOutput insHOutput5 = insH5.RunRandomizedGlobalBestFit(insHInput5);
                double iProfi5 = insHOutput5.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                if (iProfi5 > iProfit5Max) iProfit5Max = iProfi5;
            }

            sw.Restart();
            InsertionHeuristicsInput insHInput6 = new(input);
            InsertionHeuristics insH6 = new();
            insHInput6.Epsilon = 0.4;
            double iProfit6Max = double.MinValue;
            for (int i = 0; i < 20; i++)
            {
                if (i > 0 && i % 5 == 0) insHInput6.Epsilon /= 2;

                InsertionHeuristicsOutput insHOutput6 = insH6.RunRandomizedGlobalBestFit(insHInput6);
                double iProfi6 = insHOutput6.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
                if (iProfi6 > iProfit6Max) iProfit6Max = iProfi6;
            }

            sw.Restart();
            EvolutionarySolverInput esInput = new(input);
            esInput.Generations = 100;
            esInput.PopulationSize = 100;
            esInput.BestfitOrderInsertMutProb = 0.7;
            esInput.RandomOrderInsertMutProb = 0.4;
            esInput.RandomOrderRemoveMutProb = 0.45;
            esInput.RouteCrossoverProb = 0.3;
            esInput.PlanCrossoverProb = 0.3;
            esInput.EnviromentalSelection = EnviromentalSelection.Tournament;
            //esInput.FitnessLog = (g, f) => { if (g % 50 == 0) Console.WriteLine($"{g}> [{string.Join(";", f)}]"); };
            EvolutionarySolver es = new();
            EvolutionarySolverOutput output = es.Run(esInput);
            double eProfit = output.Plan.GetTotalProfit(esInput.Metric, esInput.VehicleChargePerTick);
            //Console.WriteLine($"Evolution {eProfit}, time {sw.Elapsed}");

            sw.Restart();
            EvolutionarySolverInput esInput2 = new(input);
            esInput2.Generations = 300;
            esInput2.PopulationSize = 100;
            esInput2.BestfitOrderInsertMutProb = 1;
            esInput2.RandomOrderInsertMutProb = 1;
            esInput2.RandomOrderRemoveMutProb = 0.45;
            esInput2.RouteCrossoverProb = 0.3;
            esInput2.PlanCrossoverProb = 0.3;
            esInput2.AdaptiveMutation = true;   
            esInput2.EnviromentalSelection = EnviromentalSelection.Tournament;
            esInput2.CrossoverInsertionHeuristic = new InsertionHeuristics().RunRandomizedGlobalBestFit;
            //esInput.FitnessLog = (g, f) => { if (g % 50 == 0) Console.WriteLine($"{g}> [{string.Join(";", f)}]"); };
            EvolutionarySolver es2 = new();
            EvolutionarySolverOutput output2 = es2.Run(esInput2);
            double eProfit2 = output2.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            //Console.WriteLine($"Evolution {eProfit}, time {sw.Elapsed}");

            Console.WriteLine(
                $"{iProfitFF,6:N0} ({iProfitFF / iProfit3GBF,3:N2})|" +
                $"{iProfit2LBF,6:N0} ({iProfitFF / iProfit3GBF,3:N2})|" +
                $"{iProfit3GBF,6:N0} ({iProfit3GBF / iProfit3GBF,3:N2})|" +
                $"{iProfit4Max,6:N0} ({iProfit4Max / iProfit3GBF,3:N2})|" +
                $"{iProfit5Max,6:N0} ({iProfit5Max / iProfit3GBF,3:N2})|" +
                $"{iProfit6Max,6:N0} ({iProfit6Max / iProfit3GBF,3:N2})|" +
                $"{eProfit,6:N0} ({eProfit / iProfit3GBF,3:N2})|" +
                $"{eProfit2,6:N0} ({eProfit2 / iProfit3GBF,3:N2})|");


            //sw.Restart();
            //MIPSolverInput mipInput = new(input);
            //mipInput.Solver = "SCIP";
            ////mipInput.TimeLimit = 30_000;
            //mipInput.Integer = false;
            //MIPSolver ms = new();
            //MIPSolverOutput mipOutput = ms.Run(mipInput);
            //double mProfit = mipOutput.ObjetiveValue;
            //Console.WriteLine($"MIP {mipInput.Solver} {mProfit}, time {sw.Elapsed}");

            //sw.Restart();
            //MIPSolverInput mipInput2 = new(input);
            //mipInput2.Solver = "SCIP";
            //mipInput2.TimeLimit = 30_000;
            //MIPSolver ms2 = new();
            //MIPSolverOutput mipOutput2 = ms2.Run(mipInput2);
            //double mProfit2 = mipOutput2.Plan.GetTotalProfit(input.Metric, input.VehicleChargePerTick);
            //Console.WriteLine($"MIP {mipInput2.Solver} {mProfit2}, time {sw.Elapsed}");
        }
    }
}
