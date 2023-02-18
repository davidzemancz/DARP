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

        public TimeWindow()
        {
            From = Time.Zero;
            To = Time.Zero;
        }

        public TimeWindow(Time from, Time to) 
        {
            From = from;
            To = to;
        }

        public override string ToString()
        {
            return $"{From}-{To}";
        }
    }
}
