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
        /// <summary>
        /// Gets orders in order and inserts them to first possible place
        /// </summary>
        FirstFit = 1,
        /// <summary>
        /// Gets orders in order and inserts them to best possible place
        /// </summary>
        LocalBestFit = 2,
        /// <summary>
        /// Finds best fitting order and inserts it until there are none
        /// </summary>
        GlobalBestFit = 3,
    }

    public enum StatusCode
    {
        Ok = 0,
        Failed = 1,
        Exception = 2,
    }

    public enum InsertionObjective
    {
        DeliveryTime = 0,
        Distance = 1,
    }

    public enum OptimizationObjective
    {
        DeliveryTime = 0,
        Distance = 1,
    }

    public enum OptimizationMethod
    {
        Disabled = 0,
        MIP = 1,
        Evolutionary = 2,
        AntColony = 3
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4,
    }
}
