using UnityEngine;

namespace PixelsHub.Netrooms
{
    public static partial class Extensions
    {
        /// <summary>
        /// Sets the transform's local scale to produce an equivalent change in lossy scale based on the given value.
        /// </summary>
        public static void SetLocalScaleToLossyEquivalent(this Transform t, Vector3 lossyScale)
        {
            Vector3 scaleFactors;
            if(t.parent != null)
                scaleFactors = new(1 / t.parent.lossyScale.x, 1 / t.parent.lossyScale.y, 1 / t.parent.lossyScale.z);
            else
                scaleFactors = Vector3.one;

            t.localScale = Vector3.Scale(lossyScale, scaleFactors);
        }
    }
}
