using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DARP.Services
{
    public class LoggerBase : ILogger
    {
        public virtual IEnumerable<TextWriter> TextWriters => new TextWriter[] { Console.Out };

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void Fatal(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void Log(LogLevel level, string message)
        {
            foreach(TextWriter writer in TextWriters)
            {
                writer.WriteLine($"{DateTime.Now.ToString("yyyyMMddHHmmss")} {level} {message}");
            }
        }

        public void Warn(string message)
        {
            Log(LogLevel.Warn, message);
        }
    }
}
