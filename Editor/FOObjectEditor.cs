using UnityEngine;
using UnityEditor;
using FloatingOffset.Runtime;

namespace FloatingOffset.Editor
{
    [CustomEditor(typeof(OffsetBehaviour), true)]
    [CanEditMultipleObjects]
    public class OffsetBehaviourEditor : UnityEditor.Editor
    {
        OffsetBehaviour offsetTransform;

        // Storing as a standard Vector3 allows us to use Unity's native Vector3Field, 
        // which supports right-click Copy/Paste.
        Vector3 target_position = Vector3.zero;
        bool isTargetInitialized = false;

        void OnEnable()
        {
            offsetTransform = target as OffsetBehaviour;
            isTargetInitialized = false; // Reset initialization on selection
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (Application.isPlaying)
            {
                if (offsetTransform.universe)
                {
                    if (offsetTransform.universe.server.HasScene(offsetTransform.gameObject.scene))
                    {
                        Vector3d position = offsetTransform.universe.server.GetSceneOffset(offsetTransform.gameObject.scene) + Mathd.toVector3d(offsetTransform.transform.position);

                        EditorGUILayout.BeginVertical("box");

                        // Display the real position as a read-only label
                        EditorGUILayout.LabelField($"Real Position <{position.x:F3}, {position.y:F3}, {position.z:F3}>");

                        // Initialize the target position to the current position once when viewed
                        if (!isTargetInitialized)
                        {
                            target_position = new Vector3((float)position.x, (float)position.y, (float)position.z);
                            isTargetInitialized = true;
                        }

                        // This native field retains its own state and supports right-click copy/paste
                        target_position = EditorGUILayout.Vector3Field("Teleport Target", target_position);

                        if (GUILayout.Button("Teleport"))
                        {
                            // Convert the float Vector3 back to your double precision Vector3d
                            Vector3d targetPositionDouble = new Vector3d(target_position.x, target_position.y, target_position.z);
                            offsetTransform.universe.TeleportTo((OffsetTransform)offsetTransform, targetPositionDouble);
                        }

                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("View is not in valid Scene!");
                    }
                }
            }
        }
    }


    // [InitializeOnLoad] ensures the static constructor runs when the editor opens or compiles
    [InitializeOnLoad]
    public static class RootTransformLabelInjector
    {
        // Static state variables to prevent live-overwriting of user input
        private static Vector3 targetPosition;
        private static int lastTargetId = -1;

        static RootTransformLabelInjector()
        {
            // Subscribe to the global component header drawing event
            UnityEditor.Editor.finishedDefaultHeaderGUI += OnPostHeaderGUI;
        }

        private static void OnPostHeaderGUI(UnityEditor.Editor editor)
        {
            // 1. Restrict this injection strictly to Transform components
            Transform t = editor.target as Transform;
            if (t == null) return;

            // 2. Restrict to root transforms only
            if (t.parent != null) return;

            if (!Application.isPlaying) return;

            // 3. Ensure the object is part of your system
            OffsetBehaviour offsetTransform = t.GetComponent<OffsetBehaviour>();
            if (offsetTransform == null || offsetTransform.universe == null) return;

            if (offsetTransform.universe.server.HasScene(offsetTransform.gameObject.scene))
            {
                // Calculate the real position
                Vector3d position = offsetTransform.universe.server.GetSceneOffset(offsetTransform.gameObject.scene) + Mathd.toVector3d(t.position);

                // Re-initialize the input field ONLY if the user selected a new transform
                if (lastTargetId != t.GetInstanceID())
                {
                    targetPosition = new Vector3((float)position.x, (float)position.y, (float)position.z);
                    lastTargetId = t.GetInstanceID();
                }

                // Draw the custom GUI right below the Transform header
                EditorGUILayout.BeginVertical("helpbox");

                EditorGUILayout.LabelField($"Real Universe Pos: <{position.x:F3}, {position.y:F3}, {position.z:F3}>", EditorStyles.miniBoldLabel);

                // Keep the copy/paste functionality
                targetPosition = EditorGUILayout.Vector3Field("Teleport Target", targetPosition);

                if (GUILayout.Button("Teleport"))
                {
                    Vector3d targetPositionDouble = new Vector3d(targetPosition.x, targetPosition.y, targetPosition.z);
                    offsetTransform.universe.TeleportTo((OffsetTransform)offsetTransform, targetPositionDouble);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(); // Add a small buffer before the standard Transform fields begin
            }
        }

    }
}