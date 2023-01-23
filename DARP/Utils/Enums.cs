using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Utils
{
    public enum Metric
    {
        Manhattan,
        Euclidean
    }

    public enum InsertionHeuristicsMode
    {
        Disabled = 0,
        FirstFit = 1,
        BestFit = 2
    }

    public enum StatusCode
    {
        Ok = 0,
        Failed = 1,
        Exception = 2,
    }

    public enum ObjectiveFunction
    {
        MinimizeDistance,
        MaximizeProfit
    }
}
