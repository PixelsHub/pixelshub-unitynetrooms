using System;
using Microsoft.MixedReality.GraphicsTools;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class AvatarPainter : MonoBehaviour, IPlayerAvatarColorTarget
    {
        [Serializable]
        private class MaterialTarget
        {
            public Renderer renderer;
            public int materialIndex;
            public string property = "_MainColor";
        }

        [SerializeField]
        private MeshOutline outline;

        [SerializeField]
        private MaterialTarget[] materialTargets;

        public void ApplyPlayerColor(Color color)
        {
            outline.OutlineMaterial.color = color;

            foreach(var mt in materialTargets)
            {
                if(mt.renderer.materials.Length > mt.materialIndex)
                {
                    var mat = mt.renderer.materials[mt.materialIndex];
                    mat.SetColor(mt.property, color);
                }
                else
                    Debug.LogWarning($"Missing material at index {mt.materialIndex} on target renderer {mt.renderer}.");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if(Application.isPlaying)
                return;

            if(outline == null)
                if(!TryGetComponent(out outline))
                    outline = GetComponentInChildren<MeshOutline>();
        }
#endif
    }
}