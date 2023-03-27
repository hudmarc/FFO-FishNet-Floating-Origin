using FishNet.Object;
using FishNet.Connection;
using FishNet.FloatingOrigin.Types;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    public interface IRealTransform
    {
        public Vector3d realPosition { get; set; }
    }
}