using System;
using System.Collections;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class PlayerSpatialPing : MonoBehaviour
    {
        private NetworkPlayerSpatialPinger.Pool pool;

        [SerializeField]
        private ColorEvent onColorAssigned;

        public void Initialize(NetworkPlayerSpatialPinger.Pool pool)
        {
            this.pool = pool;   
        }

        public void Play(Vector3 position, Quaternion rotation, NetworkPlayer player) 
        {
            transform.SetLocalPositionAndRotation(position, rotation);

            if(player != null)
            {
                onColorAssigned.Invoke(player.Color);
            }
            else
            {
                onColorAssigned.Invoke(PlayerColoringScheme.undefinedColor);
            }
        }

        /// <summary>
        /// Expected to be called from animator event.
        /// </summary>
        public void NotifyPlayEnd()
        {
            pool.Pool(this);
        }
    }
}
