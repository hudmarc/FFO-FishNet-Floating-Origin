

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

                // Vector3[] corners = { new Vector3(REBASE_CRITERIA, 10, REBASE_CRITERIA), new Vector3(REBASE_CRITERIA, 10, -REBASE_CRITERIA), new Vector3(-REBASE_CRITERIA, 10, -REBASE_CRITERIA), new Vector3(-REBASE_CRITERIA, 10, REBASE_CRITERIA) };


                Vector3 difference = RealToUnity(val.offset, first.scene);

                // Debug.DrawLine(corners[0] + difference, corners[1] + difference, Color.green);
                // Debug.DrawLine(corners[1] + difference, corners[2] + difference, Color.green);
                // Debug.DrawLine(corners[2] + difference, corners[3] + difference, Color.green);
                // Debug.DrawLine(corners[3] + difference, corners[0] + difference, Color.green);
                if (first == val)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.white;
                Gizmos.DrawWireCube(difference, Vector3.one * REBASE_CRITERIA * 2);
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(difference, Vector3.one * MERGE_CRITERIA * 2);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(difference, Vector3.one * (REBASE_CRITERIA + HYSTERESIS) * 2);
                // Vector3[] corners_merge = { new Vector3(MERGE_CRITERIA, 10, MERGE_CRITERIA), new Vector3(MERGE_CRITERIA, 10, -MERGE_CRITERIA), new Vector3(-MERGE_CRITERIA, 10, -MERGE_CRITERIA), new Vector3(-MERGE_CRITERIA, 10, MERGE_CRITERIA) };
                // Debug.DrawLine(corners_merge[0] + difference, corners_merge[1] + difference, Color.magenta);
                // Debug.DrawLine(corners_merge[1] + difference, corners_merge[2] + difference, Color.magenta);
                // Debug.DrawLine(corners_merge[2] + difference, corners_merge[3] + difference, Color.magenta);
                // Debug.DrawLine(corners_merge[3] + difference, corners_merge[0] + difference, Color.magenta);
            }
        }
#endif
        public void DrawDebug()
        {
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
                GUILayout.Button($" Queued: {val.scene.handle}");
            }
            foreach (var client in clients)
            {
                if (client != null)
                    GUILayout.Button($"Object: {client.networking.ObjectId} Owner: {client.networking.OwnerId} Unity: {(int)client.transform.position.x} {(int)client.transform.position.y} {(int)client.transform.position.z} Real: {(int)client.realPosition.x} {(int)client.realPosition.y} {(int)client.realPosition.z}\n Members: {offsetGroups[client.gameObject.scene].clients.Count} Handle: {client.gameObject.scene.handle}");
            }

        }

    }
}