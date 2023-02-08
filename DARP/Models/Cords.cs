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
        public static readonly Cords Empty = new Cords(double.NaN, double.NaN);
        public static readonly Cords Zero = new Cords(double.NaN, double.NaN);

        public double X { get; set; } = double.NaN;
        public double Y { get; set; } = double.NaN;

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
