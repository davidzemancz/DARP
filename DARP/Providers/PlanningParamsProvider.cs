using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Providers
{
    public class PlanningParamsProvider
    {
        public Func<OptimizationMethod> RetrieveMethod { get; set; }
    }
}
