using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    /// <summary>
    /// Time window structure
    /// </summary>
    public struct TimeWindow
    {
        /// <summary>
        /// Time from
        /// </summary>
        public Time From { get; set; } = Time.Zero;

        /// <summary>
        /// Time to
        /// </summary>
        public Time To { get; set; } = Time.Zero;

        /// <summary>
        /// Initialize TimeWindow
        /// </summary>
        public TimeWindow(Time from, Time to) 
        {
            From = from;
            To = to;
        }

        /// <summary>
        /// Returns user-friendly formated string 
        /// </summary>
        public override string ToString()
        {
            return $"{From}-{To}";
        }
    }
}
