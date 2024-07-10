using UnityEngine;

namespace PixelsHub.Netrooms
{
    [DisallowMultipleComponent]
    public class MaximumAllowedPlayerCountRequirement : PlayerConnectionRequirement
    {
        public const int maximumAmount = 24;

        // Only one requirement of this type is allowed on scene
        public static MaximumAllowedPlayerCountRequirement Instance { get; private set; }

        [SerializeField, Range(2, maximumAmount)]
        private int allowedPlayers = 12;

        public void SetAllowedPlayers(int allowedPlayers)
        {
            if(allowedPlayers < 2)
            {
                this.allowedPlayers = 2;
                Debug.LogError("Cannot set a limit lower than 2");
            }
            else if(allowedPlayers > maximumAmount)
            {
                this.allowedPlayers = maximumAmount;
                Debug.LogError($"Cannot set a limit higher than {maximumAmount}");
            }
            else
                this.allowedPlayers = allowedPlayers;
            
            KickPlayersIfLimitExceeded();
        }

        public override bool DoesPlayerMeetRequirement(NetworkPlayer player, ref string failureReason)
        {
            if(NetworkPlayer.Players.Count >= allowedPlayers)
            {
                failureReason = "PLAYER_LIMIT_REACHED";
                return false;
            }

            return true;
        }

        protected override void OnEnable()
        {
            if(Instance != null)
            {
                Debug.LogError($"Multiple {GetType()} requirements are not allowed on scene. Disabling new...");
                enabled = false;
                return;
            }

            Instance = this;

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if(Instance == this)
                Instance = null;
        }

        private void KickPlayersIfLimitExceeded()
        {
            int playerCount = NetworkPlayer.Players.Count;

            if(playerCount > allowedPlayers)
            {
                int amountToKick = allowedPlayers - playerCount;

                var players = NetworkPlayerSlots.Instance.GetPlayers();

                for(int i = players.Count - 1; amountToKick > 0; i--)
                {
                    if(players[i] != null)
                    {
                        players[i].Kick();
                        amountToKick--;
                    }
                }
            }
        }
    }
}
