using System;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public struct User
    {
        public const string server = "server";

        public static string LocalIdentifier { get; private set; } = "player";

        public string identifier;
        public string name;
        public string role;

        public bool connected;
        public int connectedToRoom;

        public static void SetLocalIdentifier(string identifier) 
        {
            LocalIdentifier = identifier;
        }

        public readonly bool IsLocal() => identifier == LocalIdentifier;

        public readonly string GetNamePrefix()
        {
            try
            {
                if(string.IsNullOrEmpty(name))
                {
                    if(string.IsNullOrEmpty(identifier))
                        return "??";

                    return identifier[..2].ToUpper();
                }

                var split = name.Trim().ToUpper().Split(' ');
                if(split.Length > 1 && split[1].Length > 0)
                {
                    return $"{split[0][0]}{split[1][0]}";
                }
                else
                    return split[0][..2];
            }
            catch(Exception ex)
            {
                Debug.LogError(name);
                Debug.LogException(ex);
                return "ERR";
            }
        }
    }
}