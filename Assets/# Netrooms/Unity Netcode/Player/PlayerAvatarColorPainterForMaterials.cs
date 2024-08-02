using System;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class PlayerAvatarColorPainterForMaterials : PlayerAvatarColorPainter
    {
        [Serializable]
        private class MaterialTarget
        {
            public Renderer renderer;
            public int materialIndex;

            public string property = "_MainColor";

            [Range(0, 1)]
            public float colorAlpha = 1;
        }

        [SerializeField]
        private MaterialTarget[] materialTargets;

        protected override void SetColor(Color color)
        {
            foreach(var mt in materialTargets)
            {
                color.a = mt.colorAlpha;
                mt.renderer.materials[mt.materialIndex].SetColor(mt.property, color);
            }
        }
    }
}
