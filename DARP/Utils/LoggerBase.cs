using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        public bool DisplayLevel { get; set; }
        public bool DisplayThread { get; set; }
        public bool DisplayLineNumbers { get; set; } = true;

        public static LoggerBase Instance { get; protected set; } = new LoggerBase();

        public virtual IList<TextWriter> TextWriters { get; protected set; } = new List<TextWriter>();

        public void StopwatchStart()
        {
            int count = 0;
            lock (_stopwatches)
            {
                Stopwatch sw = new();
                _stopwatches.Push(sw);
                sw.Start();
                count = _stopwatches.Count;
            }
            Debug($"Stopwatch {count} started");
        }

        public void StopwatchStop()
        {
            int count = 0;
            TimeSpan elapsed = TimeSpan.Zero;
            lock (_stopwatches)
            {
                count = _stopwatches.Count;
                Stopwatch sw = _stopwatches.Pop();
                sw.Stop();
                elapsed = sw.Elapsed;
            }
            Debug($"Stopwatch {count} stopped, time {elapsed}");
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

        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public virtual void Log(LogLevel level, string message)
        {
            lock (TextWriters)
            {
                foreach (TextWriter writer in TextWriters)
                {
                    writer.WriteLine($"{(DisplayLineNumbers ? ++_lineCounter + "> " : "")}{(DisplayLevel ? $"[{level}] " : "")}{(DisplayThread ? $"[Thread {Thread.CurrentThread.ManagedThreadId}] " : "")}{message}");
                }
            }
        }

        public void Warn(string message)
        {
            Log(LogLevel.Warn, message);
        }
    }
}
