using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    public struct Time : IComparable<Time>
    {
        public static readonly Time Zero = new Time(0);
        public double Ticks { get; set; }

        public Time(int ticks)
        {
            Ticks = ticks;
        }

        public Time(double ticks)
        {
            Ticks = ticks;
        }

        public double ToDouble()
        {
            return Ticks;
        }

        public static Time operator +(Time left, Time right)
        {
            return new(left.Ticks + right.Ticks);
        }

        public static Time operator -(Time left, Time right)
        {
            return new(left.Ticks - right.Ticks);
        }

        public static bool operator ==(Time left, Time right)
        {
            return left.Ticks == right.Ticks;
        }

        public static bool operator !=(Time left, Time right)
        {
            return !(left == right);
        }

        public static bool operator <(Time left, Time right)
        {
            return left.Ticks < right.Ticks;
        }

        public static bool operator >(Time left, Time right)
        {
            return left.Ticks > right.Ticks;
        }
        public static bool operator <=(Time left, Time right)
        {
            return left.Ticks < right.Ticks || left.Ticks == right.Ticks;
        }

        public static bool operator >=(Time left, Time right)
        {
            return left.Ticks > right.Ticks || left.Ticks == right.Ticks;
        }

        public int CompareTo(Time other)
        {
            return (int)(Ticks - other.Ticks);
        }

        public override bool Equals(object obj)
        {
            return this == (Time)obj;
        }

        public override string ToString()
        {
            return Ticks.ToString();
        }

        public override int GetHashCode()
        {
            return Ticks.GetHashCode();
        }
    }
}
