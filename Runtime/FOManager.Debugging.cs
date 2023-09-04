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
                Debug.Log($"[{InstanceFinder.TimeManager.Tick}] {message}");
            else
                Debug.Log($"[{category}    {InstanceFinder.TimeManager.Tick}] {message}");
#endif
        }
        bool hideDebug = false;
#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!enabled || offsetGroups.Count < 1)
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
            GUILayout.Button($"Groups: {offsetGroups.Count} Tracked Groups: {groups.Count} Clients: {clients.Count} Objects: {objects.Count} Queued Groups: {queuedGroups.Count}");
            foreach (var val in offsetGroups)
            {
                GUILayout.Button($" Scene {val.Key.handle}: {val.Value.offset} O: {val.Value.clients.Count} o: {val.Value.clients.Count}");
            }
            if (GUILayout.Button("Toggle FO Debug"))
            {
                hideDebug = !hideDebug;
            }
            if (hideDebug)
                return;

            if (queuedGroups.Count > 100)
            {
                Debug.Break();
                return;
            }
            foreach (var val in queuedGroups)
            {
                GUILayout.Button($" Queued: {Math.Abs(val.scene.handle):X}");
            }
            foreach (var client in clients)
            {
                if (client != null)
                    GUILayout.Button($"Object: {client.networking.ObjectId} Owner: {client.networking.OwnerId} Unity: {(int)client.transform.position.x} {(int)client.transform.position.y} {(int)client.transform.position.z} Real: {(int)client.realPosition.x} {(int)client.realPosition.y} {(int)client.realPosition.z}\n Members: {offsetGroups[client.gameObject.scene].clients.Count} Handle: {Math.Abs(client.gameObject.scene.handle):X}");
            }

        }

    }
}