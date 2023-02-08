using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Utils
{
    public delegate Time MetricFunc(Cords c1, Cords c2);
    
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

        public static Time ManhattanMetric(Cords c1, Cords c2) 
        {
            double distance = Math.Abs(c1.X - c2.X) + Math.Abs(c1.Y - c2.Y);
            return new Time(distance); 
        }
        public static Time EuclideanMetric(Cords c1, Cords c2)
        {
            double distance = Math.Sqrt(Math.Pow(c1.X + c2.X, 2) + Math.Pow(c1.Y + c2.Y, 2));
            return new Time(distance);
        }

        public static MetricFunc GetMetric(Metric metric)
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
