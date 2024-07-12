using System;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    /// <summary>
    /// The static identifier assigned locally for the player.
    /// Assignation is expected before client connection, such as after performing a login or via user input.
    /// </summary>
    public static class LocalPlayerUserIdentifier
    {
        public static event Action<string> OnLocalIdentifierChanged;

        public static string Value
        {
            get => value;
            private set
            {
                if(string.IsNullOrEmpty(value))
                {
                    Debug.Assert(false);
                    return;
                }

                value = value.Trim();
                if(value != LocalPlayerUserIdentifier.value)
                {
                    LocalPlayerUserIdentifier.value = value;
                    OnLocalIdentifierChanged?.Invoke(value);
                }
            }
        }

        private static string value = "player";
    }
}
