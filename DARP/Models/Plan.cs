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
        
        public double GetTotalProfit()
        {
            double totalProfit = 0;
            foreach (Route route in Routes)
            {
                totalProfit += route.GetTotalProfit();
            }
            return totalProfit;
        }

        public double GetTotalTimeTraveled()
        {
            double totalTime = 0;
            foreach (Route route in Routes)
            {
                totalTime += route.GetTotalTimeTraveled();
            }
            return totalTime;
        }

        public Plan Clone()
        {
            Plan plan = new Plan();
            plan.Routes = Routes.Select(r => r.Clone()).ToList();
            return plan;
        }
       
    }
}
