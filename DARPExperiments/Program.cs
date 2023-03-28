using DARP.Solvers;

namespace DARPExperiments
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<IDataset> datasets = new List<IDataset>();
            datasets.Add(new DatasetSmall());

            foreach (IDataset dataset in datasets)
            {
                for (int run = 0; run < dataset.Runs; run++)
                {
                    RunFirstFit(dataset);
                }
            }
        }

        static InsertionHeuristicsOutput RunFirstFit(IDataset dataset)
        {
            InsertionHeuristicsInput input = dataset.GetInsertionHeuristicInput();
            InsertionHeuristicsInput insHInput2 = new(input);
            InsertionHeuristics insH2 = new();
            InsertionHeuristicsOutput insHOutput2 = insH2.RunFirstFit(insHInput2);
            return insHOutput2;
        }
    }
}