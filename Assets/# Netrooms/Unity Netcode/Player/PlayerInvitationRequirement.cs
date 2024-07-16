using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    /// <summary>
    /// Provides a list of identifiers for NetworkPlayers to limit who is allowed to connect.
    /// </summary>
    public class PlayerInvitationRequirement : PlayerConnectionRequirement
    {
        public event Action<IReadOnlyList<string>> OnInvitedPlayersChanged;

        public PlayerInvitationRequirement Instance { get; private set; }

        public IReadOnlyList<string> InvitedPlayers => invitedPlayers;

        private const string playerNotInvitedReason = "PLAYER_NOT_INVITED";

        [SerializeField]
        private List<string> invitedPlayers;

        public void SetInvitedPlayers(params string[] invitedPlayers)
        {
            if(invitedPlayers == null || invitedPlayers.Length < 2)
            {
                Debug.LogError("Cannot set less than 2 invited players.");
                return;
            }

            this.invitedPlayers = new(invitedPlayers.Length);

            for(int i = 0; i < invitedPlayers.Length; i++)
            {
                if(string.IsNullOrEmpty(invitedPlayers[i]))
                    continue;

                this.invitedPlayers.Add(invitedPlayers[i].Trim());
            }

            OnInvitedPlayersChanged?.Invoke(invitedPlayers);
        }

        public override bool DoesPlayerMeetRequirement(NetworkPlayer player, ref string failureReason)
        {
            if(!invitedPlayers.Contains(player.UserIdentifier.ToString()))
            {
                failureReason = playerNotInvitedReason;
                return false;
            }

            return true;
        }

        protected override void OnEnable()
        {
            if(Instance != null)
            {
                Debug.LogError($"Only a single {GetType()} is permitted. Disabling new in \"{name}\"...");
                enabled = false;
                return;
            }

            Instance = this;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            if(Instance == this)
                Instance = null;

            base.OnDisable();
        }
    }
}
