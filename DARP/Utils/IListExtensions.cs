﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Utils
{
    internal static class IListExtensions
    {
        private static Random rnd = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void AddMany<T>(this IList<T> list, Func<T> next, int count)
        {
            for (int i = 0; i < count; i++)
            {
                list.Add(next());
            }
        }
    }
}
