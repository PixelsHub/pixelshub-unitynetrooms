using UnityEngine;

namespace PixelsHub.Netrooms
{
    public abstract class PlayerAvatarColorPainter : MonoBehaviour
    {
        [SerializeField]
        private PlayerAvatar avatar;

        private void Awake()
        {
            if(avatar == null)
                FindAvatarReference();
        }

        private void OnEnable()
        {
            if(avatar.Player != null)
                SetColor(avatar.Player.Color);

            avatar.OnPlayerColorChanged += SetColor;
        }

        private void OnDisable()
        {
            avatar.OnPlayerColorChanged -= SetColor;
        }

        protected abstract void SetColor(Color color);

        private void FindAvatarReference()
        {
            var parent = transform.parent;

            while(parent != null)
            {
                if(parent.TryGetComponent(out avatar))
                    break;
                else
                    parent = parent.parent;
            }
        }

        protected virtual void OnValidate()
        {
            if(Application.isPlaying)
                return;

            if(avatar == null)
                FindAvatarReference();
        }
    }
}
