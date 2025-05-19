using System;
using System.IO;
using Newtonsoft.Json;
using Lab2.Interfaces;
using Lab2.Classes;
using Lab2.Documentn;
using Lab2.Enums;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

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

    static User CreateOrSelectUser(Document currentDocument)
    {
        Console.Clear();
        Console.WriteLine("1. Создать нового пользователя");
        Console.WriteLine("2. Выбрать существующего пользователя");
        Console.Write("Выберите действие: ");
        string choice = Console.ReadLine();

        if (choice == "1")
        {
            Console.Write("Введите имя пользователя: ");
            string userName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userName) || UserManager.Users.Any(u => u.Name == userName))
            {
                Console.WriteLine("Имя пользователя недопустимо или уже существует. Попробуйте снова.");
                PressAnyButton();
                return CreateOrSelectUser(currentDocument);
            }
            User newUser = new User(userName, UserRole.Viewer);
            UserManager.AddUser(newUser);
            UserManager.SaveUsers();
            return newUser;
        }
        else if (choice == "2")
        {
            if (UserManager.Users.Count == 0)
            {
                Console.WriteLine("Нет существующих пользователей. Создайте нового.");
                PressAnyButton();
                return CreateOrSelectUser(currentDocument);
            }

            Console.WriteLine("Список пользователей:");
            for (int i = 0; i < UserManager.Users.Count; i++)
            {
                User user = UserManager.Users[i];
                string roleInfo = GetUserRoleDisplay(user, currentDocument);
                Console.WriteLine($"{i + 1}. {user.Name} ({roleInfo})");
            }
            Console.Write("Выберите пользователя: ");
            if (int.TryParse(Console.ReadLine(), out int userIndex) && userIndex > 0 && userIndex <= UserManager.Users.Count)
            {
                return UserManager.Users[userIndex - 1];
            }
            else
            {
                Console.WriteLine("Неверный выбор. Попробуйте снова.");
                PressAnyButton();
                return CreateOrSelectUser(currentDocument);
            }
        }
        else
        {
            Console.WriteLine("Неверный выбор. Попробуйте снова.");
            PressAnyButton();
            return CreateOrSelectUser(currentDocument);
        }
    }

    static string GetUserRoleDisplay(User user, Document currentDocument)
    {
        // Directory where .permissions.json files are stored
        string directory = @"D:\OOP\DocumentEditor\Lab2\bin\Debug\net9.0"; // Adjust to your actual project directory
        List<string> roleDescriptions = new List<string>();

        // If a document is loaded, prioritize its permissions
        if (currentDocument != null)
        {
            UserRole fileRole = currentDocument.GetUserPermission(user);
            if (fileRole != UserRole.Viewer)
            {
                string fileName = Path.GetFileName(currentDocument.FilePath);
                roleDescriptions.Add($"{fileRole} of {fileName}");
            }
        }

        // Scan .permissions.json files for other file-specific roles
        try
        {
            if (Directory.Exists(directory))
            {
                Console.WriteLine($"[DEBUG] Scanning directory: {directory}");
                var permissionFiles = Directory.GetFiles(directory, "*.permissions.json");
                Console.WriteLine($"[DEBUG] Found {permissionFiles.Length} .permissions.json files");

                foreach (var permFile in permissionFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(permFile);
                        var permissions = JsonConvert.DeserializeObject<Dictionary<string, UserRole>>(json);
                        if (permissions != null && permissions.ContainsKey(user.Name) && permissions[user.Name] != UserRole.Viewer)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(permFile).Replace(".permissions", "");
                            // Avoid duplicating the current document's role
                            if (currentDocument == null || Path.GetFileName(currentDocument.FilePath) != fileName)
                            {
                                roleDescriptions.Add($"{permissions[user.Name]} of {fileName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] Error reading {permFile}: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine($"[DEBUG] Directory {directory} does not exist");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Error accessing directory {directory}: {ex.Message}");
        }

        // Return file-specific roles or "No file permissions"
        if (roleDescriptions.Any())
        {
            return string.Join(", ", roleDescriptions);
        }
        return "No file permissions";
    }

    static Document currentDocument = null;

    static async Task Main(string[] args)
    {
        UndoRedoManager undoRedoManager = new UndoRedoManager();
        bool running = true;

        // Выбор или создание пользователя при запуске
        User currentUser = CreateOrSelectUser(currentDocument);
        Session.Login(currentUser);
        Console.WriteLine($"Текущий пользователь: {Session.CurrentUser.Name}");
        PressAnyButton();

        while (running)
        {
            Console.Clear();
            Console.WriteLine("Система управления документами");
            Console.WriteLine("--------------------------");
            Console.WriteLine($"Текущий пользователь: {Session.CurrentUser.Name}");
            Console.WriteLine($"Текущий документ: {(currentDocument?.FilePath ?? "Отсутствует")}");
            Console.WriteLine($"Тип текущего документа: {(currentDocument != null ? currentDocument.Type.ToString() : "Отсутствует")}");
            Console.WriteLine($"Роль в текущем файле: {(currentDocument != null ? currentDocument.GetUserPermission(Session.CurrentUser).ToString() : "N/A")}");

            // Проверка прав доступа (use file-specific role)
            bool canEdit = currentDocument == null ||
                          currentDocument.GetUserPermission(Session.CurrentUser) >= UserRole.Editor;

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
            if (canEdit)
            {
                Console.WriteLine("3. Добавить текст");
                Console.WriteLine("4. Дополнить текст");
                Console.WriteLine("5. Удалить текст");
                Console.WriteLine("6. Копировать текст");
                Console.WriteLine("7. Вырезать текст");
                Console.WriteLine("8. Вставить текст");
            }
            Console.WriteLine("9. Поиск слова");
            Console.WriteLine("10. Сохранить документ");
            if (canEdit)
            {
                Console.WriteLine("11. Удалить документ");
            }
            Console.WriteLine("12. Отменить");
            Console.WriteLine("13. Повторить");
            Console.WriteLine("14. Выход");
            Console.WriteLine("=============================");
            if (Session.CurrentUser.Role == UserRole.Admin)
            {
                Console.WriteLine("15. Управление пользователями (только для Admin)");
            }
            Console.WriteLine("16. Сменить пользователя");
            Console.WriteLine("17. Настройки терминала");
            Console.WriteLine("18. Просмотр истории документа");
            if (currentDocument != null && currentDocument.GetUserPermission(Session.CurrentUser) == UserRole.Admin)
            {
                Console.WriteLine("19. Изменить права доступа (только для Admin файла)");
            }
            Console.Write("\nВведите ваш выбор: ");

            string choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
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
                                break;
                        }
                        currentDocument = DocumentManager.CreateNewDocument(docType, Session.CurrentUser);
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
                        PressAnyButton();
                        break;

                    case "3":
                        if (!canEdit)
                        {
                            Console.WriteLine("У вас нет прав на редактирование этого документа!");
                            PressAnyButton();
                            break;
                        }
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
                        if (!canEdit)
                        {
                            Console.WriteLine("У вас нет прав на редактирование этого документа!");
                            PressAnyButton();
                            break;
                        }
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
                        if (!canEdit)
                        {
                            Console.WriteLine("У вас нет прав на редактирование этого документа!");
                            PressAnyButton();
                            break;
                        }
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
                        if (!canEdit)
                        {
                            Console.WriteLine("У вас нет прав на редактирование этого документа!");
                            PressAnyButton();
                            break;
                        }
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
                        if (!canEdit)
                        {
                            Console.WriteLine("У вас нет прав на редактирование этого документа!");
                            PressAnyButton();
                            break;
                        }
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
                        if (!canEdit)
                        {
                            Console.WriteLine("У вас нет прав на удаление этого документа!");
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
                        undoRedoManager.Undo();
                        Console.WriteLine("Отмена выполнена.");
                        PressAnyButton();
                        break;

                    case "13":
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
                        if (Session.CurrentUser.Role != UserRole.Admin)
                        {
                            Console.WriteLine("Доступ запрещён! Требуется роль Admin.");
                            PressAnyButton();
                            break;
                        }
                        Console.WriteLine("Список пользователей:");
                        for (int i = 0; i < UserManager.Users.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {UserManager.Users[i].Name} ({UserManager.Users[i].Role})");
                        }
                        Console.Write("\nВведите номер пользователя для изменения: ");
                        if (!int.TryParse(Console.ReadLine(), out int userNumber) ||
                            userNumber < 1 || userNumber > UserManager.Users.Count)
                        {
                            Console.WriteLine("Некорректный номер пользователя!");
                            PressAnyButton();
                            break;
                        }
                        var userToUpdate = UserManager.Users[userNumber - 1];
                        Console.WriteLine("Доступные роли:");
                        var roles = Enum.GetValues(typeof(UserRole)).Cast<UserRole>().ToList();
                        for (int i = 0; i < roles.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}. {roles[i]}");
                        }
                        Console.Write("Выберите номер новой роли: ");
                        if (!int.TryParse(Console.ReadLine(), out int roleNumber) ||
                            roleNumber < 1 || roleNumber > roles.Count)
                        {
                            Console.WriteLine("Некорректный номер роли!");
                            PressAnyButton();
                            break;
                        }
                        UserManager.UpdateUserRole(userToUpdate.Name, roles[roleNumber - 1]);
                        Console.WriteLine($"Роль пользователя {userToUpdate.Name} изменена на {roles[roleNumber - 1]}!");
                        PressAnyButton();
                        break;

                    case "16":
                        currentDocument = null;
                        undoRedoManager = new UndoRedoManager();
                        User newUser = CreateOrSelectUser(currentDocument);
                        Session.Login(newUser);
                        Console.WriteLine($"Текущий пользователь: {Session.CurrentUser.Name}");
                        PressAnyButton();
                        break;

                    case "17":
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
                        Console.WriteLine("{0,-25} {1,-10} {2,-50}", "Время", "Действие", "Превью содержимого");
                        foreach (var entry in currentDocument.GetHistory())
                        {
                            Console.WriteLine("{0,-25} {1,-10} {2,-50}",
                                entry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                entry.ActionType,
                                entry.Content.Truncate(90));
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
                        if (currentDocument.GetUserPermission(Session.CurrentUser) != UserRole.Admin)
                        {
                            Console.WriteLine("Только администратор файла может изменять права доступа!");
                            PressAnyButton();
                            break;
                        }
                        Console.WriteLine("Список пользователей:");
                        for (int i = 0; i < UserManager.Users.Count; i++)
                        {
                            User user = UserManager.Users[i];
                            Console.WriteLine($"{i + 1}. {user.Name} (Текущая роль для файла: {currentDocument.GetUserPermission(user)})");
                        }
                        Console.Write("Введите номер пользователя для изменения прав: ");
                        if (int.TryParse(Console.ReadLine(), out int userIdx) && userIdx >= 1 && userIdx <= UserManager.Users.Count)
                        {
                            User selectedUserForPermission = UserManager.Users[userIdx - 1];
                            Console.WriteLine("Выберите новую роль для файла:");
                            Console.WriteLine("1. Viewer (только просмотр)");
                            Console.WriteLine("2. Editor (редактирование)");
                            Console.WriteLine("3. Admin (полные права)");
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
                                    newRole = UserRole.Admin;
                                    break;
                                default:
                                    Console.WriteLine("Неверный выбор. Изменения не внесены.");
                                    PressAnyButton();
                                    continue;
                            }
                            currentDocument.SetUserPermission(selectedUserForPermission, newRole);
                            Console.WriteLine($"Права пользователя {selectedUserForPermission.Name} для файла изменены на {newRole}.");
                        }
                        else
                        {
                            Console.WriteLine("Неверный выбор пользователя.");
                        }
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