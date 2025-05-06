using System;
using System.IO;
using Newtonsoft.Json;
using System.Xml.Serialization;
using Lab2.Interfaces;
using Lab2.Classes;
using Lab2.Documentn;
using Lab2.Enums;
using System.Text;

class Program
{
    static void DisplayFragments(Document doc)
    {
        if (doc != null && doc._fragments != null && doc._fragments.Count > 0)
        {
            Console.WriteLine("Текущие фрагменты:");
            for (int i = 0; i < doc._fragments.Count; i++)
            {
                string fragmentText = doc._fragments[i].GetText();
                if (fragmentText == "\n")
                {
                    fragmentText = "[Newline]";
                }
                Console.WriteLine($"{i}: {fragmentText}");
            }
        }
        else
        {
            Console.WriteLine("Нет доступных фрагментов или документ не загружен.");
        }
    }

    static private void PressAnyButton()
    {
        Console.WriteLine("Нажмите любую клавишу для продолжения...");
        Console.ReadKey();
    }

    static async Task Main(string[] args)
    {
        Document currentDocument = null;
        UndoRedoManager undoRedoManager = new UndoRedoManager();
        bool running = true, entry = true;

        while (entry)
        {
            Console.WriteLine("Выберите пользователя для входа:");
            if (UserManager.Users.Count == 0)
            {
                Console.WriteLine("Пользователи недоступны. Проверьте UserData.json или конфигурацию.");
                PressAnyButton();
                return;
            }

            for (int i = 0; i < UserManager.Users.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {UserManager.Users[i].Name} ({UserManager.Users[i].Role})");
            }
            Console.Write($"Введите выбор (1-{UserManager.Users.Count}): ");

            if (int.TryParse(Console.ReadLine(), out int userChoice) && userChoice >= 1 && userChoice <= UserManager.Users.Count)
            {
                Session.Login(UserManager.Users[userChoice - 1]);
                Console.WriteLine($"Вход выполнен как: {Session.CurrentUser.Name}");
                PressAnyButton();
                entry = false;
            }
            else
            {
                Console.WriteLine("Неверный выбор. Попробуйте снова.");
                PressAnyButton();
                Console.Clear();
            }
        }

        while (running)
        {
            Console.Clear();
            Console.WriteLine("Система управления документами");
            Console.WriteLine("--------------------------");
            Console.WriteLine($"Текущий пользователь: {Session.CurrentUser.Name} | Роль: {Session.CurrentUser.Role}");
            Console.WriteLine("Текущий документ: " + (currentDocument?.FilePath ?? "Отсутствует"));
            Console.WriteLine("Тип текущего документа: " + (currentDocument != null ? currentDocument.Type.ToString() : "Отсутствует"));
            Console.WriteLine("Содержимое:");
            if (currentDocument != null)
            {
                string formattedText = TextFormatter.FormatText(currentDocument.GetDisplayText(), currentDocument.Type.ToString());
                string[] lines = formattedText.Split('\n');
                foreach (var line in lines)
                {
                    Console.WriteLine("  " + line.TrimEnd('\r'));
                }
            }
            else
            {
                Console.WriteLine("Нет содержимого");
            }
            Console.WriteLine("\nОпции:");
            Console.WriteLine("1. Создать новый документ");
            Console.WriteLine("2. Открыть документ");
            Console.WriteLine("3. Добавить текст");
            Console.WriteLine("4. Вставить текст");
            Console.WriteLine("5. Удалить текст");
            Console.WriteLine("6. Копировать текст");
            Console.WriteLine("7. Вырезать текст");
            Console.WriteLine("8. Вставить текст");
            Console.WriteLine("9. Поиск слова");
            Console.WriteLine("10. Сохранить документ");
            Console.WriteLine("11. Удалить документ");
            Console.WriteLine("12. Отменить");
            Console.WriteLine("13. Повторить");
            Console.WriteLine("14. Выход");
            Console.WriteLine("=============================");
            Console.WriteLine("15. Управление пользователями (только для Admin)");
            Console.WriteLine("16. Сменить пользователя");
            Console.WriteLine("17. Настройки терминала");
            Console.WriteLine("18. Просмотр истории документа");
            Console.WriteLine("19. Изменить права доступа (только для Admin)");
            Console.Write("\nВведите ваш выбор (1-19): ");

            string choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("Вы не можете выполнить это действие с вашей ролью!");
                            PressAnyButton();
                            break;
                        }
                        Console.WriteLine("Выберите тип документа:");
                        Console.WriteLine("1. PlainText");
                        Console.WriteLine("2. Markdown");
                        Console.WriteLine("3. RichText");
                        string typeChoice = Console.ReadLine();
                        DocumentType docType;
                        switch (typeChoice)
                        {
                            case "1":
                                docType = DocumentType.PlainText;
                                break;
                            case "2":
                                docType = DocumentType.Markdown;
                                break;
                            case "3":
                                docType = DocumentType.RichText;
                                break;
                            default:
                                Console.WriteLine("Неверный выбор. Установлен PlainText по умолчанию.");
                                docType = DocumentType.PlainText;
                                PressAnyButton();
                                break;
                        }
                        currentDocument = DocumentManager.CreateNewDocument(docType);
                        currentDocument.Notify("Создан новый документ!");
                        currentDocument.Subscribe(Session.CurrentUser);
                        Console.WriteLine($"Создан новый документ {docType}.");
                        PressAnyButton();
                        break;

                    case "2":
                        Console.WriteLine("Выберите тип хранилища:");
                        Console.WriteLine("1. Локальный файл");
                        Console.WriteLine("2. Supabase Cloud");
                        var storageChoice = Console.ReadLine();

                        try
                        {
                            if (storageChoice == "2")
                            {
                                DocumentManager.SetStorageStrategy(new SupabaseStorageStrategy());
                                Console.Write("Введите имя файла в облаке (например, document.json): ");
                            }
                            else
                            {
                                DocumentManager.SetStorageStrategy(new LocalFileStrategy());
                                Console.Write("Введите путь к локальному файлу (например, doc.txt): ");
                            }

                            string FileName = Console.ReadLine();
                            currentDocument = await DocumentManager.OpenDocument(FileName);
                            Console.WriteLine($"Загружен: {FileName}");

                            currentDocument.Subscribe(Session.CurrentUser);
                            Console.WriteLine($"Тип: {currentDocument.Type}");
                            Console.WriteLine($"Содержимое:\n{currentDocument.GetDisplayText()}");
                        }
                        catch (FileNotFoundException ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                        }
                        catch (Postgrest.Exceptions.PostgrestException ex)
                        {
                            Console.WriteLine($"Ошибка Supabase: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                        }

                        PressAnyButton();
                        break;

                    case "3":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен. Сначала создайте или откройте документ.");
                        }
                        else
                        {
                            Console.WriteLine("Введите текст для добавления (используйте **bold**, __underline__, *italic*, #, ##, ###). Введите 'END' на новой строке для завершения:");
                            StringBuilder inputBuilder = new StringBuilder();
                            string line;
                            while ((line = Console.ReadLine()) != "END")
                            {
                                inputBuilder.AppendLine(line);
                            }
                            string appendText = inputBuilder.ToString().TrimEnd();
                            ICommand appendCommand = new AppendTextCommand(currentDocument, appendText);
                            undoRedoManager.ExecuteCommand(appendCommand);
                        }
                        PressAnyButton();
                        break;

                    case "4":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен. Сначала создайте или откройте документ.");
                        }
                        else
                        {
                            Console.Write("Введите позицию символа для вставки (игнорируя **, __, *): ");
                            int insertPos = int.Parse(Console.ReadLine());
                            Console.Write("Введите текст для вставки (используйте **bold**, __underline__, *italic*): ");
                            string insertText = Console.ReadLine();
                            ICommand insertCommand = new InsertTextCommand(currentDocument, insertPos, insertText);
                            undoRedoManager.ExecuteCommand(insertCommand);
                        }
                        PressAnyButton();
                        break;

                    case "5":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен. Сначала создайте или откройте документ.");
                        }
                        else
                        {
                            DisplayFragments(currentDocument);
                            Console.Write("Введите начальный индекс фрагмента для удаления: ");
                            int deleteStart = int.Parse(Console.ReadLine());
                            Console.Write("Введите количество фрагментов для удаления: ");
                            int deleteCount = int.Parse(Console.ReadLine());
                            ICommand deleteCommand = new DeleteTextCommand(currentDocument, deleteStart, deleteCount);
                            undoRedoManager.ExecuteCommand(deleteCommand);
                        }
                        PressAnyButton();
                        break;

