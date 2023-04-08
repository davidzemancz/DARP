using DARP.Models;
using DARP.Utils;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Xceed.Wpf.Toolkit.Calculator;
using Order = DARP.Models.Order;

namespace DARP.Solvers
{
    /// <summary>
    /// Evolutionary solver output
    /// </summary>
    public class EvolutionarySolverOutputV2 : ISolverOutput
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
        public EvolutionarySolverOutputV2()
        {
        }

        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="plan">Plan</param>
        /// <param name="status">Status</param>
        public EvolutionarySolverOutputV2(Plan plan, Status status)
        {
            Plan = plan;
            Status = status;
        }

    }

    /// <summary>
    ///  Evolutionary solver input
    /// </summary>
    public class EvolutionarySolverInputV2 : SolverInputBase
    {
        /// <summary>
        /// Instance of random generator. If null, new instance is created.
        /// </summary>
        public Random RandomInstance { get; set; } = null;

        /// <summary>
        /// Number of generations
        /// </summary>
        public int Generations { get; set; } = 100;

        /// <summary>
        /// Population size
        /// </summary>
        public int PopulationSize { get; set; } = 100;

        /// <summary>
        /// Probability of removing order in insertion mutations
        /// </summary>
        public double RandomOrderRemoveMutProb { get; set; } = 0.4;

        /// <summary>
        /// Probability of trying to randomly insert order into random route
        /// </summary>
        public double RandomOrderInsertMutProb { get; set; } = 0.5;

        /// <summary>
        /// Probability of inserting order into random route in best possible place
        /// </summary>
        public double BestfitOrderInsertMutProb { get; set; } = 0.5;

        /// <summary>
        /// Probability of crossing over two plans
        /// </summary>
        public double PlanCrossoverProb {  get; set; } = 0.3;

        /// <summary>
        /// Probability of crossing over two routes in the same plan
        /// </summary>
        public double RouteCrossoverProb { get; set; } = 0.3;

        /// <summary>
        /// Function for logging fitness
        /// </summary>
        public FitnessLogFunc FitnessLog { get; set; }

        /// <summary>
        /// Use adaptive mutation. Decreases mutations probability over generations.
        /// </summary>
        public bool AdaptiveMutation { get; set; }

        /// <summary>
        /// Enviromental selection
        /// </summary>
        public EvolutionarySelection EnviromentalSelection { get; set; } = EvolutionarySelection.Elitism;

        /// <summary>
        /// Parental selection
        /// </summary>
        public EvolutionarySelection ParentalSelection { get; set; } = EvolutionarySelection.Tournament;

        /// <summary>
        /// Heuristic that is used after crossover to insert remained orders
        /// </summary>
        public InsertionHeuristicFunc CrossoverInsertionHeuristic { get; set; } = null;

        /// <summary>
        /// Initialize
        /// </summary>
        public EvolutionarySolverInputV2() { }

        /// <summary>
        /// Initialize EvolutionarySolverInput base on SolverInputBase instance
        /// </summary>
        /// <param name="solverInputBase">Instance</param>
        public EvolutionarySolverInputV2(SolverInputBase solverInputBase) : base(solverInputBase) { }

       
    }

    /// <summary>
    /// Evolutionary solver
    /// </summary>
    public class EvolutionarySolver2 : ISolver
    {
        private Random _random;
        private EvolutionarySolverInputV2 _input;

        /// <summary>
        /// Run evolutionary solver
        /// </summary>
        /// <param name="input">Input</param>
        ISolverOutput ISolver.Run(ISolverInput input)
        {
            return Run((EvolutionarySolverInputV2)input);
        }

        /// <summary>
        /// Run evolutionary solver
        /// </summary>
        /// <param name="input">Input</param>
        public EvolutionarySolverOutputV2 Run(EvolutionarySolverInputV2 input) 
        {
            

            return new EvolutionarySolverOutputV2(null, Status.Success);
        }
    }

    
}
