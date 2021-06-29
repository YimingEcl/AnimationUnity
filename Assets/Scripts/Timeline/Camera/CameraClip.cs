using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;

[System.Serializable]
public class CameraClip : PlayableAsset
{
    public CameraBehaviour Behaviour = new CameraBehaviour();

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CameraBehaviour>.Create(graph, Behaviour);
    }

    [CustomEditor(typeof(CameraClip))]
    public class CameraClipEditor : Editor
    {
        public CameraClip Target = null;
        private int RecordTypeIndex = 0;

        private void Awake()
        {
            Target = (CameraClip)target;
            RecordTypeIndex = Target.Behaviour.TypeIndex;
        }

        public override void OnInspectorGUI()
        {
            Target.Behaviour.TypeIndex = EditorGUILayout.Popup("Control Type:", Target.Behaviour.TypeIndex, Target.Behaviour.GetNames());
            if (Target.Behaviour.TypeIndex != RecordTypeIndex)
            {
                RecordTypeIndex = Target.Behaviour.TypeIndex;
                Target.Behaviour.Reset();
            }

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                switch ((CameraBehaviour.Type)Target.Behaviour.TypeIndex)
                {
                    case CameraBehaviour.Type.Translate:
                        Target.Behaviour.Speed = EditorGUILayout.FloatField("Speed:", Target.Behaviour.Speed);
                        Target.Behaviour.AxisIndex = EditorGUILayout.Popup("Axis:", Target.Behaviour.AxisIndex, Target.Behaviour.AxisName);
                        break;

                    case CameraBehaviour.Type.Rotate:
                        Target.Behaviour.TargetObject = (GameObject)EditorGUILayout.ObjectField("Rotate Target:", Target.Behaviour.TargetObject, typeof(GameObject), true);
                        Target.Behaviour.Degree = EditorGUILayout.FloatField("Degree:", Target.Behaviour.Degree);
                        Target.Behaviour.AxisIndex = EditorGUILayout.Popup("Axis:", Target.Behaviour.AxisIndex, Target.Behaviour.AxisName);
                        break;

                    case CameraBehaviour.Type.Follow:
                        Target.Behaviour.TargetObject = (GameObject)EditorGUILayout.ObjectField("Follow Target:", Target.Behaviour.TargetObject, typeof(GameObject), true);
                        break;

                    case CameraBehaviour.Type.LookAt:
                        Target.Behaviour.TargetObject = (GameObject)EditorGUILayout.ObjectField("LookAt Target:", Target.Behaviour.TargetObject, typeof(GameObject), true);
                        break;
                }
            }
        }
    }
}

