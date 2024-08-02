using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class PlayerAvatarXRController : MonoBehaviour
    {
        protected const float poseInterpolationTime = 0.12f;

        private static readonly int pinchBaseColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int pinchRimColorProperty = Shader.PropertyToID("_RimColor");

        [SerializeField]
        protected Transform avatarHead;

        [SerializeField]
        private GameObject rootObject;

        [SerializeField]
        private Transform poseTarget;

        [Header("Render")]
        [SerializeField]
        private Renderer[] mainRenderers;

        [SerializeField]
        private Renderer pinchRenderer;

        private Vector3 startPosePosition;
        private Vector3 endWristPosition;
        private Quaternion startWristRotation;
        private Quaternion endWristRotation;
        private float wristInterpolationTimer;

        public void SetActive(bool isVisible)
        {
            if(rootObject.activeSelf != isVisible)
                rootObject.SetActive(isVisible);
        }

        public void SetPose(Vector3 relativePosition, Quaternion relativeRotation, bool interpolate = true)
        {
            Vector3 position = avatarHead.TransformPoint(relativePosition);
            Quaternion rotation = avatarHead.rotation * relativeRotation;

            if(IsPoseBelowThreshold(position, rotation))
                return;

            endWristPosition = position;
            endWristRotation = rotation;

            if(interpolate)
            {
                startPosePosition = poseTarget.position;
                startWristRotation = poseTarget.rotation;
                wristInterpolationTimer = 0;
            }
            else
            {
                wristInterpolationTimer = poseInterpolationTime;
                poseTarget.SetPositionAndRotation(position, rotation);
            }
        }

        public virtual void SetColor(Color color)
        {
            foreach(var r in mainRenderers)
                r.material.color = color;

            pinchRenderer.material.SetColor(pinchRimColorProperty, color);

            color.a = 0.5f;
            pinchRenderer.material.SetColor(pinchBaseColorProperty, color);
        }

        protected virtual void Update()
        {
            UpdatePoseInterpolation();
        }

        private bool IsPoseBelowThreshold(Vector3 position, Quaternion rotation)
        {
            return Vector3.Distance(position, poseTarget.position) < 0.002f
                && Quaternion.Angle(rotation, poseTarget.rotation) < 0.15f;
        }

        private void UpdatePoseInterpolation()
        {
            if(wristInterpolationTimer <= poseInterpolationTime)
            {
                wristInterpolationTimer += Time.unscaledDeltaTime;

                float t = wristInterpolationTimer / poseInterpolationTime;

                poseTarget.SetPositionAndRotation
                (
                    Vector3.Lerp(startPosePosition, endWristPosition, t),
                    Quaternion.Slerp(startWristRotation, endWristRotation, t)
                );
            }
        }
    }
}
