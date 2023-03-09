using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Models
{
    /// <summary>
    /// Time structure using Ticks as default units.
    /// </summary>
    public struct Time : IComparable<Time>
    {
        /// <summary>
        /// Zero time
        /// </summary>
        public static readonly Time Zero = new Time(0);

        /// <summary>
        /// Ticks
        /// </summary>
        public long Ticks { get; set; }

        /// <summary>
        /// Initialize the time structure
        /// </summary>
        /// <param name="ticks">Ticks</param>
        public Time(long ticks)
        {
            Ticks = ticks;
        }

        /// <summary>
        /// Initialize the time structure
        /// </summary>
        /// <param name="ticks">Ticks</param>
        public Time(double ticks)
        {
            Ticks = (long)ticks;
        }

        /// <summary>
        /// Returns Ticks as double
        /// </summary>
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

        /// <summary>
        /// Implementation for IComparable
        /// </summary>
        public int CompareTo(Time other)
        {
            return (int)(Ticks - other.Ticks);
        }

        /// <summary>
        /// Implementation for IComparable
        /// </summary>
        public override bool Equals(object obj)
        {
            return this == (Time)obj;
        }

        /// <summary>
        /// Returns user-friendly formated string 
        /// </summary>
        public override string ToString()
        {
            return Ticks.ToString();
        }

        /// <summary>
        /// Returns Ticks
        /// </summary>
        public override int GetHashCode()
        {
            return Ticks.GetHashCode();
        }

        /// <summary>
        /// Reutnrs new istance of Time with Ticks increased
        /// </summary>
        public Time AddTicks(int ticks)
        {
            return new Time(Ticks + ticks);
        }
    }

    public static class TimeExtensions
    {
        public static Time NextTime(this Random random, Time lb, Time ub)
        {
            return new Time(random.Next((int)lb.Ticks, (int)ub.Ticks));
        }
    }
}
