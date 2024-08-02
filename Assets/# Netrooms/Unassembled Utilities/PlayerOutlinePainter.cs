using UnityEngine;
using Microsoft.MixedReality.GraphicsTools;

namespace PixelsHub.Netrooms
{
    public class PlayerOutlinePainter : MonoBehaviour
    {
        [SerializeField]
        private PlayerAvatar avatar;

        [SerializeField]
        private BaseMeshOutline outline;

        private void Awake()
        {
            if(avatar == null)
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
        }

        private void OnEnable()
        {
            if(avatar.Player == null)
                return;

            SetOutlineColor(avatar.Player.Color);
            avatar.OnPlayerColorChanged += SetOutlineColor;
        }

        private void OnDisable()
        {
            avatar.OnPlayerColorChanged -= SetOutlineColor;
        }

        private void SetOutlineColor(Color color)
        {
            outline.OutlineMaterial.color = color;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(outline == null && !TryGetComponent(out outline))
                outline = GetComponentInChildren<BaseMeshOutline>();
        }
#endif
    }
}