                    case "6":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен. Сначала создайте или откройте документ.");
                        }
                        else
                        {
                            DisplayFragments(currentDocument);
                            Console.Write("Введите начальный индекс фрагмента для копирования: ");
                            int copyStart = int.Parse(Console.ReadLine());
                            Console.Write("Введите количество фрагментов для копирования: ");
                            int copyCount = int.Parse(Console.ReadLine());
                            ICommand copyCommand = new CopyTextCommand(currentDocument, copyStart, copyCount);
                            undoRedoManager.ExecuteCommand(copyCommand);
                            Console.WriteLine("Текст скопирован в буфер обмена.");
                        }
                        PressAnyButton();
                        break;

                    case "7":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен. Сначала создайте или откройте документ.");
                        }
                        else
                        {
                            DisplayFragments(currentDocument);
                            Console.Write("Введите начальный индекс фрагмента для вырезания: ");
                            int cutStart = int.Parse(Console.ReadLine());
                            Console.Write("Введите количество фрагментов для вырезания: ");
                            int cutCount = int.Parse(Console.ReadLine());
                            ICommand cutCommand = new CutTextCommand(currentDocument, cutStart, cutCount);
                            undoRedoManager.ExecuteCommand(cutCommand);
                            Console.WriteLine("Текст вырезан в буфер обмена.");
                        }
                        PressAnyButton();
                        break;

                    case "8":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен. Сначала создайте или откройте документ.");
                        }
                        else
                        {
                            DisplayFragments(currentDocument);
                            Console.Write("Введите позицию фрагмента для вставки: ");
                            int pastePos = int.Parse(Console.ReadLine());
                            ICommand pasteCommand = new PasteTextCommand(currentDocument, pastePos);
                            undoRedoManager.ExecuteCommand(pasteCommand);
                            Console.WriteLine("Текст вставлен из буфера обмена.");
                        }
                        PressAnyButton();
                        break;

                    case "9":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен. Сначала создайте или откройте документ.");
                        }
                        else
                        {
                            Console.Write("Введите слово для поиска (игнорируя **, __, *): ");
                            string searchWord = Console.ReadLine();
                            List<int> positions = currentDocument.SearchWord(searchWord);
                            if (positions.Count > 0)
                            {
                                Console.WriteLine($"Найдено '{searchWord}' в позициях: {string.Join(", ", positions)}");
                            }
                            else
                            {
                                Console.WriteLine($"'{searchWord}' не найдено.");
                            }
                        }
                        PressAnyButton();
                        break;

                    case "10":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("Вы не можете выполнить это действие с вашей ролью!");
                            PressAnyButton();
                            break;
                        }
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен. Сначала создайте или откройте документ.");
                            PressAnyButton();
                            break;
                        }

                        Console.WriteLine("Выберите тип хранилища:");
                        Console.WriteLine("1. Локальный файл");
                        Console.WriteLine("2. Supabase Cloud");
                        var storageChoice1 = Console.ReadLine();

                        try
                        {
                            if (storageChoice1 == "1")
                            {
                                DocumentManager.SetStorageStrategy(new LocalFileStrategy());
                                Console.Write("Введите имя локального файла для сохранения (например, doc.txt): ");
                            }
                            else if (storageChoice1 == "2")
                            {
                                DocumentManager.SetStorageStrategy(new SupabaseStorageStrategy());
                                Console.Write("Введите имя файла в облаке для сохранения (например, document.json): ");
                            }
                            else
                            {
                                throw new ArgumentException("Неверный тип хранилища");
                            }
                            string FileName = Console.ReadLine();
                            await DocumentManager.SaveDocument(currentDocument, FileName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка: {ex.Message}");
                        }

                        PressAnyButton();
                        break;

                    case "11":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("Вы не можете выполнить это действие с вашей ролью!");
                            PressAnyButton();
                            break;
                        }
                        Console.Write("Введите имя файла для удаления: ");
                        string deletePath = Console.ReadLine();
                        if (File.Exists(deletePath))
                        {
                            DocumentManager.DeleteDocument(deletePath);
                            if (currentDocument != null && currentDocument.FilePath == deletePath)
                            {
                                currentDocument = null;
                            }
                            Console.WriteLine("Документ успешно удалён.");
                        }
                        else
                        {
                            Console.WriteLine("Файл не существует.");
                        }
                        PressAnyButton();
                        break;

                    case "12":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("Вы не можете выполнить это действие с вашей ролью!");
                            PressAnyButton();
                            break;
                        }
                        undoRedoManager.Undo();
                        Console.WriteLine("Отмена выполнена.");
                        PressAnyButton();
                        break;

                    case "13":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("Вы не можете выполнить это действие с вашей ролью!");
                            PressAnyButton();
                            break;
                        }
                        undoRedoManager.Redo();
                        Console.WriteLine("Повтор выполнен.");
                        PressAnyButton();
                        break;

                    case "14":
                        running = false;
                        Console.WriteLine("Выход из программы.");
                        PressAnyButton();
                        break;

                    case "15":
                        if (!Session.PermissionStrategy.CanManageUsers())
                        {
                            Console.WriteLine("Доступ запрещён!");
                            PressAnyButton();
                            break;
                        }
                        Console.WriteLine("Доступ разрешён!");
                        Console.WriteLine("Список пользователей:");
                        for (int i = 0; i < UserManager.Users.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {UserManager.Users[i].Name} ({UserManager.Users[i].Role})");
                        }

                        Console.Write("\nВведите номер пользователя для изменения: ");
                        if (!int.TryParse(Console.ReadLine(), out int userNumber) ||
                            userNumber < 1 ||
                            userNumber > UserManager.Users.Count)
                        {
                            Console.WriteLine("Некорректный номер пользователя!");
                            PressAnyButton();
                            break;
                        }

                        var selectedUser = UserManager.Users[userNumber - 1];

                        Console.WriteLine("Доступные роли:");
                        var roles = Enum.GetValues(typeof(UserRole)).Cast<UserRole>().ToList();
                        for (int i = 0; i < roles.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {roles[i]}");
                        }

                        Console.Write("Выберите номер новой роли: ");
                        if (!int.TryParse(Console.ReadLine(), out int roleNumber) ||
                            roleNumber < 1 ||
                            roleNumber > roles.Count)
                        {
                            Console.WriteLine("Некорректный номер роли!");
                            PressAnyButton();
                            break;
                        }

                        UserManager.UpdateUserRole(selectedUser.Name, roles[roleNumber - 1]);
                        bool nulling = false;
                        if (currentDocument == null)
                        {
                            currentDocument = new Document(0);
                            nulling = true;
                        }
                        currentDocument.Notify($"Роль пользователя {selectedUser.Name} успешно изменена на {roles[roleNumber - 1]}!");
                        if (nulling)
                        {
                            currentDocument = null;
                            nulling = false;
                        }
                        PressAnyButton();
                        break;

                    case "16":
                        currentDocument = null;
                        undoRedoManager = new UndoRedoManager();
                        entry = true;
                        running = true;

                        while (entry)
                        {
                            Console.Clear();
                            Console.WriteLine("Выберите пользователя для входа:");
                            for (int i = 0; i < UserManager.Users.Count; i++)
                            {
                                Console.WriteLine($"{i + 1}. {UserManager.Users[i].Name} ({UserManager.Users[i].Role})");
                            }
                            Console.Write("Введите выбор (1-4): ");

                            if (int.TryParse(Console.ReadLine(), out int newUserChoice) &&
                                newUserChoice >= 1 &&
                                newUserChoice <= UserManager.Users.Count)
                            {
                                Session.Login(UserManager.Users[newUserChoice - 1]);
                                Console.WriteLine($"Вход выполнен как: {Session.CurrentUser.Name}");
                                PressAnyButton();
                                entry = false;
                            }
                            else
                            {
                                Console.WriteLine("Неверный выбор. Попробуйте снова.");
                                PressAnyButton();
                            }
                        }
                        break;

                    case "17":
                        if (!Session.PermissionStrategy.CanView())
                        {
                            Console.WriteLine("Доступ запрещён!");
                            break;
                        }
                        TerminalSettingsManager.Instance.ShowTerminalSettingsMenu();
                        PressAnyButton();
                        break;

                    case "18":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен!");
                            PressAnyButton();
                            break;
                        }

                        Console.WriteLine("\nИстория документа:");
                        Console.WriteLine("{0,-25} {1,-10} {2,-50}",
                            "Время", "Действие", "Превью содержимого");

                        foreach (var entry1 in currentDocument.GetHistory())
                        {
                            Console.WriteLine("{0,-25} {1,-10} {2,-50}",
                                entry1.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                entry1.ActionType,
                                entry1.Content.Truncate(90));
                        }
                        PressAnyButton();
                        break;

                    case "19":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("Документ не загружен. Сначала создайте или откройте документ.");
                            PressAnyButton();
                            break;
                        }
                        if (Session.CurrentUser.Role != UserRole.Admin)
                        {
                            Console.WriteLine("Только администратор может изменять права доступа!");
                            PressAnyButton();
                            break;
                        }
                        Console.WriteLine($"Текущие права доступа: {currentDocument.AccessRole}");
                        Console.WriteLine("Выберите новые права доступа:");
                        Console.WriteLine("1. Viewer");
                        Console.WriteLine("2. Editor");
                        Console.WriteLine("3. Auditor");
                        Console.WriteLine("4. Admin");
                        string roleChoice = Console.ReadLine();
                        UserRole newRole;
                        switch (roleChoice)
                        {
                            case "1":
                                newRole = UserRole.Viewer;
                                break;
                            case "2":
                                newRole = UserRole.Editor;
                                break;
                            case "3":
                                newRole = UserRole.Auditor;
                                break;
                            case "4":
                                newRole = UserRole.Admin;
                                break;
                            default:
                                Console.WriteLine("Неверный выбор. Изменения не внесены.");
                                PressAnyButton();
                                continue;
                        }
                        DocumentManager.ChangeAccessRole(currentDocument, newRole);
                        Console.WriteLine($"Права доступа изменены на: {newRole}");
                        PressAnyButton();
                        break;

                    default:
                        Console.WriteLine("Неверный выбор. Пожалуйста, введите число от 1 до 19.");
                        PressAnyButton();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                PressAnyButton();
            }
        }
    }
}