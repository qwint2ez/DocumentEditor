using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lab2.Interfaces;
using Lab2.Classes;
using Lab2.Enums;
using Newtonsoft.Json;
using System.Xml.Serialization;
using System.Reflection.Metadata;

namespace Lab2.Documentn
{
    public class Document
    {
        public List<ITextFragment> _fragments;
        public string FilePath { get; set; }
        public DocumentType Type { get; set; }
        public readonly DocumentHistory _history = new DocumentHistory();
        public Document(DocumentType type)
        {
            _fragments = new List<ITextFragment>();
            FilePath = string.Empty;
            Type = type;
        }
        public void AppendText(string text)
        {
            var beforeContent = GetOriginalText();
            _history.AddEntry("APPEND", beforeContent);
            var newFragments = TextParser.Parse(text, Type);
            _fragments.AddRange(newFragments);
            Notify($"!!! Document updated: text appended. !!!");
        }
        public void AppendTextNoNotify(string text)
        {
            var newFragments = TextParser.Parse(text, Type);
            _fragments.AddRange(newFragments);
        }
        public void InsertText(int charPosition, string text)
        {
            if (charPosition < 0 || charPosition > GetTextWithoutMarkersLength())
            {
                throw new ArgumentOutOfRangeException("Позиция символа вне допустимого диапазона.");
            }

            var beforeContent = GetOriginalText();
            var newFragments = TextParser.Parse(text, Type);

            if (charPosition == 0)
            {
                _fragments.InsertRange(0, newFragments);
            }
            else if (charPosition == GetTextWithoutMarkersLength())
            {
                _fragments.AddRange(newFragments);
            }
            else
            {
                int currentPos = 0;
                for (int i = 0; i < _fragments.Count; i++)
                {
                    string fragmentTextWithoutMarkers = RemoveMarkers(_fragments[i].GetOriginalText());
                    int fragmentLength = fragmentTextWithoutMarkers.Length;

                    if (charPosition == currentPos)
                    {
                        _fragments.InsertRange(i, newFragments);
                        _history.AddEntry("INSERT", beforeContent);
                        Notify($"!!! Документ обновлен: Текст вставлен. !!!");
                        return;
                    }
                    else if (charPosition > currentPos && charPosition < currentPos + fragmentLength)
                    {
                        if (_fragments[i] is NewlineFragment)
                        {
                            throw new InvalidOperationException("Нельзя вставлять текст внутрь перевода строки.");
                        }
                        int splitPos = charPosition - currentPos;
                        ITextFragment leftFragment;
                        ITextFragment rightFragment;

                        if (_fragments[i] is TextDecorator decorator)
                        {
                            // Берем внутренний текст без маркеров
                            string innerText = decorator._fragment.GetOriginalText();
                            string leftInner = innerText.Substring(0, splitPos);
                            string rightInner = innerText.Substring(splitPos);

                            // Создаем новые фрагменты с тем же декоратором
                            if (decorator is BoldDecorator)
                            {
                                leftFragment = new BoldDecorator(new PlainTextFragment(leftInner));
                                rightFragment = new BoldDecorator(new PlainTextFragment(rightInner));
                            }
                            else if (decorator is ItalicDecorator)
                            {
                                leftFragment = new ItalicDecorator(new PlainTextFragment(leftInner));
                                rightFragment = new ItalicDecorator(new PlainTextFragment(rightInner));
                            }
                            else if (decorator is UnderlineDecorator)
                            {
                                leftFragment = new UnderlineDecorator(new PlainTextFragment(leftInner));
                                rightFragment = new UnderlineDecorator(new PlainTextFragment(rightInner));
                            }
                            else
                            {
                                // Если вдруг появятся другие декораторы, кидаем ошибку или обрабатываем как plain
                                leftFragment = new PlainTextFragment(leftInner);
                                rightFragment = new PlainTextFragment(rightInner);
                            }
                        }
                        else
                        {
                            // Для обычного текста без декораторов
                            string originalText = _fragments[i].GetOriginalText();
                            string textWithoutMarkers = RemoveMarkers(originalText);
                            int plainTextPos = 0;
                            int splitIndex = 0;
                            for (int j = 0; j < originalText.Length && plainTextPos < splitPos; j++)
                            {
                                if (j + 1 < originalText.Length && (originalText.Substring(j, 2) == "**" || originalText.Substring(j, 2) == "__"))
                                {
                                    j++;
                                }
                                else if (originalText[j] == '*')
                                {
                                    // Пропускаем одиночный *
                                }
                                else
                                {
                                    plainTextPos++;
                                }
                                splitIndex = j + 1;
                            }
                            string leftText = originalText.Substring(0, splitIndex);
                            string rightText = originalText.Substring(splitIndex);
                            leftFragment = new PlainTextFragment(leftText);
                            rightFragment = new PlainTextFragment(rightText);
                        }

                        // Заменяем текущий фрагмент на левую часть
                        _fragments[i] = leftFragment;
                        // Вставляем новый текст
                        _fragments.InsertRange(i + 1, newFragments);
                        // Добавляем правую часть, если она не пустая
                        if (!string.IsNullOrEmpty(rightFragment.GetOriginalText()))
                        {
                            _fragments.Insert(i + 1 + newFragments.Count, rightFragment);
                        }

                        _history.AddEntry("INSERT", beforeContent);
                        Notify($"!!! Документ обновлен: Текст вставлен. !!!");
                        return;
                    }
                    else if (charPosition == currentPos + fragmentLength)
                    {
                        _fragments.InsertRange(i + 1, newFragments);
                        _history.AddEntry("INSERT", beforeContent);
                        Notify($"!!! Документ обновлен: Текст вставлен. !!!");
                        return;
                    }
                    currentPos += fragmentLength;
                }
            }
            _history.AddEntry("INSERT", beforeContent);
            Notify($"!!! Документ обновлен: Текст вставлен. !!!");
        }

