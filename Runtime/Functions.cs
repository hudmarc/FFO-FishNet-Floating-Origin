using UnityEngine;

namespace FishNet.FloatingOrigin
{
    /// <summary>
    /// Every function in this class has full test coverage.
    /// </summary>
    public class Functions
    {
        public static Vector3 RealToUnity(Vector3d realPosition, Vector3d offset) => (Vector3)(realPosition - offset);
        public static Vector3d UnityToReal(Vector3 unityPosition, Vector3d offset) => ((Vector3d)unityPosition) + offset;

        public static float MaxLengthScalar(Vector3 vector) => Mathf.Max(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));

        public static double MaxLengthScalar(Vector3d vector) => Mathd.Max(Mathd.Abs(vector.x), Mathd.Abs(vector.y), Mathd.Abs(vector.z));
    }
}
