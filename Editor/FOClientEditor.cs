using UnityEngine;
using UnityEditor;

namespace FishNet.FloatingOrigin
{
    [CustomEditor(typeof(FOClient))]
    [CanEditMultipleObjects]
    public class FOClientEditor : Editor
    {
        // SerializedProperty x, y, z;
        FOClient client;
        void OnEnable()
        {
            client = (target as FOClient);
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (Application.isPlaying)
            {
                if (FOManager.instance != null)
                {
                    var position = client.realPosition;
                    EditorGUILayout.BeginHorizontal("box");
                    EditorGUILayout.LabelField("Real Position: ");
                    double x = EditorGUILayout.DoubleField(position.x);
                    double y = EditorGUILayout.DoubleField(position.y);
                    double z = EditorGUILayout.DoubleField(position.z);
                    if (x != position.x || y != position.y || z != position.z)
                    {
                        client.transform.position = FOManager.instance.RealToUnity(new Vector3d(x, y, z), client.gameObject.scene);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            // if (GUILayout.Button("Set Current Position"))
            // {
            //     Debug.Log("current pos " + ((MonoBehaviour)target).transform.position.ToString());
            //     x.doubleValue = (double)((MonoBehaviour)target).transform.position.x;
            //     y.doubleValue = (double)((MonoBehaviour)target).transform.position.y;
            //     z.doubleValue = (double)((MonoBehaviour)target).transform.position.z;
            //     Debug.Log(x.doubleValue + " " + y.doubleValue + " " + z.doubleValue);
            //     serializedObject.ApplyModifiedProperties();
            //     // serializedObject.Update();
            // }
            // EditorGUILayout.BeginHorizontal();
            // if (GUILayout.Button("Move to Origin"))
            // {
            //     ((MonoBehaviour)target).transform.position = Vector3.zero;
            //     serializedObject.ApplyModifiedProperties();
            // }
            // if (GUILayout.Button("Move to to Position (Approximate, from origin)"))
            // {
            //     ((MonoBehaviour)target).transform.position = new Vector3((float)x.doubleValue, (float)y.doubleValue, (float)z.doubleValue);
            //     serializedObject.ApplyModifiedProperties();
            // }
            // EditorGUILayout.EndHorizontal();
            // EditorGUILayout.LabelField("Real Position");
            // EditorGUILayout.PropertyField(x, GUILayout.ExpandWidth(false));
            // EditorGUILayout.PropertyField(y, GUILayout.ExpandWidth(false));
            // EditorGUILayout.PropertyField(z, GUILayout.ExpandWidth(false));
        }
    }
}