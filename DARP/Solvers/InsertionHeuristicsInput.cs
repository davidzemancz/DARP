using DARP.Models;
using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class InsertionHeuristicsInput : SolverInputBase
    {
        public InsertionHeuristicsMode Mode { get; set; }
        public InsertionObjective Objective { get; set; }

        public InsertionHeuristicsInput() { }
        public InsertionHeuristicsInput(SolverInputBase solverInputBase) : base(solverInputBase) { }
    }
}
