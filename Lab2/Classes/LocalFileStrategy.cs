using Lab2.Documentn;
using Lab2.Enums;
using Lab2.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lab2.Classes
{
    public class LocalFileStrategy : IStorageStrategy
    {
        public async Task SaveDocument(DocumentData data, string fileName)
        {
            string format = Path.GetExtension(fileName).ToLower().TrimStart('.');
            switch (format)
            {
                case "txt":
                    // Сохраняем AccessRole в первой строке файла
                    string txtContent = $"[AccessRole:{data.AccessRole}]\n{data.Content}";
                    await File.WriteAllTextAsync(fileName, txtContent);
                    Console.WriteLine($"[DEBUG] Сохранённый AccessRole для .txt: {data.AccessRole}");
                    break;
                case "json":
                    string json = JsonConvert.SerializeObject(data);
                    await File.WriteAllTextAsync(fileName, json);
                    break;
                case "xml":
                    var serializer = new XmlSerializer(typeof(DocumentData));
                    using (var writer = new StreamWriter(fileName))
                    {
                        serializer.Serialize(writer, data);
                    }
                    break;
                default:
                    throw new ArgumentException("Неподдерживаемый формат файла");
            }
        }

        public async Task<DocumentData> LoadDocument(string fileName)
        {
            if (!File.Exists(fileName))
                throw new FileNotFoundException("Файл не найден");

            string format = Path.GetExtension(fileName).ToLower().TrimStart('.');
            switch (format)
            {
                case "txt":
                    string[] lines = await File.ReadAllLinesAsync(fileName);
                    if (lines.Length == 0)
                    {
                        return new DocumentData { Type = DocumentType.PlainText, Content = "", AccessRole = UserRole.Viewer };
                    }

                    // Первая строка должна содержать AccessRole
                    string firstLine = lines[0].Trim();
                    UserRole accessRole = UserRole.Viewer; // По умолчанию
                    if (firstLine.StartsWith("[AccessRole:") && firstLine.EndsWith("]"))
                    {
                        // Извлекаем роль между "[AccessRole:" и "]"
                        int startIndex = "[AccessRole:".Length;
                        int endIndex = firstLine.LastIndexOf(']');
                        if (endIndex > startIndex)
                        {
                            string roleStr = firstLine.Substring(startIndex, endIndex - startIndex).Trim();
                            // Удаляем возможные лишние двоеточия
                            roleStr = roleStr.TrimStart(':').Trim();
                            if (Enum.TryParse<UserRole>(roleStr, true, out var parsedRole))
                            {
                                accessRole = parsedRole;
                            }
                            else
                            {
                                Console.WriteLine($"[DEBUG] Не удалось распарсить AccessRole из строки: {roleStr}. Установлен Viewer по умолчанию.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[DEBUG] Неверный формат AccessRole: {firstLine}. Установлен Viewer по умолчанию.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] Первая строка не содержит AccessRole: {firstLine}. Установлен Viewer по умолчанию.");
                    }

                    // Остальные строки — содержимое
                    string txtContent = string.Join("\n", lines.Skip(1));
                    return new DocumentData { Type = DocumentType.PlainText, Content = txtContent, AccessRole = accessRole };
                case "json":
                    string json = await File.ReadAllTextAsync(fileName);
                    return JsonConvert.DeserializeObject<DocumentData>(json);
                case "xml":
                    using (var reader = new StreamReader(fileName))
                    {
                        var serializer = new XmlSerializer(typeof(DocumentData));
                        return (DocumentData)serializer.Deserialize(reader);
                    }
                default:
                    throw new ArgumentException("Неподдерживаемый формат файла");
            }
        }
    }
}