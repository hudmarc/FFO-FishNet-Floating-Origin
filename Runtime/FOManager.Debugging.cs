#if UNITY_EDITOR

using UnityEngine;
namespace FishNet.FloatingOrigin
{
    public partial class FOManager
    {
        public void DrawDebug()
        {
            foreach (var val in FOGroups)
            {
                GUILayout.Button($" Scene {val.Key.handle.ToString()}: {val.Value.offset} {val.Value.members}");
            }
            foreach (var ob in observers)
            {
                if (ob != null)
                    GUILayout.Button($"Owner: {ob.OwnerId} Unity Position: {(int)ob.unityPosition.x} {(int)ob.unityPosition.y} {(int)ob.unityPosition.z} Real Position: {(int)ob.realPosition.x} {(int)ob.realPosition.y} {(int)ob.realPosition.z} Group Offset: {(int)ob.groupOffset.x} {(int)ob.groupOffset.y} {(int)ob.groupOffset.z} Group Members: {FOGroups[ob.gameObject.scene].members}");
            }
        }

    }
}

#endif
