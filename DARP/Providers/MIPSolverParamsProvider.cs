﻿using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Providers
{
    public class MIPSolverParamsProvider
    {
        public Func<int> RetrieveTimeLimitSeconds { get; set; }
        public Func<bool> RetrieveMultithreading { get; set; }
        public Func<ObjectiveFunction> RetrieveObjective {  get; set; }
        public Func<int> RetrieveVehicleCharge { get; set; }
    }
}