using DARP.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Providers
{
    public class InsertionHeuristicsParamsProvider
    {
        public Func<InsertionHeuristicsMode> RetrieveMode { get; set; }
        public Func<InsertionObjective> RetrieveObjective { get; set; }
    }
}
