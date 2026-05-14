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
        Vector3d target_position = Vector3d.zero;
        void OnEnable()
        {
            offsetTransform = target as OffsetBehaviour;
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

                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField($"Real Position <{position.x},{position.y},{position.z}> ");
                        double x = EditorGUILayout.DoubleField(position.x);
                        double y = EditorGUILayout.DoubleField(position.y);
                        double z = EditorGUILayout.DoubleField(position.z);
                        if (x != position.x || y != position.y || z != position.z)
                        {
                            target_position = new Vector3d(x, y, z);

                        }

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.EndHorizontal();
                        if (GUILayout.Button("Teleport"))
                        {
                            offsetTransform.universe.TeleportTo((OffsetTransform) offsetTransform, target_position);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("View is not in valid Scene!");
                    }
                }
            }
        }
    }
}