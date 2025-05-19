using Lab2.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lab2.Classes
{
    public static class UserManager
    {
        private const string UsersFilePath = "D:\\OOP\\DocumentEditor\\Lab2\\UserData.json";
        private static List<User> _users;

        static UserManager()
        {
            LoadUsers();
            InitializeDefaultUsers();
        }

        public static List<User> Users => _users;

        private static void InitializeDefaultUsers()
        {
            // Define default users
            string[] defaultUserNames = { "User1", "User2", "User3", "User4" };
            var validUsers = new List<User>();

            // Keep only valid users from the loaded list
            foreach (var user in _users)
            {
                if (defaultUserNames.Contains(user.Name) || !IsDefaultInvalidUser(user.Name))
                {
                    validUsers.Add(user);
                }
            }
            _users = validUsers;

            // Ensure default users exist, all as Viewer
            foreach (var name in defaultUserNames)
            {
                if (!_users.Any(u => u.Name == name))
                {
                    _users.Add(new User(name, UserRole.Viewer));
                }
            }

            SaveUsers();
        }

        private static bool IsDefaultInvalidUser(string name)
        {
            // List of known invalid user names from your output
            string[] invalidNames = { "Вьюер Вьюеров", "Эдитор Эдиторов", "Админ Админов", "Аудитор Проверялов" };
            return invalidNames.Contains(name);
        }

        private static void LoadUsers()
        {
            if (!File.Exists(UsersFilePath))
            {
                Console.WriteLine("UserData.json not found. Creating new user list.");
                _users = new List<User>();
                SaveUsers();
                return;
            }

            try
            {
                var json = File.ReadAllText(UsersFilePath);
                _users = JsonConvert.DeserializeObject<List<User>>(json) ?? new List<User>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading users: {ex.Message}");
                _users = new List<User>();
                SaveUsers();
            }
        }

        public static void SaveUsers()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_users, Formatting.Indented);
                File.WriteAllText(UsersFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving users: {ex.Message}");
            }
        }

        public static void AddUser(User user)
        {
            if (!_users.Any(u => u.Name == user.Name))
            {
                _users.Add(user);
                SaveUsers();
            }
        }

        public static void UpdateUserRole(string userName, UserRole newRole)
        {
            var user = _users.FirstOrDefault(u => u.Name == userName);
            if (user != null)
            {
                user.Role = newRole;
                SaveUsers();
            }
        }

        public static IEnumerable<User> GetAdmins()
            => Users.Where(u => u.Role == UserRole.Admin || u.Role == UserRole.Auditor);
    }
}