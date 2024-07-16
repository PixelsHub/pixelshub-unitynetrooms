using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace PixelsHub.Netrooms
{
    public class PlayerPermissions
    {
        public static event Action OnPermissionsChanged;

        /// <summary>
        /// Value associated with the current local player to check for permissions.
        /// Implementations must specify this value for players (such as id, role or custom conditions).
        /// </summary>
        public static string LocalPlayerValue;

        public static string RegisterJson => registerJson;

        private static Dictionary<string, List<string>> register = new();

        private static string registerJson;

        public static ReadonlyPlayerPermissionList GenerateReadonlyList() => register;

        public static bool CheckLocalPlayer(string permissionCode, bool passCheckIfCodeNotFound = false)
        {
            if(LocalPlayerValue == null)
                throw new Exception("LocalPlayerValue has not been set for PlayerPermissions.");
           
            return Check(permissionCode, LocalPlayerValue, passCheckIfCodeNotFound);
        }

        public static bool Check(string permissionCode, string playerValue, bool passCheckIfCodeNotFound = false)
        {
            if(register.TryGetValue(permissionCode, out var set))
                return set.Contains(playerValue);

            Debug.LogError($"Checked for permission \"{permissionCode}\" but it could not be found.");

            return passCheckIfCodeNotFound;
        }

        public static void ParsePermissions(string permissionsJson)
        {
            if(string.IsNullOrEmpty(permissionsJson))
            {
                register = new();
            }
            else
            {
                try
                {
                    register = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(permissionsJson);
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            registerJson = permissionsJson;
            OnPermissionsChanged?.Invoke();
        }

        public static void SetPermissions(Dictionary<string, List<string>> permissions)
        {
            register = permissions;
            registerJson = JsonConvert.SerializeObject(register);
            OnPermissionsChanged?.Invoke();
        }
    }
}
