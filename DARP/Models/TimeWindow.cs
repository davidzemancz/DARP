using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public struct TimeWindow
    {
        public Time From {  get; set; }
        public Time To { get; set; }
        public TimeWindow(Time from, Time to) 
        {
            From = from;
            To = to;
        }
    }
}
