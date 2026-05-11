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
                        Vector3d offset = offsetTransform.universe.server.GetSceneOffset(offsetTransform.gameObject.scene);
                        var position = offset + Mathd.toVector3d(offsetTransform.gameObject.transform.position);
                        EditorGUILayout.BeginHorizontal("box");
                        EditorGUILayout.LabelField("Real Position: ");
                        double x = EditorGUILayout.DoubleField(position.x);
                        double y = EditorGUILayout.DoubleField(position.y);
                        double z = EditorGUILayout.DoubleField(position.z);
                        if (x != position.x || y != position.y || z != position.z)
                        {
                            offsetTransform.transform.position = Mathd.RealToUnity(new Vector3d(x, y, z), offset);
                        }

                        if(offsetTransform.GetType() == typeof(OffsetTransform))
                        {
                            EditorGUILayout.LabelField($"State: {offsetTransform.universe.server.GetState((OffsetTransform)offsetTransform).ToString()}");
                        }

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal("box");
                        // EditorGUILayout.LabelField(FOManager.instance.GetDebugText(offsetTransform));
                        EditorGUILayout.EndHorizontal();
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