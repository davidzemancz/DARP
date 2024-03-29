﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Utils
{
    public enum EvolutionarySelection
    {
        None = 0,
        Elitism = 1,
        Tournament = 2,
        Roulette = 3,
    }

    public enum ParentalSelection
    {
        RouletteWheel = 0,
    }

    public enum Metric
    {
        Manhattan,
        Euclidean
    }

    public enum InsertionHeuristicsMode
    {
        /// <summary>
        /// Gets orders in order and inserts them to first possible place
        /// </summary>
        FirstFit = 0,
        /// <summary>
        /// Gets orders in order and inserts them to best possible place
        /// </summary>
        LocalBestFit = 1,
        /// <summary>
        /// Finds best fitting order and inserts it until there are none
        /// </summary>
        GlobalBestFit = 2,
        /// <summary>
        /// Finds best fitting order and inserts it until there are none
        /// </summary>
        RandomizedGlobalBestFit = 3,
    }

    public enum StatusCode
    {
        Ok = 0,
        Failed = 1,
        Exception = 2,
    }

    public enum InsertionObjective
    {
        MinimizeDeliveryTime = 0,
        MinimizeDistance = 1,
        MaximizeProfit = 2,
    }

    public enum OptimizationObjective
    {
        /// <summary>
        /// Not supported yet
        /// </summary>
        MinimizeDeliveryTime = 0,
        /// <summary>
        /// Minimizes distance traveled by vehicles. All orders must be handled for optimality.
        /// </summary>
        MinimizeTime = 1,
        /// <summary>
        /// Maximizes profit. Not all orders must be handled for optimality.
        /// </summary>
        MaximizeProfit = 2,
    }

    public enum OptimizationMethod
    {
        Disabled = 0,
        MIP = 1,
        Evolutionary = 2,
        AntColony = 3,
        GraphSearch = 4,
    }

    public enum LogLevel
    {
        Debug = 1,
        Info = 2,
        Warn = 3,
        Error = 4,
        Fatal = 5,
    }
}
