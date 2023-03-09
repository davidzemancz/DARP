using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    /// <summary>
    /// Coordinates structure in 2 dimensional space
    /// </summary>
    public struct Cords2D
    {
        public static readonly Cords2D Empty = new Cords2D(double.NaN, double.NaN);
        public static readonly Cords2D Zero = new Cords2D(double.NaN, double.NaN);

        /// <summary>
        /// X coordinate
        /// </summary>
        public double X { get; set; } = double.NaN;

        /// <summary>
        /// Y coordinate
        /// </summary>
        public double Y { get; set; } = double.NaN;

        /// <summary>
        /// Creates new instance of coordinates type
        /// </summary>
        public Cords2D(double x, double y)
        {
            X = x; 
            Y = y;
        }

        /// <summary>
        /// Returns user-friendly formated string
        /// </summary>
        public override string ToString()
        {
            return $"{X}{CultureInfo.CurrentCulture.TextInfo.ListSeparator}{Y}";
        }
    }
}
