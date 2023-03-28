using DARP.Models;
using DARP.Solvers;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARPExperiments
{
    public interface IDataset
    {
        int Runs { get; }
        int Seed { get; }
        InsertionHeuristicsInput GetInsertionHeuristicInput();
    }

    internal class DatasetSmall : IDataset
    {
        public int Runs { get; } = 10;
        public int Seed { get; } = 2023;

        public SolverInputBase GetSolverInput()
        {
            SolverInputBase input = new()
            {
                Metric = XMath.ManhattanMetric,
                Time = Time.Zero,
                VehicleChargePerTick = 1,
                Plan = new(),
            };
            return input;
        }

        public InsertionHeuristicsInput GetInsertionHeuristicInput()
        {
            InsertionHeuristicsInput input = new(GetSolverInput());
            return input;
        }
    }
}
