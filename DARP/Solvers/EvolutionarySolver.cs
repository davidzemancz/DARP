using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Solvers
{
    public class EvolutionarySolver : ISolver
    {
        ISolverOutput ISolver.Run(ISolverInput input)
        {
            return Run((EvolutionarySolverInput)input);
        }

        public EvolutionarySolverOutput Run(EvolutionarySolverInput input) 
        {
            const int GENERATIONS = 100;
            const int POP_SIZE = 100;

            // Initialize population
            Individual[] population = new Individual[POP_SIZE];
            for (int i = 0; i < population.Length; i++)
            {
                population[i].Plan = input.Plan.Clone();
            }

            // Evolution
            for (int g  = 0; g < GENERATIONS; g++)
            {

            }

            return new EvolutionarySolverOutput();
        }

        public class Individual
        {
            public Plan Plan {  get; set; }
        }
    }

    
}
