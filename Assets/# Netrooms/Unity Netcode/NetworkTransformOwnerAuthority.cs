using UnityEngine;
using Unity.Netcode.Components;

namespace PixelsHub.Netrooms
{
    public class NetworkTransformOwnerAuthority : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;

    }
}
