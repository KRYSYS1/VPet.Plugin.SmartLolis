using System;
using System.Collections.Generic;
using System.Linq;

namespace VPet.Plugin.SmartLolis
{
    public static class SmartLolisLog
    {
        private static readonly object Sync = new();
        private static readonly List<string> Entries = new();
        private const int MaxEntries = 300;

        public static event Action<string> LogUpdated;

        public static void Info(string message)
        {
            Add("INFO", message);
        }

        public static void Error(string message, Exception ex = null)
        {
            string fullMessage = ex == null
                ? message
                : $"{message}{Environment.NewLine}{ex.GetType().Name}: {ex.Message}";
            Add("ERROR", fullMessage);
        }

        public static string GetText()
        {
            lock (Sync)
            {
                return string.Join(Environment.NewLine, Entries);
            }
        }

        public static void Clear()
        {
            lock (Sync)
            {
                Entries.Clear();
            }

            LogUpdated?.Invoke(string.Empty);
        }

        private static void Add(string level, string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {level} {message}";
            string text;

            lock (Sync)
            {
                Entries.Add(line);
                if (Entries.Count > MaxEntries)
                    Entries.RemoveRange(0, Entries.Count - MaxEntries);

                text = string.Join(Environment.NewLine, Entries.ToList());
            }

            LogUpdated?.Invoke(text);
        }
    }
}
