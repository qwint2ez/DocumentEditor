using Lab2.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab2.Classes
{
    public static class UserManager
    {
        private const string UsersFilePath = "D:\\OOP\\DocumentEditor\\Lab2\\UserData.json";
        private static List<User> _users;

        static UserManager()
        {
            LoadUsers();
        }

        public static List<User> Users => _users;

        private static void LoadUsers()
        {
            if (!File.Exists(UsersFilePath))
            {
                Console.WriteLine("UserData.json not found. Creating default users.");
                _users = new List<User>
            {
                new User("Вьюер Вьюеров", UserRole.Viewer),
                new User("Эдитор Эдиторов", UserRole.Editor),
                new User("Админ Админов", UserRole.Admin),
                new User("Аудитор Проверялов", UserRole.Auditor)
            };
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
                _users = new List<User>
            {
                new User("Вьюер Вьюеров", UserRole.Viewer),
                new User("Эдитор Эдиторов", UserRole.Editor),
                new User("Админ Админов", UserRole.Admin),
                new User("Аудитор Проверялов", UserRole.Auditor)
            };
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
        => Users.Where(u => u.Role == UserRole.Admin);
    }
}
