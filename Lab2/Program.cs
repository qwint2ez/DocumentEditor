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
            Console.WriteLine("Current fragments:");
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
            Console.WriteLine("No fragments available or no document loaded.");
        }
    }
    static private void PressAnyButton()
    {
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
    static async Task Main(string[] args)
    {
        Document currentDocument = null;
        UndoRedoManager undoRedoManager = new UndoRedoManager();
        bool running = true, entry = true;

        while (entry)
        {
            Console.WriteLine("Select user to login:");
            if (UserManager.Users.Count == 0)
            {
                Console.WriteLine("No users available. Check UserData.json or configuration.");
                PressAnyButton();
                return;
            }

            for (int i = 0; i < UserManager.Users.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {UserManager.Users[i].Name} ({UserManager.Users[i].Role})");
            }
            Console.Write($"Enter choice (1-{UserManager.Users.Count}): ");

            if (int.TryParse(Console.ReadLine(), out int userChoice) && userChoice >= 1 && userChoice <= UserManager.Users.Count)
            {
                Session.Login(UserManager.Users[userChoice - 1]);
                Console.WriteLine($"Logged in as: {Session.CurrentUser.Name}");
                PressAnyButton();
                entry = false;
            }
            else
            {
                Console.WriteLine("Invalid choice. Try again.");
                PressAnyButton();
                Console.Clear();
            }
        }

        while (running)
        {
            Console.Clear();
            Console.WriteLine("Document Management System");
            Console.WriteLine("--------------------------");
            Console.WriteLine($"Current User: {Session.CurrentUser.Name} | Role: {Session.CurrentUser.Role}");
            Console.WriteLine("Current Document: " + (currentDocument?.FilePath ?? "None"));
            Console.WriteLine("Current Document type: " + (currentDocument != null ? currentDocument.Type.ToString() : "None"));
            Console.WriteLine("Content:");
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
                Console.WriteLine("No content");
            }
            Console.WriteLine("\nOptions:");
            Console.WriteLine("1. Create New Document");
            Console.WriteLine("2. Open Document");
            Console.WriteLine("3. Append Text");
            Console.WriteLine("4. Insert Text");
            Console.WriteLine("5. Delete Text");
            Console.WriteLine("6. Copy Text");
            Console.WriteLine("7. Cut Text");
            Console.WriteLine("8. Paste Text");
            Console.WriteLine("9. Search Word");
            Console.WriteLine("10. Save Document");
            Console.WriteLine("11. Delete Document");
            Console.WriteLine("12. Undo");
            Console.WriteLine("13. Redo");
            Console.WriteLine("14. Exit");
            Console.WriteLine("=============================");
            Console.WriteLine("15. Manage Users (Admin only)");
            Console.WriteLine("16. Switch User");
            Console.WriteLine("17. Terminal Settings");
            Console.WriteLine("18. View Document History");
            Console.Write("\nEnter your choice (1-18): ");

            string choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("You cant do this action with your role!");
                            PressAnyButton();
                            break;
                        }
                        Console.WriteLine("Select document type:");
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
                                Console.WriteLine("Invalid choice. Defaulting to PlainText.");
                                docType = DocumentType.PlainText;
                                PressAnyButton();
                                break;
                        }
                        currentDocument = DocumentManager.CreateNewDocument(docType);
                        currentDocument.Notify("New document created!");
                        currentDocument.Subscribe(Session.CurrentUser);
                        Console.WriteLine($"New {docType} document created.");
                        PressAnyButton();
                        break;

                    case "2":
                        Console.WriteLine("Select storage type:");
                        Console.WriteLine("1. Local File");
                        Console.WriteLine("2. Supabase Cloud");
                        var storageChoice = Console.ReadLine();

                        try
                        {
                            if (storageChoice == "2")
                            {
                                DocumentManager.SetStorageStrategy(new SupabaseStorageStrategy());
                                Console.Write("Enter cloud file name (e.g., document.json): ");
                            }
                            else
                            {
                                DocumentManager.SetStorageStrategy(new LocalFileStrategy());
                                Console.Write("Enter local file path (e.g., doc.txt): ");
                            }

                            string FileName = Console.ReadLine();
                            currentDocument = await DocumentManager.OpenDocument(FileName);
                            Console.WriteLine($"Loaded: {FileName}");

                            currentDocument.Subscribe(Session.CurrentUser);
                            Console.WriteLine($"Type: {currentDocument.Type}");
                            Console.WriteLine($"Content:\n{currentDocument.GetDisplayText()}");
                        }
                        catch (FileNotFoundException ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }
                        catch (Postgrest.Exceptions.PostgrestException ex)
                        {
                            Console.WriteLine($"Supabase error: {ex.Message}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }

                        PressAnyButton();
                        break;

                    case "3":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("No document loaded. Create or open a document first.");
                        }
                        else
                        {
                            Console.WriteLine("Enter text to append (use **bold**, __underline__, *italic*, #, ##, ###). Type 'END' on a new line to finish:");
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
                            Console.WriteLine("No document loaded. Create or open a document first.");
                        }
                        else
                        {
                            Console.Write("Enter character position to insert at (ignoring **, __, *): ");
                            int insertPos = int.Parse(Console.ReadLine());
                            Console.Write("Enter text to insert (use **bold**, __underline__, *italic*): ");
                            string insertText = Console.ReadLine();
                            ICommand insertCommand = new InsertTextCommand(currentDocument, insertPos, insertText);
                            undoRedoManager.ExecuteCommand(insertCommand);
                        }
                        PressAnyButton();
                        break;

                    case "5":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("No document loaded. Create or open a document first.");
                        }
                        else
                        {
                            DisplayFragments(currentDocument);
                            Console.Write("Enter start fragment index to delete: ");
                            int deleteStart = int.Parse(Console.ReadLine());
                            Console.Write("Enter number of fragments to delete: ");
                            int deleteCount = int.Parse(Console.ReadLine());
                            ICommand deleteCommand = new DeleteTextCommand(currentDocument, deleteStart, deleteCount);
                            undoRedoManager.ExecuteCommand(deleteCommand);
                        }
                        PressAnyButton();
                        break;

                    case "6":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("No document loaded. Create or open a document first.");
                        }
                        else
                        {
                            DisplayFragments(currentDocument);
                            Console.Write("Enter start fragment index to copy: ");
                            int copyStart = int.Parse(Console.ReadLine());
                            Console.Write("Enter number of fragments to copy: ");
                            int copyCount = int.Parse(Console.ReadLine());
                            ICommand copyCommand = new CopyTextCommand(currentDocument, copyStart, copyCount);
                            undoRedoManager.ExecuteCommand(copyCommand);
                            Console.WriteLine("Text copied to clipboard.");
                        }
                        PressAnyButton();
                        break;

                    case "7":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("No document loaded. Create or open a document first.");
                        }
                        else
                        {
                            DisplayFragments(currentDocument);
                            Console.Write("Enter start fragment index to cut: ");
                            int cutStart = int.Parse(Console.ReadLine());
                            Console.Write("Enter number of fragments to cut: ");
                            int cutCount = int.Parse(Console.ReadLine());
                            ICommand cutCommand = new CutTextCommand(currentDocument, cutStart, cutCount);
                            undoRedoManager.ExecuteCommand(cutCommand);
                            Console.WriteLine("Text cut to clipboard.");
                        }
                        PressAnyButton();
                        break;

                    case "8":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("No document loaded. Create or open a document first.");
                        }
                        else
                        {
                            DisplayFragments(currentDocument);
                            Console.Write("Enter fragment position to paste at: ");
                            int pastePos = int.Parse(Console.ReadLine());
                            ICommand pasteCommand = new PasteTextCommand(currentDocument, pastePos);
                            undoRedoManager.ExecuteCommand(pasteCommand);
                            Console.WriteLine("Text pasted from clipboard.");
                        }
                        PressAnyButton();
                        break;
                    case "9":
                        if (currentDocument == null)
                        {
                            Console.WriteLine("No document loaded. Create or open a document first.");
                        }
                        else
                        {
                            Console.Write("Enter word to search (ignore **, __, *): ");
                            string searchWord = Console.ReadLine();
                            List<int> positions = currentDocument.SearchWord(searchWord);
                            if (positions.Count > 0)
                            {
                                Console.WriteLine($"Found '{searchWord}' at positions: {string.Join(", ", positions)}");
                            }
                            else
                            {
                                Console.WriteLine($"'{searchWord}' not found.");
                            }
                        }
                        PressAnyButton();
                        break;

                    case "10":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("You cant do this action with your role!");
                            PressAnyButton();
                            break;
                        }
                        if (currentDocument == null)
                        {
                            Console.WriteLine("No document loaded. Create or open a document first.");
                            PressAnyButton();
                            break;
                        }

                        Console.WriteLine("Select storage type:");
                        Console.WriteLine("1. Local File");
                        Console.WriteLine("2. Supabase Cloud");
                        var storageChoice1 = Console.ReadLine();

                        try
                        {
                            if (storageChoice1 == "1")
                            {
                                DocumentManager.SetStorageStrategy(new LocalFileStrategy());
                                Console.Write("Enter local file name to save (e.g., doc.txt): ");
                            }
                            else if (storageChoice1 == "2")
                            {
                                DocumentManager.SetStorageStrategy(new SupabaseStorageStrategy());
                                Console.Write("Enter cloud file name to save (e.g., document.json): ");
                            }
                            else
                            {
                                throw new ArgumentException("Invalid storage type");
                            }
                            string FileName = Console.ReadLine();
                            await DocumentManager.SaveDocument(currentDocument, FileName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");
                        }

                        PressAnyButton();
                        break;

                    case "11":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("You cant do this action with your role!");
                            PressAnyButton();
                            break;
                        }
                        Console.Write("Enter filename to delete: ");
                        string deletePath = Console.ReadLine();
                        if (File.Exists(deletePath))
                        {
                            DocumentManager.DeleteDocument(deletePath);
                            if (currentDocument != null && currentDocument.FilePath == deletePath)
                            {
                                currentDocument = null;
                            }
                            Console.WriteLine("Document deleted successfully.");
                        }
                        else
                        {
                            Console.WriteLine("File does not exist.");
                        }
                        PressAnyButton();
                        break;

                    case "12":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("You cant do this action with your role!");
                            PressAnyButton();
                            break;
                        }
                        undoRedoManager.Undo();
                        Console.WriteLine("Undo performed.");
                        PressAnyButton();
                        break;

                    case "13":
                        if (!Session.PermissionStrategy.CanEdit())
                        {
                            Console.WriteLine("You cant do this action with your role!");
                            PressAnyButton();
                            break;
                        }
                        undoRedoManager.Redo();
                        Console.WriteLine("Redo performed.");
                        PressAnyButton();
                        break;

                    case "14":
                        running = false;
                        Console.WriteLine("Exiting program.");
                        PressAnyButton();
                        break;
                    case "15":
                        if (!Session.PermissionStrategy.CanManageUsers())
                        {
                            Console.WriteLine("Access denied!");
                            PressAnyButton();
                            break;
                        }
                        Console.WriteLine("Access granted!");
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
                            Console.WriteLine("Select user to login:");
                            for (int i = 0; i < UserManager.Users.Count; i++)
                            {
                                Console.WriteLine($"{i + 1}. {UserManager.Users[i].Name} ({UserManager.Users[i].Role})");
                            }
                            Console.Write("Enter choice (1-4): ");

                            if (int.TryParse(Console.ReadLine(), out int newUserChoice) &&
                                newUserChoice >= 1 &&
                                newUserChoice <= UserManager.Users.Count)
                            {
                                Session.Login(UserManager.Users[newUserChoice - 1]);
                                Console.WriteLine($"Logged in as: {Session.CurrentUser.Name}");
                                PressAnyButton();
                                entry = false;
                            }
                            else
                            {
                                Console.WriteLine("Invalid choice. Try again.");
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
                            Console.WriteLine("No document loaded!");
                            PressAnyButton();
                            break;
                        }

                        Console.WriteLine("\nDocument History:");
                        Console.WriteLine("{0,-25} {1,-10} {2,-50}",
                            "Timestamp", "Action", "Content Preview");

                        foreach (var entry1 in currentDocument.GetHistory())
                        {
                            Console.WriteLine("{0,-25} {1,-10} {2,-50}",
                                entry1.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                                entry1.ActionType,
                                entry1.Content.Truncate(90));
                        }
                        PressAnyButton();
                        break;

                    default:
                        Console.WriteLine("Invalid choice. Please enter a number between 1 and 14.");
                        PressAnyButton();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                PressAnyButton();
            }
        }
    }
}