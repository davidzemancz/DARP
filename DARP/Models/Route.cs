using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public class Route
    {
        public Vehicle Vehicle { get; set; }
        public List<RoutePoint> Points { get; set; }
        public Route()
        {

        }
    }
}
