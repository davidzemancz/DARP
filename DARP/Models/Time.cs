using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public struct Time : IComparable<Time>
    {
        public int Minutes { get; set; }

        public Time(int minutes)
        {
            Minutes = minutes;
        }

        public static Time operator +(Time left, Time right)
        {
            return new(left.Minutes + right.Minutes);
        }

        public static bool operator ==(Time left, Time right)
        {
            return left.Minutes == right.Minutes;
        }

        public static bool operator !=(Time left, Time right)
        {
            return !(left == right);
        }

        public static bool operator <(Time left, Time right)
        {
            return left.Minutes < right.Minutes;
        }

        public static bool operator >(Time left, Time right)
        {
            return left.Minutes > right.Minutes;
        }
        public static bool operator <=(Time left, Time right)
        {
            return left.Minutes < right.Minutes || left.Minutes == right.Minutes;
        }

        public static bool operator >=(Time left, Time right)
        {
            return left.Minutes > right.Minutes || left.Minutes == right.Minutes;
        }

        public int CompareTo(Time other)
        {
            return Minutes - other.Minutes;
        }

        public override bool Equals(object obj)
        {
            return this == (Time)obj;
        }

    }
}
