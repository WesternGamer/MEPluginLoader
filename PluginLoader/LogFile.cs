﻿using System;
using System.IO;
using VRage.Logging;

namespace MEPluginLoader
{
    public static class LogFile
    {
        private const string fileName = "loader.log";
        private static StreamWriter writer;

        public static void Init(string mainPath)
        {
            string file = Path.Combine(mainPath, fileName);
            writer = File.CreateText(file);
        }

        public static void WriteLine(string text, bool gameLog = true)
        {
            writer?.WriteLine($"{DateTime.UtcNow:O} {text}");
            if (gameLog)
            {
                MyLog.Default.WriteLine($"[PluginLoader] {text}");
            }

            writer?.Flush();
        }

        public static void WriteTrace(string text, bool gameLog = true)
        {
#if DEBUG
            writer?.WriteLine($"{DateTime.UtcNow:O} {text}");
            if (gameLog)
            {
                MyLog.Default.WriteLine($"[PluginLoader] {text}");
            }

            writer?.Flush();
#endif
        }

        public static void Dispose()
        {
            if (writer == null)
            {
                return;
            }

            WriteLine("Log closed.");

            writer.Flush();
            writer.Close();
            writer = null;
        }
    }
}