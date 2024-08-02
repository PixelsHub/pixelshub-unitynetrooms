using UnityEngine;
using Microsoft.MixedReality.GraphicsTools;

namespace PixelsHub.Netrooms
{
    public class PlayerAvatarColorPainterForOutline : PlayerAvatarColorPainter
    {
        [SerializeField]
        private BaseMeshOutline outline;

        protected override void SetColor(Color color)
        {
            if(outline is IMeshOutlineIndividual individual)
                individual.SetOutlineIndividualColor(color);
            else
            {
                Debug.Assert(false, "Instanced outline should be used for coloring.");
                outline.OutlineMaterial.color = color;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            if(Application.isPlaying)
                return;

            if(outline == null && !TryGetComponent(out outline))
                outline = GetComponentInChildren<BaseMeshOutline>();
        }
    }
}