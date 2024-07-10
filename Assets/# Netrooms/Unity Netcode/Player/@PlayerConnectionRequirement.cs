using System.Collections.Generic;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public abstract class PlayerConnectionRequirement : MonoBehaviour
    {
        private static readonly List<PlayerConnectionRequirement> requirements = new();

        public static bool IsPlayerAllowedToConnect(NetworkPlayer player, out string failureReason)
        {
            failureReason = null;

            for(int i = 0; i < requirements.Count; i++)
                if(!requirements[i].DoesPlayerMeetRequirement(player, ref failureReason))
                    return false;

            return true;
        }

        public abstract bool DoesPlayerMeetRequirement(NetworkPlayer player, ref string failureReason);

        protected virtual void OnEnable()
        {
            requirements.Add(this);
        }

        protected virtual void OnDisable()
        {
            requirements.Remove(this);
        }
    }
}
