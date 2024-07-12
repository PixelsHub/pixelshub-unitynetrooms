using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace PixelsHub.Netrooms
{
    public class PlayerPermissions
    {
        public class Register : Dictionary<string, List<string>> { }

        public static event Action OnPermissionsChanged;

        /// <summary>
        /// Value associated with the current local player to check for permissions.
        /// Implementations must specify this value for players (such as id, role or custom conditions).
        /// </summary>
        public static string LocalPlayerValue;

        public static string RegisterJson => registerJson;

        private static Register register = new();

        private static string registerJson;

        public static ReadonlyPlayerPermissionList GenerateReadonlyList() => register;

        public static bool CheckLocalPlayer(string permissionCode)
        {
            if(LocalPlayerValue == null)
            {
                Debug.LogError("LocalPlayerValue has not been set for PlayerPermissions.");
                return false;
            }

            return Check(permissionCode, LocalPlayerValue);
        }

        public static bool Check(string permissionCode, string playerValue)
        {
            if(register.TryGetValue(permissionCode, out var set))
                return set.Contains(playerValue);
            else
                Debug.LogError($"Checked for permission \"{permissionCode}\" but it could not be found.");

            return false;
        }

        public static void ParsePermissions(string permissionsJson)
        {
            try
            {
                register = JsonConvert.DeserializeObject<Register>(permissionsJson);
                registerJson = permissionsJson;
                OnPermissionsChanged?.Invoke();
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public static void SetPermissions(Register permissions)
        {
            register = permissions;
            registerJson = JsonConvert.SerializeObject(register);
            OnPermissionsChanged?.Invoke();
        }
    }
}
