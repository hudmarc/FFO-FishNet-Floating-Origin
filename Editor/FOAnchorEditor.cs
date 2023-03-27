using UnityEngine;
using UnityEditor;

namespace FishNet.FloatingOrigin
{
    [CustomEditor(typeof(FOAnchor))]
    [CanEditMultipleObjects]
    public class FOAnchorEditor : Editor
    {
        SerializedProperty x, y, z;

        void OnEnable()
        {
            x = serializedObject.FindProperty("x");
            y = serializedObject.FindProperty("y");
            z = serializedObject.FindProperty("z");
        }
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Set Current Position"))
            {
                Debug.Log("current pos " + ((MonoBehaviour)target).transform.position.ToString());
                x.doubleValue = (double)((MonoBehaviour)target).transform.position.x;
                y.doubleValue = (double)((MonoBehaviour)target).transform.position.y;
                z.doubleValue = (double)((MonoBehaviour)target).transform.position.z;
                Debug.Log(x.doubleValue + " " + y.doubleValue + " " + z.doubleValue);
                serializedObject.ApplyModifiedProperties();
                // serializedObject.Update();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Move to Origin"))
            {
                ((MonoBehaviour)target).transform.position = Vector3.zero;
                serializedObject.ApplyModifiedProperties();
            }
            if (GUILayout.Button("Move to to Position (Approximate, from origin)"))
            {
                ((MonoBehaviour)target).transform.position = new Vector3((float)x.doubleValue, (float)y.doubleValue, (float)z.doubleValue);
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField("Real Position");
            EditorGUILayout.PropertyField(x, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(y, GUILayout.ExpandWidth(false));
            EditorGUILayout.PropertyField(z, GUILayout.ExpandWidth(false));
        }
    }
}
