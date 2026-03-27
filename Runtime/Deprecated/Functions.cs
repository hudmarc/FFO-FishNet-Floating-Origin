using System;
using UnityEngine;

namespace FishNet.FloatingOrigin
{
    /// <summary>
    /// Every function in this class has full test coverage.
    /// </summary>
    [Obsolete("Removed", true)]
    public class Functions
    {
        [Obsolete("Moved to Mathd", true)]
        public static Vector3 RealToUnity(Vector3d realPosition, Vector3d offset) => (Vector3)(realPosition - offset);
        [Obsolete("Moved to Mathd", true)]
        public static Vector3d UnityToReal(Vector3 unityPosition, Vector3d offset) => ((Vector3d)unityPosition) + offset;
        [Obsolete("Moved to Mathd", true)]
        public static float MaxLengthScalar(Vector3 vector) => Mathf.Max(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
        [Obsolete("Moved to Mathd", true)]
        public static double MaxLengthScalar(Vector3d vector) => Mathd.Max(Mathd.Abs(vector.x), Mathd.Abs(vector.y), Mathd.Abs(vector.z));
    }
}
