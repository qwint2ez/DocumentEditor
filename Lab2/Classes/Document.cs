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
        public UserRole AccessRole { get; set; }
        private readonly Dictionary<User, int> _historyViewCount = new Dictionary<User, int>(); // Отслеживание просмотров истории

        public Document(DocumentType type, UserRole accessRole = UserRole.Viewer)
        {
            _fragments = new List<ITextFragment>();
            FilePath = string.Empty;
            Type = type;
            AccessRole = accessRole;
        }

        public void AppendText(string text)
        {
            if (!HasEditPermission())
            {
                throw new UnauthorizedAccessException($"Недостаточно прав для редактирования документа (требуется {AccessRole}).");
            }
            var beforeContent = GetOriginalText();
            _history.AddEntry("APPEND", beforeContent);
            var newFragments = TextParser.Parse(text, Type);
            _fragments.AddRange(newFragments);
            Notify($"!!! Документ обновлён: текст добавлен. !!!");
        }

        public void AppendTextNoNotify(string text)
        {
            if (!HasEditPermission())
            {
                throw new UnauthorizedAccessException($"Недостаточно прав для редактирования документа (требуется {AccessRole}).");
            }
            var newFragments = TextParser.Parse(text, Type);
            _fragments.AddRange(newFragments);
        }

        public void InsertText(int charPosition, string text)
        {
            if (!HasEditPermission())
            {
                throw new UnauthorizedAccessException($"Недостаточно прав для редактирования документа (требуется {AccessRole}).");
            }
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
                        Notify($"!!! Документ обновлён: текст вставлен. !!!");
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
                            string innerText = decorator._fragment.GetOriginalText();
                            string leftInner = innerText.Substring(0, splitPos);
                            string rightInner = innerText.Substring(splitPos);

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
                                leftFragment = new PlainTextFragment(leftInner);
                                rightFragment = new PlainTextFragment(rightInner);
                            }
                        }
                        else
                        {
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

                        _fragments[i] = leftFragment;
                        _fragments.InsertRange(i + 1, newFragments);
                        if (!string.IsNullOrEmpty(rightFragment.GetOriginalText()))
                        {
                            _fragments.Insert(i + 1 + newFragments.Count, rightFragment);
                        }

                        _history.AddEntry("INSERT", beforeContent);
                        Notify($"!!! Документ обновлён: текст вставлен. !!!");
                        return;
                    }
                    else if (charPosition == currentPos + fragmentLength)
                    {
                        _fragments.InsertRange(i + 1, newFragments);
                        _history.AddEntry("INSERT", beforeContent);
                        Notify($"!!! Документ обновлён: текст вставлен. !!!");
                        return;
                    }
                    currentPos += fragmentLength;
                }
            }
            _history.AddEntry("INSERT", beforeContent);
            Notify($"!!! Документ обновлён: текст вставлен. !!!");
        }

        public void DeleteText(int fragmentStart, int fragmentCount)
        {
            if (!HasEditPermission())
            {
                throw new UnauthorizedAccessException($"Недостаточно прав для редактирования документа (требуется {AccessRole}).");
            }
            if (fragmentStart < 0 || fragmentStart >= _fragments.Count || fragmentCount < 0 || fragmentStart + fragmentCount > _fragments.Count)
            {
                throw new ArgumentOutOfRangeException("Неверный старт или количество.");
            }
            var beforeContent = GetOriginalText();
            _fragments.RemoveRange(fragmentStart, fragmentCount);
            Notify($"!!! Документ обновлён: текст удалён. !!!");
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
                if (!_observers.Contains(admin))
                {
                    admin.Update($"[ADMIN OVERRIDE] {fullMessage}");
                }
            }
        }

        public IEnumerable<DocumentSnapshot> GetHistory()
        {
            if (Session.CurrentUser != null)
            {
                // Увеличиваем счётчик просмотров для текущего пользователя
                if (!_historyViewCount.ContainsKey(Session.CurrentUser))
                {
                    _historyViewCount[Session.CurrentUser] = 0;
                }
                _historyViewCount[Session.CurrentUser]++;

                // Если это второй просмотр, очищаем историю
                if (_historyViewCount[Session.CurrentUser] == 2)
                {
                    _history.ClearHistory();
                    _historyViewCount[Session.CurrentUser] = 0; // Сбрасываем счётчик
                    Console.WriteLine("[INFO] История изменений очищена.");
                }
            }

            return _history.GetHistory();
        }

        private bool HasEditPermission()
        {
            if (Session.CurrentUser == null)
            {
                return false;
            }

            int userRoleValue = (int)Session.CurrentUser.Role;
            int requiredRoleValue = (int)AccessRole;
            return userRoleValue >= requiredRoleValue;
        }
    }

    public class DocumentData
    {
        [XmlElement("Type")]
        [JsonProperty("Type")]
        public DocumentType Type { get; set; }

        [XmlElement("Content")]
        [JsonProperty("Content")]
        public string Content { get; set; }

        [XmlElement("AccessRole")]
        [JsonProperty("AccessRole")]
        public UserRole AccessRole { get; set; }
    }

    public static class DocumentManager
    {
        private static IStorageStrategy _storageStrategy = new LocalFileStrategy();

        public static Document CreateNewDocument(DocumentType type)
        {
            var accessRole = Session.CurrentUser?.Role ?? UserRole.Viewer;
            return new Document(type, accessRole);
        }

        public static List<ITextFragment> Clipboard { get; set; } = new List<ITextFragment>();

        public static void DeleteDocument(string path)
        {
            if (_storageStrategy is LocalFileStrategy && File.Exists(path))
            {
                var document = OpenDocument(path).Result;
                document.Notify($"!!! Документ удалён: {path} !!!");
                File.Delete(path);
                // Удаляем файл истории, если он существует
                string historyPath = GetHistoryFilePath(path);
                if (File.Exists(historyPath))
                {
                    File.Delete(historyPath);
                }
            }
            else
            {
                throw new NotSupportedException("Удаление поддерживается только для локальных файлов в этой реализации.");
            }
        }

        public static void SetStorageStrategy(IStorageStrategy strategy)
        {
            _storageStrategy = strategy;
        }

        public static async Task<Document> OpenDocument(string fileName)
        {
            var data = await _storageStrategy.LoadDocument(fileName);
            var doc = new Document(data.Type, data.AccessRole);
            Console.WriteLine($"[DEBUG] Загруженный AccessRole: {doc.AccessRole}");
            if (Session.CurrentUser == null || (int)Session.CurrentUser.Role < (int)doc.AccessRole)
            {
                throw new UnauthorizedAccessException($"Недостаточно прав для открытия документа (требуется {doc.AccessRole}).");
            }
            doc.AppendTextNoNotify(data.Content);
            doc.FilePath = fileName;

            // Загружаем историю
            string historyPath = GetHistoryFilePath(fileName);
            await doc._history.LoadHistoryAsync(historyPath);

            return doc;
        }

        public static async Task SaveDocument(Document document, string fileName)
        {
            var data = new DocumentData { Type = document.Type, Content = document.GetOriginalText(), AccessRole = document.AccessRole };
            await _storageStrategy.SaveDocument(data, fileName);
            document.FilePath = fileName;

            // Сохраняем историю в отдельный файл
            string historyPath = GetHistoryFilePath(fileName);
            await document._history.SaveHistoryAsync(historyPath);

            document.Notify($"!!! Документ сохранён в: {fileName} !!!");
        }

        public static void ChangeAccessRole(Document document, UserRole newRole)
        {
            if (Session.CurrentUser?.Role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("Только администратор может изменять права доступа.");
            }
            document.AccessRole = newRole;
            document.Notify($"Права доступа изменены на {newRole} пользователем {Session.CurrentUser.Name}");
        }

        private static string GetHistoryFilePath(string fileName)
        {
            return Path.ChangeExtension(fileName, ".history.json");
        }
    }
}