using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Utils
{
    public interface ILogger
    {
        public IList<TextWriter> TextWriters { get; }
        void Debug(string message);
        void Warn(string message);
        void Error(string message);
        void Fatal(string message);
        void Log(LogLevel level, string message);
    }

    public class LoggerBase : ILogger
    {
        private long _lineCounter = 0;
        private Stack<Stopwatch> _stopwatches = new();
        
        public static LoggerBase Instance { get; protected set; } = new LoggerBase();

        public virtual IList<TextWriter> TextWriters { get; protected set; } = new List<TextWriter>();

        public void StopwatchStart()
        {
            Stopwatch sw = new();
            _stopwatches.Push(sw);
            sw.Start();
            Debug($"Stopwatch {sw.GetHashCode()} started");
        }

        public void StopwatchStop()
        {
            Stopwatch sw = _stopwatches.Pop();
            sw.Stop();
            Debug($"Stopwatch {sw.GetHashCode()} stopped, time {sw.Elapsed}");
        }

        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void Fatal(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public virtual void Log(LogLevel level, string message)
        {
            foreach (TextWriter writer in TextWriters)
            {
                writer.WriteLine($"{++_lineCounter}> {message}");
            }
        }

        public void Warn(string message)
        {
            Log(LogLevel.Warn, message);
        }
    }
}
