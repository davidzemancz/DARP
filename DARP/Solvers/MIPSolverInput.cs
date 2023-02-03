using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class MIPSolverInput : SolverInputBase
    {
        public bool Multithreading { get; set; }
        public long TimeLimit { get; set; }
        public OptimizationObjective Objective { get; set; } = OptimizationObjective.MaximizeProfit;
       
        public MIPSolverInput() { }
        public MIPSolverInput(SolverInputBase solverInputBase) : base(solverInputBase) { }
    }
}
