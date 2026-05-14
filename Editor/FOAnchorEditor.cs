using UnityEngine;
using UnityEditor;
using FloatingOffset.Runtime;

namespace FloatingOffset.Editor
{
    [CustomEditor(typeof(OffsetAnchor), true)]
    [CanEditMultipleObjects]
    public class FOAnchorEditor : UnityEditor.Editor
    {
        OffsetAnchor anchor;
        void OnEnable()
        {
            anchor = target as OffsetAnchor;
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (Application.isPlaying)
            {
                if (anchor.universe.server != null)
                {
                    if (anchor.universe.server.HasScene(anchor.gameObject.scene))
                    {
                        var position = anchor.realPosition;
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField("Real Position: ");
                        double x = EditorGUILayout.DoubleField(position.x);
                        double y = EditorGUILayout.DoubleField(position.y);
                        double z = EditorGUILayout.DoubleField(position.z);
                        if (x != position.x || y != position.y || z != position.z)
                        {
                            anchor.SetRealPosition(new Vector3d(x, y, z));
                        }

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.LabelField("View is not in valid Scene!");
                    }
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal("box");
                if (GUILayout.Button("Anchor Here"))
                {
                    anchor.SetRealPosition(anchor.universe.server.GetSceneOffset(anchor.gameObject.scene) + Mathd.toVector3d(anchor.transform.position));
                }
                if (GUILayout.Button("Reset Anchor"))
                {
                    anchor.SetRealPosition(Vector3d.zero);
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}