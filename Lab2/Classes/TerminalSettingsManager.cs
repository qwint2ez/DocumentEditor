using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab2.Classes
{
    public sealed class TerminalSettingsManager
    {
        private static readonly Lazy<TerminalSettingsManager> _instance =
            new Lazy<TerminalSettingsManager>(() => new TerminalSettingsManager());

        public static TerminalSettingsManager Instance => _instance.Value;

        private readonly string SettingsPath;

        private TerminalSettingsManager()
        {
            SettingsPath = GetWindowsTerminalSettingsPath();
            if (!File.Exists(SettingsPath))
            {
                throw new FileNotFoundException("Windows Terminal settings.json not found. Ensure Windows Terminal is installed.");
            }
        }

        private static string GetWindowsTerminalSettingsPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string terminalPath = Path.Combine(localAppData, @"Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json");

            if (!File.Exists(terminalPath))
            {
                var packagesDir = Path.Combine(localAppData, "Packages");
                var terminalFolders = Directory.GetDirectories(packagesDir, "Microsoft.WindowsTerminal_*");
                if (terminalFolders.Length > 0)
                {
                    terminalPath = Path.Combine(terminalFolders[0], "LocalState", "settings.json");
                }
            }
            return terminalPath;
        }

        public void UpdateSettings(int fontSize, string colorScheme)
        {
            try
            {
                var json = JObject.Parse(File.ReadAllText(SettingsPath));

                if (json["profiles"] == null)
                    json["profiles"] = new JObject();
                if (json["profiles"]["defaults"] == null)
                    json["profiles"]["defaults"] = new JObject();
                if (json["profiles"]["defaults"]["font"] == null)
                    json["profiles"]["defaults"]["font"] = new JObject();

                json["profiles"]["defaults"]["font"]["size"] = fontSize;
                json["profiles"]["defaults"]["colorScheme"] = colorScheme;

                File.WriteAllText(SettingsPath, json.ToString());
                Console.WriteLine("Настройки терминала обновлены!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        public void ShowTerminalSettingsMenu()
        {
            Console.WriteLine("\nНастройки терминала:");
            Console.WriteLine("1. Изменить цветовую схему");
            Console.WriteLine("2. Изменить размер шрифта");
            Console.Write("Выберите действие: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.WriteLine("Доступные схемы:");
                    Console.WriteLine("1. CGA (Тёмная)");
                    Console.WriteLine("2. One Half Light (Светлая)");
                    Console.Write("Выберите схему: ");

                    var scheme = Console.ReadLine() == "1" ? "CGA" : "One Half Light";
                    UpdateSettings(GetCurrentFontSize(), scheme);
                    break;

                case "2":
                    Console.Write("Введите размер шрифта: ");
                    if (int.TryParse(Console.ReadLine(), out int size))
                    {
                        UpdateSettings(size, GetCurrentColorScheme());
                    }
                    break;
            }
        }

        public int GetCurrentFontSize()
        {
            try
            {
                var json = JObject.Parse(File.ReadAllText(SettingsPath));
                return json["profiles"]?["defaults"]?["font"]?["size"]?.Value<int>() ?? 12;
            }
            catch
            {
                return 12;
            }
        }

        private string GetCurrentColorScheme()
        {
            try
            {
                var json = JObject.Parse(File.ReadAllText(SettingsPath));
                return json["profiles"]?["defaults"]?["colorScheme"]?.Value<string>() ?? "CGA";
            }
            catch
            {
                return "CGA";
            }
        }
    }
}
