using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LAB2.Protocol
{
    public enum LogLevel
    {
        SELECTION,
        TRANSFORMATION,
        SAVING
    }

    public sealed class Logger
    {
        private static Lazy<Logger> _instance = new Lazy<Logger>(() => new Logger());
        private readonly string _logFilePath;

        private Logger()
        {
            _logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "program_log.txt");
        }

        public static Logger Instance => _instance.Value;

        public void Log(LogLevel level, string message)
        {
            string logEntry = TransformLogEntry(level, message);
            SaveLogEntry(logEntry);
        }

        private string TransformLogEntry(LogLevel level, string message)
        {
            return $"{DateTime.Now:dd-MM-yyyyy HH:mm:ss} [{level}] {message}";
        }

        private void SaveLogEntry(string logEntry)
        {
            try
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception e)
            {
                return;
            }
        }
    }
}
