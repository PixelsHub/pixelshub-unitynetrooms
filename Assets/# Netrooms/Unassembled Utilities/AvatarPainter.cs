using System;
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
        private MaterialTarget[] materialTargets;

        public void ApplyPlayerColor(Color color)
        {
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
    }
}