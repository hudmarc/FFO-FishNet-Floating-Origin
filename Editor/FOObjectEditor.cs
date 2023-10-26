using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace FishNet.FloatingOrigin
{
    [CustomEditor(typeof(FOObject), true)]
    [CanEditMultipleObjects]
    public class FOObjectEditor : Editor
    {
        FOObject foobject;
        void OnEnable()
        {
            foobject = target as FOObject;
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (Application.isPlaying)
            {
                if (FOManager.instance != null && !InstanceFinder.IsClientOnly)
                {
                    if (FOManager.instance.IsGroup(foobject.gameObject.scene))
                    {
                        var position = foobject.realPosition;
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField("Real Position: ");
                        double x = EditorGUILayout.DoubleField(position.x);
                        double y = EditorGUILayout.DoubleField(position.y);
                        double z = EditorGUILayout.DoubleField(position.z);
                        if (x != position.x || y != position.y || z != position.z)
                        {
                            foobject.transform.position = FOManager.instance.RealToUnity(new Vector3d(x, y, z), foobject.gameObject.scene);
                        }

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField(FOManager.instance.GetDebugText(foobject));
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
                    foobject.anchoredPosition = (Vector3d)foobject.transform.position;
                }
                if (GUILayout.Button("Reset Anchor"))
                {
                    foobject.anchoredPosition = Vector3d.zero;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}