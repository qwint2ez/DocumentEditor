using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Lab2.Enums;
using Lab2.Interfaces;

namespace Lab2.Classes
{
    public class User : IObserver
    {
        public string Name { get; }
        public UserRole Role { get; set; }

        [JsonConstructor]
        public User(string name, UserRole role)
        {
            Name = name;
            Role = role;
        }

        public void Update(string message)
        {
            Console.WriteLine($"[Notification to {Name}]: {message}");
        }
        public override string ToString() => $"{Name} ({Role})";
    }

    public class ViewerPermission : IPermissionStrategy
    {
        public bool CanView() => true;
        public bool CanEdit() => false;
        public bool CanManageUsers() => false;
    }

    public class EditorPermission : IPermissionStrategy
    {
        public bool CanView() => true;
        public bool CanEdit() => true;
        public bool CanManageUsers() => false;
    }
    public class AuditorPermission : IPermissionStrategy
    {
        public bool CanView() => true;
        public bool CanEdit() => true;
        public bool CanManageUsers() => false;
    }

    public class AdminPermission : IPermissionStrategy
    {
        public bool CanView() => true;
        public bool CanEdit() => true;
        public bool CanManageUsers() => true;
    }
}
