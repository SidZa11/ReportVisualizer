using System;
using System.IO;

namespace ReportVisualizer.Utilities
{
    /// <summary>
    /// Handles application logging
    /// </summary>
    public static class Logger
    {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "application.log");
        private static readonly object _lock = new object();

        static Logger()
        {
            // Ensure log directory exists
            string logDirectory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        /// <summary>
        /// Logs an information message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void Log(string message)
        {
            WriteToLog("INFO", message);
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void LogWarning(string message)
        {
            WriteToLog("WARNING", message);
        }

        /// <summary>
        /// Logs an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void LogError(string message)
        {
            WriteToLog("ERROR", message);
        }

        /// <summary>
        /// Writes a message to the log file
        /// </summary>
        /// <param name="level">Log level</param>
        /// <param name="message">Message to log</param>
        private static void WriteToLog(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                    
                    // Also output to console in development environment
                    Console.WriteLine(logEntry);
                }
            }
            catch (Exception ex)
            {
                // Fallback to console if file logging fails
                Console.WriteLine($"Error writing to log file: {ex.Message}");
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}");
            }
        }
    }
}