using Lab2.Enums;
using Lab2.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab2.Classes
{
    public static class Session
    {
        public static User CurrentUser { get; private set; }
        public static IPermissionStrategy PermissionStrategy { get; private set; }

        public static void Login(User user)
        {
            CurrentUser = user;
            PermissionStrategy = user.Role switch
            {
                UserRole.Viewer => new ViewerPermission(),
                UserRole.Editor => new EditorPermission(),
                UserRole.Auditor => new AuditorPermission(),
                UserRole.Admin => new AdminPermission(),
                _ => new ViewerPermission()
            };
        }
    }
}
