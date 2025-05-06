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
                    await File.WriteAllTextAsync(fileName, data.Content);
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
                    string txtContent = await File.ReadAllTextAsync(fileName);
                    return new DocumentData { Type = DocumentType.PlainText, Content = txtContent, AccessRole = UserRole.Viewer }; // Для txt по умолчанию Viewer
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