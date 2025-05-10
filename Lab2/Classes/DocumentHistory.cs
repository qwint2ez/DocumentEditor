using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Lab2.Documentn
{
    public class DocumentHistory
    {
        private readonly List<DocumentSnapshot> _history = new List<DocumentSnapshot>();
        private const int MaxHistoryEntries = 50;

        public void AddEntry(string actionType, string content)
        {
            if (_history.Count >= MaxHistoryEntries)
            {
                _history.RemoveAt(0);
            }

            _history.Add(new DocumentSnapshot(
                DateTime.Now,
                actionType,
                content
            ));
        }

        public IEnumerable<DocumentSnapshot> GetHistory()
        {
            return _history.AsEnumerable().Reverse();
        }

        public void ClearHistory()
        {
            _history.Clear();
        }

        // Новый метод для сохранения истории в файл
        public async Task SaveHistoryAsync(string historyFilePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(_history, Formatting.Indented);
                await File.WriteAllTextAsync(historyFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении истории: {ex.Message}");
            }
        }

        // Новый метод для загрузки истории из файла
        public async Task LoadHistoryAsync(string historyFilePath)
        {
            try
            {
                if (File.Exists(historyFilePath))
                {
                    string json = await File.ReadAllTextAsync(historyFilePath);
                    var loadedHistory = JsonConvert.DeserializeObject<List<DocumentSnapshot>>(json);
                    if (loadedHistory != null)
                    {
                        _history.Clear();
                        _history.AddRange(loadedHistory);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при загрузке истории: {ex.Message}");
                _history.Clear();
            }
        }
    }

    public record DocumentSnapshot(
        DateTime Timestamp,
        string ActionType,
        string Content
    );

    public static class StringExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            return string.IsNullOrEmpty(value)
                ? value
                : value.Length <= maxLength
                    ? value
                    : value[..(maxLength - 3)] + "...";
        }
    }
}