using Lab2.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Lab2.Documentn;
using Lab2.Enums;

namespace Lab2.Classes
{
    public class TxtFileSaver
    {
        public void WriteToTxt(string path, string content)
        {
            File.WriteAllText(path, content);
        }
    }

    public class JsonFileSaver
    {
        public void SaveAsJson(string path, DocumentData data)
        {
            string json = JsonConvert.SerializeObject(data);
            File.WriteAllText(path, json);
        }
    }

    public class XmlFileSaver
    {
        public void SerializeToXml(string path, DocumentData data)
        {
            var serializer = new XmlSerializer(typeof(DocumentData));
            using (var writer = new StreamWriter(path))
            {
                serializer.Serialize(writer, data);
            }
        }
    }

    public class TxtFileLoader
    {
        public string ReadFromTxt(string path)
        {
            return File.ReadAllText(path);
        }
    }

    public class JsonFileLoader
    {
        public DocumentData LoadFromJson(string path)
        {
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<DocumentData>(json);
        }
    }

    public class XmlFileLoader
    {
        public DocumentData DeserializeFromXml(string path)
        {
            using (var reader = new StreamReader(path))
            {
                var serializer = new XmlSerializer(typeof(DocumentData));
                return (DocumentData)serializer.Deserialize(reader);
            }
        }
    }

    public class TxtSaverAdapter : IDocumentSaver
    {
        private readonly TxtFileSaver _txtFileSaver;

        public TxtSaverAdapter(TxtFileSaver txtFileSaver)
        {
            _txtFileSaver = txtFileSaver;
        }

        public void Save(string path, Document document)
        {
            _txtFileSaver.WriteToTxt(path, document.GetOriginalText());
        }
    }

    public class JsonSaverAdapter : IDocumentSaver
    {
        private readonly JsonFileSaver _jsonFileSaver;

        public JsonSaverAdapter(JsonFileSaver jsonFileSaver)
        {
            _jsonFileSaver = jsonFileSaver;
        }

        public void Save(string path, Document document)
        {
            var data = new DocumentData
            {
                Type = document.Type,
                Content = document.GetOriginalText()
            };
            _jsonFileSaver.SaveAsJson(path, data);
        }
    }

    public class XmlSaverAdapter : IDocumentSaver
    {
        private readonly XmlFileSaver _xmlFileSaver;

        public XmlSaverAdapter(XmlFileSaver xmlFileSaver)
        {
            _xmlFileSaver = xmlFileSaver;
        }

        public void Save(string path, Document document)
        {
            var data = new DocumentData
            {
                Type = document.Type,
                Content = document.GetOriginalText()
            };
            _xmlFileSaver.SerializeToXml(path, data);
        }
    }

    public class TxtLoaderAdapter : IDocumentLoader
    {
        private readonly TxtFileLoader _txtFileLoader;

        public TxtLoaderAdapter(TxtFileLoader txtFileLoader)
        {
            _txtFileLoader = txtFileLoader;
        }

        public Document Load(string path)
        {
            string content = _txtFileLoader.ReadFromTxt(path);
            Document doc = new Document(DocumentType.PlainText);
            doc.AppendText(content);
            doc.FilePath = path;
            return doc;
        }
    }

    public class JsonLoaderAdapter : IDocumentLoader
    {
        private readonly JsonFileLoader _jsonFileLoader;

        public JsonLoaderAdapter(JsonFileLoader jsonFileLoader)
        {
            _jsonFileLoader = jsonFileLoader;
        }

        public Document Load(string path)
        {
            var data = _jsonFileLoader.LoadFromJson(path);
            DocumentType type = data.Type != 0 ? data.Type : DocumentType.PlainText;
            Document doc = new Document(type);
            doc.AppendText(data.Content);
            doc.FilePath = path;
            return doc;
        }
    }

    public class XmlLoaderAdapter : IDocumentLoader
    {
        private readonly XmlFileLoader _xmlFileLoader;

        public XmlLoaderAdapter(XmlFileLoader xmlFileLoader)
        {
            _xmlFileLoader = xmlFileLoader;
        }

        public Document Load(string path)
        {
            var data = _xmlFileLoader.DeserializeFromXml(path);
            DocumentType type = data.Type != 0 ? data.Type : DocumentType.PlainText;
            Document doc = new Document(type);
            doc.AppendText(data.Content);
            doc.FilePath = path;
            return doc;
        }
    }

    public static class DocumentFormatFactory
    {
        public static IDocumentSaver GetSaver(string format)
        {
            switch (format.ToLower())
            {
                case "txt":
                    return new TxtSaverAdapter(new TxtFileSaver());
                case "json":
                    return new JsonSaverAdapter(new JsonFileSaver());
                case "xml":
                    return new XmlSaverAdapter(new XmlFileSaver());
                default:
                    throw new ArgumentException("Unsupported format");
            }
        }

        public static IDocumentLoader GetLoader(string format)
        {
            switch (format.ToLower())
            {
                case "txt":
                    return new TxtLoaderAdapter(new TxtFileLoader());
                case "json":
                    return new JsonLoaderAdapter(new JsonFileLoader());
                case "xml":
                    return new XmlLoaderAdapter(new XmlFileLoader());
                default:
                    throw new ArgumentException("Unsupported format");
            }
        }
    }

}
