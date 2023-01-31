﻿using DARP.Providers;
using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DARP.Utils;

namespace DARP.Services
{
    public interface IPlanningService
    {
        public IPlanningServiceOutput UpdatePlan(IPlanningServiceInput input);
    }

    public interface IPlanningServiceInput
    {
        Time Time { get; set; }
        Plan Plan { get; set; }
        List<Vehicle> Vehicles { get; set; }
        List<Order> Orders { get; set; }
        Func<Cords, Cords, double> Metric { get; set; }
    }

    public interface IPlanningServiceOutput
    {
        Plan Plan { get; }
        Status Status { get; }
    }
}
