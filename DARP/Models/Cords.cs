using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public struct Cords
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Cords(double x, double y)
        {
            X = x; 
            Y = y;
        }

        public override string ToString()
        {
            return $"{X}{CultureInfo.CurrentCulture.TextInfo.ListSeparator}{Y}";
        }
    }
}