        public void DeleteText(int fragmentStart, int fragmentCount)
        {
            if (fragmentStart < 0 || fragmentStart >= _fragments.Count || fragmentCount < 0 || fragmentStart + fragmentCount > _fragments.Count)
            {
                throw new ArgumentOutOfRangeException("Invalid start or count.");
            }
            var beforeContent = GetOriginalText();
            _fragments.RemoveRange(fragmentStart, fragmentCount);
            Notify($"!!! Document updated: Some text deleted. !!!");
            _history.AddEntry("DELETE", beforeContent);
        }

        public List<int> SearchWord(string word)
        {
            string textWithoutMarkers = GetTextWithoutMarkers();
            List<int> positions = new List<int>();
            int index = textWithoutMarkers.IndexOf(word, 0);
            while (index != -1)
            {
                positions.Add(index);
                index = textWithoutMarkers.IndexOf(word, index + 1);
            }
            return positions;
        }

        public string GetDisplayText()
        {
            if (Type == DocumentType.PlainText)
            {
                return string.Join("", _fragments.Select(f => f.GetOriginalText()));
            }
            return string.Join("", _fragments.Select(f => f.GetText()));
        }

        public string GetOriginalText()
        {
            if (Type == DocumentType.RichText)
            {
                return string.Join("", _fragments.Select(f => f.GetText()));
            }
            return string.Join("", _fragments.Select(f => f.GetOriginalText()));
        }

        private string GetTextWithoutMarkers()
        {
            string originalText = GetOriginalText();
            return RemoveMarkers(originalText);
        }

        private int GetTextWithoutMarkersLength()
        {
            return GetTextWithoutMarkers().Length;
        }

        private string RemoveMarkers(string text)
        {
            // Удаляем маркеры **, __, * из текста
            return text.Replace("**", "").Replace("__", "").Replace("*", "");
        }

        private List<IObserver> _observers = new List<IObserver>();

        public void Subscribe(IObserver observer) => _observers.Add(observer);
        public void Unsubscribe(IObserver observer) => _observers.Remove(observer);

        public void Notify(string message)
        {
            var fullMessage = $"{message}\nFile: {FilePath}";

            foreach (var observer in _observers)
            {
                observer.Update(fullMessage);
            }

            foreach (var admin in UserManager.GetAdmins())
            {
                // Чтобы избежать дублей, если админ уже подписан
                if (!_observers.Contains(admin))
                {
                    admin.Update($"[ADMIN OVERRIDE] {fullMessage}");
                }
            }
        }
        public IEnumerable<DocumentSnapshot> GetHistory()
        {
            return _history.GetHistory();
        }
    }
    public class DocumentData
    {
        public DocumentType Type { get; set; }
        public string Content { get; set; }
    }

    public static class DocumentManager
    {
        public static Document CreateNewDocument(DocumentType type)
        {
            return new Document(type);
        }
        public static List<ITextFragment> Clipboard { get; set; } = new List<ITextFragment>();

        public static void DeleteDocument(string path)
        {
            // Note: Deletion might need strategy-specific logic, but for simplicity:
            if (_storageStrategy is LocalFileStrategy && File.Exists(path))
            {
                var document = OpenDocument(path).Result; // Use await in async context
                document.Notify($"!!! Document deleted: {path} !!!");
                File.Delete(path);
            }
            else
            {
                throw new NotSupportedException("Deletion only supported for local files in this implementation");
            }
        }

        private static IStorageStrategy _storageStrategy = new LocalFileStrategy();
        public static void SetStorageStrategy(IStorageStrategy strategy)
        {
            _storageStrategy = strategy;
        }
        public static async Task<Document> OpenDocument(string fileName)
        {
            var data = await _storageStrategy.LoadDocument(fileName);
            var doc = new Document(data.Type);
            doc.AppendTextNoNotify(data.Content);
            doc.FilePath = fileName;
            return doc;
        }

        public static async Task SaveDocument(Document document, string fileName)
        {
            var data = new DocumentData { Type = document.Type, Content = document.GetOriginalText() };
            await _storageStrategy.SaveDocument(data, fileName);
            document.FilePath = fileName;
            document.Notify($"!!! Document saved to: {fileName} !!!");
        }
    }
}
