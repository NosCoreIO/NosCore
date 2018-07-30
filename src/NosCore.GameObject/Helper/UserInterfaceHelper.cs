using System;
using NosCore.Database.Entities;
using NosCore.Shared.Enumerations.Items;

namespace NosCore.GameObject.Helper
{
    public class UserInterfaceHelper
    {
        private static UserInterfaceHelper _instance = null;

        private UserInterfaceHelper()
        {
        }

        public static UserInterfaceHelper Instance => _instance ?? (_instance = new UserInterfaceHelper());
    }
}