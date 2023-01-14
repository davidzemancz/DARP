using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public interface ILoggerService
    {
        public IList<TextWriter> TextWriters { get; }
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Fatal(string message);
        void Log(LogLevel level, string message);
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4,
    }
}
