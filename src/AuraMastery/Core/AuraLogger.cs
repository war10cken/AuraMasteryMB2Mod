using System;
using System.IO;
using TaleWorlds.Library;

namespace AuraMastery.Core
{
    public static class AuraLogger
    {
        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "AuraMastery",
            "aura_debug.log"
        );

        public static void Log(string message)
        {
            try
            {
                string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

                // В игровой чат
                InformationManager.DisplayMessage(new InformationMessage($"[AuraMastery] {message}"));

                // В файл
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.AppendAllText(LogPath, line + Environment.NewLine);

                // В trace-лог игры
                System.Diagnostics.Trace.WriteLine($"[AuraMastery] {message}");
            }
            catch
            {
                // Игнорируем, чтобы логирование не роняло мод
            }
        }
    }
}