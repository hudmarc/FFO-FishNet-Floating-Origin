using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FloatingOffset.Runtime
{
    public static class Functions
    {
        const string HEX = "X";
        /// <summary>
        /// Converts the given scene handle to hex for easy display.
        /// </summary>
        /// <param name="scene">The scene to convert.</param>
        /// <returns>The scene ID in Hex code.</returns>
        public static string ToHex(this Scene scene) => Math.Abs(scene.handle).ToString(HEX);
        public static PhysicsScene Physics(this GameObject gameObject) => gameObject.scene.GetPhysicsScene();
    }
}
