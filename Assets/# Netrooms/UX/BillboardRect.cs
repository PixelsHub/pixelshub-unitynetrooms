using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PixelsHub.XR;

namespace PixelsHub.Netrooms.UX
{
    [RequireComponent(typeof(RectTransform))]
    public class BillboardRect : MonoBehaviour
    {
        public static Transform cameraTransform;

        private static IEnumerator updateCoroutine;

        private static readonly List<BillboardRect> cache = new(12);

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
        }

        private void OnEnable()
        {
            cache.Add(this);

            if(updateCoroutine == null)
            {
                updateCoroutine = Immersiveness.IsActive ? UpdateCoroutineImmersive() : UpdateCoroutineDefault();
                StaticCoroutineRunner.Start(updateCoroutine);
            }
        }

        private void OnDisable()
        {
            cache.Remove(this);
        }

        private static IEnumerator UpdateCoroutineImmersive() 
        {
            while(cache.Count > 0)
            {
                while(cameraTransform == null)
                {
                    cameraTransform = Camera.main != null ? Camera.main.transform : null;
                    yield return null;
                }

                for(int i = 0; i < cache.Count; i++)
                    cache[i].SetRotationImmersive();

                yield return null;
            }
        }

        private static IEnumerator UpdateCoroutineDefault()
        {
            while(cache.Count > 0)
            {
                while(cameraTransform == null)
                {
                    cameraTransform = Camera.main != null ? Camera.main.transform : null;
                    yield return null;
                }

                for(int i = 0; i < cache.Count; i++)
                    cache[i].SetRotationDefault();

                yield return null;
            }
        }

        private void SetRotationDefault()
        {
            rectTransform.rotation = Quaternion.LookRotation(cameraTransform.forward, cameraTransform.up);
        }

        private void SetRotationImmersive()
        {
            Vector3 toCamera = (CalculateMiddlePoint() - cameraTransform.position).normalized;
            rectTransform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
        }

        private Vector3 CalculateMiddlePoint()
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            return Vector3.Lerp(corners[0], corners[2], 0.5f);
        }
    }
}