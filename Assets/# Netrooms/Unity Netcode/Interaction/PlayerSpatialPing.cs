using System;
using System.Collections;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class PlayerSpatialPing : MonoBehaviour
    {
        [Serializable]
        private class ColorHandler
        {
            public float alpha = 1;
            public ColorEvent colorEvent;
        }

        public event Action<PlayerSpatialPing> OnPlayEnded;

        public bool IsPlaying { get; private set; }

        [SerializeField, Tooltip("Set <= 0 if play end will be notified externally.")]
        private float forcedDuration = -1;

        [Space(8)]
        [SerializeField]
        private ColorHandler[] colorHandlers;

        public void Play(Vector3 worldPosition, Quaternion worldRotation, NetworkPlayer player) 
        {
            IsPlaying = true;

            transform.SetPositionAndRotation(worldPosition, worldRotation);

            if(player != null)
            {
                AssignColor(player.Color);
            }
            else
            {
                AssignColor(PlayerColoringScheme.undefinedColor);
            }

            if(forcedDuration > 0)
                StartCoroutine(DurationControlUpdate());
        }

        public void EndPlay()
        {
            if(!IsPlaying)
                return;

            IsPlaying = false;
            OnPlayEnded?.Invoke(this);
        }

        private void OnDisable()
        {
            if(IsPlaying)
                EndPlay();
        }

        private void OnDestroy()
        {
            OnPlayEnded = null;
        }

        private IEnumerator DurationControlUpdate() 
        {
            yield return new WaitForSecondsRealtime(forcedDuration);
            EndPlay();
        }

        private void AssignColor(Color color)
        {
            if(colorHandlers == null)
            {
                Debug.LogWarning($"{GetType()} in object {name} does not have any color handler set.");
                return;
            }

            foreach(var colorHandler in colorHandlers)
            {
                color.a = colorHandler.alpha;
                colorHandler.colorEvent.Invoke(color);
            }
        }
    }
}