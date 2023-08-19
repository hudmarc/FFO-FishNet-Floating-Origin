using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.FloatingOrigin
{
    public partial class FOManager
    {
        public Vector3 RealToUnity(Vector3d realPosition, Scene scene) => Functions.RealToUnity(realPosition, offsetGroups[scene].offset);

        public Vector3d UnityToReal(Vector3 unityPosition, Scene scene) => Functions.UnityToReal(unityPosition, offsetGroups[scene].offset);

    }
}