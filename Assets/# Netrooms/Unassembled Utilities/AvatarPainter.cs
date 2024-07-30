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
        }

        [SerializeField]
        private MeshOutline outline;

        [SerializeField]
        private MaterialTarget[] materialTargets;

        public void ApplyPlayerColor(Color color)
        {
            outline.OutlineMaterial.color = color;

            foreach(var materialTarget in materialTargets)
            {
                if(materialTarget.renderer.materials.Length > materialTarget.materialIndex)
                    materialTarget.renderer.materials[materialTarget.materialIndex].color = color;
                else
                    Debug.LogWarning($"Missing material at index {materialTarget.materialIndex} on target renderer {materialTarget.renderer}.");
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