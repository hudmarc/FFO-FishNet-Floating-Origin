using System;
using FishNet.FloatingOrigin.Types;
using UnityEngine;
namespace FishNet.FloatingOrigin
{
    public partial class FOManager
    {
        [SerializeField] bool loggingEnabled = false;

        private void Log(object message, string category = null)
        {
#if UNITY_EDITOR
            if (!loggingEnabled)
                return;
            if (category == null)
                Debug.Log($"[{InstanceFinder.TimeManager?.Tick}] {message}");
            else
                Debug.Log($"[{category} : {InstanceFinder.TimeManager?.Tick}] {message}");
#endif
        }
        bool hideDebug = false;
#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!hideDebug || !enabled || offsetGroups.Count < 1 || InstanceFinder.IsClientOnly || InstanceFinder.IsServerOnly)
                return;
            DrawDebug();
        }
        private void OnDrawGizmos()
        {
            OffsetGroup first = null;
            foreach (var val in offsetGroups.Values)
            {
                if (first == null)
                {
                    first = val;
                }

                Vector3 difference = RealToUnity(val.offset, first.scene);

                if (first == val)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.white;
                Gizmos.DrawWireCube(difference, Vector3.one * REBASE_CRITERIA * 2);
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(difference, Vector3.one * MERGE_CRITERIA * 2);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(difference, Vector3.one * (REBASE_CRITERIA + HYSTERESIS) * 2);
            }
        }
#endif
        public void DrawDebug()
        {
            GUILayout.Button($"Groups: {offsetGroups.Count} Tracked Groups: {groups.Count} Clients: {views.Count} Objects: {objects.Count} Queued Groups: {queuedGroups.Count}");
            foreach (var val in offsetGroups)
            {
                GUILayout.Button($" Scene {val.Key.ToHex()}: {val.Value.offset} O: {val.Value.views.Count} o: {val.Value.views.Count}");
            }
            if (GUILayout.Button("Toggle FO Debug"))
            {
                hideDebug = !hideDebug;
            }
            if (hideDebug)
                return;

            foreach (var val in queuedGroups)
            {
                GUILayout.Button($" Queued: {val.scene.ToHex()}");
            }
        }
        public String GetDebugText(FOObject foo)
        {
            return $"Unity: {(int)foo.transform.position.x} {(int)foo.transform.position.y} {(int)foo.transform.position.z} Real: {(int)foo.realPosition.x} {(int)foo.realPosition.y} {(int)foo.realPosition.z} Members: {offsetGroups[foo.gameObject.scene].views.Count} Handle: {foo.gameObject.scene.ToHex()} Grid: {objects.Quantize(foo.realPosition)}";
        }
    }
}