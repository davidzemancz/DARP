using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace DARP.Models
{
    public class Plan
    {
        public List<Route> Routes { get; set; } = new();
        
        public Plan()
        {

        }
        
        public Plan Clone()
        {
            Plan plan = new Plan();
            plan.Routes = Routes.Select(r => r.Clone()).ToList();
            return plan;
        }
       
    }
}
