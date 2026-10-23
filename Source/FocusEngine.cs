// Zen Focus - Masaüstü Minimalist Pomodoro ve Odaklanma Sayacı
// Geliştirici: Serkan ŞENTÜRK
// Telif Hakkı © 2026 Serkan ŞENTÜRK. Tüm Hakları Saklıdır.

using System;
using System.IO;

namespace ZenFocus
{
    public enum SessionMode
    {
        Focus,      // 25 min default
        ShortBreak, // 5 min default
        LongBreak   // 15 min default
    }

    public class FocusConfig
    {
        public int FocusMinutes { get; set; }
        public int ShortBreakMinutes { get; set; }
        public int LongBreakMinutes { get; set; }
        public int CompletedToday { get; set; }
        public string LastDateString { get; set; }

        public FocusConfig()
        {
            FocusMinutes = 25;
            ShortBreakMinutes = 5;
            LongBreakMinutes = 15;
            CompletedToday = 0;
            LastDateString = DateTime.Now.ToString("yyyy-MM-dd");
        }

        public static string GetConfigPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "ZenSuite", "ZenFocus");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return Path.Combine(dir, "config.txt");
        }

        public static FocusConfig Load()
        {
            var cfg = new FocusConfig();
            try
            {
                string path = GetConfigPath();
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string k = parts[0].Trim();
                            string v = parts[1].Trim();
                            int val;
                            if (k == "Focus" && int.TryParse(v, out val)) cfg.FocusMinutes = val;
                            if (k == "Short" && int.TryParse(v, out val)) cfg.ShortBreakMinutes = val;
                            if (k == "Long" && int.TryParse(v, out val)) cfg.LongBreakMinutes = val;
                            if (k == "Completed" && int.TryParse(v, out val)) cfg.CompletedToday = val;
                            if (k == "Date") cfg.LastDateString = v;
                        }
                    }
                }
            }
            catch { }

            // Reset count if new day
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (cfg.LastDateString != today)
            {
                cfg.CompletedToday = 0;
                cfg.LastDateString = today;
            }

            return cfg;
        }

        public void Save()
        {
            try
            {
                string path = GetConfigPath();
                string content = string.Format("Focus={0}\nShort={1}\nLong={2}\nCompleted={3}\nDate={4}",
                    FocusMinutes, ShortBreakMinutes, LongBreakMinutes, CompletedToday, LastDateString);
                File.WriteAllText(path, content);
            }
            catch { }
        }
    }
}
