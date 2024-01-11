using UnityEngine;
using UnityEditor;

namespace FishNet.FloatingOrigin
{
    [CustomEditor(typeof(FOAnchor), true)]
    [CanEditMultipleObjects]
    public class FOAnchorEditor : Editor
    {
        FOAnchor anchor;
        void OnEnable()
        {
            anchor = target as FOAnchor;
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (Application.isPlaying)
            {
                if (FOManager.instance != null && !InstanceFinder.IsClientOnly)
                {
                    if (FOManager.instance.IsGroup(anchor.gameObject.scene))
                    {
                        var position = anchor.anchoredPosition;
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField("Real Position: ");
                        double x = EditorGUILayout.DoubleField(position.x);
                        double y = EditorGUILayout.DoubleField(position.y);
                        double z = EditorGUILayout.DoubleField(position.z);
                        if (x != position.x || y != position.y || z != position.z)
                        {
                            anchor.transform.position = FOManager.instance.RealToUnity(new Vector3d(x, y, z), anchor.gameObject.scene);
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
                    anchor.anchoredPosition = (Vector3d)anchor.transform.position;
                }
                if (GUILayout.Button("Reset Anchor"))
                {
                    anchor.anchoredPosition = Vector3d.zero;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}