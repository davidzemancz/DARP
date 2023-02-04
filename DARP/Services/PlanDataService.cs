﻿using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public interface IPlanDataService
    {
        Plan GetPlan();
        void SetPlan (Plan plan);
    }

    public class PlanDataService : IPlanDataService
    {
        private Plan _plan;

        public PlanDataService()
        {
            _plan = new Plan();
        }

        public Plan GetPlan() => _plan;

        public void SetPlan(Plan plan) => _plan = plan;
    }
}
