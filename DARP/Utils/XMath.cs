﻿using DARP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Utils
{
    public delegate Time MetricFunc(Cords2D c1, Cords2D c2);

    internal class XMath
    {
        public static T Max<T>(T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) > 0 ? x : y;
        }

        public static T Min<T>(T x, T y) where T : IComparable<T>
        {
            return x.CompareTo(y) < 0 ? x : y;
        }

        public static Time ManhattanMetric(Cords2D c1, Cords2D c2) 
        {
            double distance = Math.Abs(c1.X - c2.X) + Math.Abs(c1.Y - c2.Y);
            return new Time(distance); 
        }
        public static Time EuclideanMetric(Cords2D c1, Cords2D c2)
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

        public static int RandomIndexByWeight<T>(T[] sequence, double[] weights)
        {
            Random random = new();

            double totalWeight = weights.Sum();
            if (totalWeight == 0) return -1;

            // The weight we are after...
            double itemWeightIndex = (double)random.NextDouble() * totalWeight;
            double currentWeightIndex = 0;

            for (int i = 0; i < weights.Length; i++)
            {
                currentWeightIndex += weights[i];

                if (currentWeightIndex >= itemWeightIndex)
                    return i;
            }

            throw new NotImplementedException();
        }

        public static T RandomElementByWeight<T>(IEnumerable<T> sequence, Func<T, double> weightSelector)
        {
            Random random = new();

            double totalWeight = sequence.Sum(weightSelector);
            // The weight we are after...
            double itemWeightIndex = (double)random.NextDouble() * totalWeight;
            double currentWeightIndex = 0;

            foreach (var item in from weightedItem in sequence select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
            {
                currentWeightIndex += item.Weight;

                // If we've hit or passed the weight we are after for this item then it's the one we want....
                if (currentWeightIndex >= itemWeightIndex)
                    return item.Value;

            }

            return sequence.Last();
        }
    }

}
