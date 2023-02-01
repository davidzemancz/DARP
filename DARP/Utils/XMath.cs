using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Utils
{
    public class XMath
    {
        public static T Max<T>(T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) > 0 ? x : y;
        }

        public static T Min<T>(T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) < 0 ? x : y;
        }

        public static double ManhattanMetric(Cords c1, Cords c2) 
        {
            return Math.Abs(c1.X - c2.X) + Math.Abs(c1.Y - c2.Y); 
        }
        public static double EuclideanMetric(Cords c1, Cords c2)
        {
            return Math.Sqrt(Math.Pow(c1.X + c2.X, 2) + Math.Pow(c1.Y + c2.Y, 2));
        }

        public static Func<Cords, Cords, double>GetMetric(Metric metric)
        {
            switch (metric)
            {
                case Metric.Manhattan:
                    return ManhattanMetric;
                case Metric.Euclidean:
                    return EuclideanMetric;
                default:
                    throw new NotImplementedException();
            }
        }
    }

}
