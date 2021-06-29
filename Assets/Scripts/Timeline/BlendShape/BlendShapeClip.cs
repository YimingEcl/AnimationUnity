using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class BlendShapeClip : PlayableAsset
{
    public BlendShapeBehaviour Behaviour = new BlendShapeBehaviour();
    public ExposedReference<SkinnedMeshRenderer> Mesh;
    public TimelineClip Clip;
    public Shape[] Shapes;
    public int ShapeIndex = 0;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<BlendShapeBehaviour>.Create(graph, Behaviour);
        var behaviour = playable.GetBehaviour();
        behaviour.Shapes = Shapes;
        Behaviour.FaceMesh = Mesh.Resolve(graph.GetResolver());

        return playable;
    }

    public void AddShape(Shape shape)
    {
        if (Shapes == null)
        {
            Shapes = new Shape[0];
            ArrayExtensions.Add(ref Shapes, shape);
        }
        else
            ArrayExtensions.Add(ref Shapes, shape);
    }

    public void RemoveShape(Shape shape)
    {
        if (FindShape(shape))
        {
            ArrayExtensions.Remove(ref Shapes, shape);
        }
    }

    public bool FindShape(Shape shape)
    {
        if (Shapes != null && Shapes.Length != 0)
            return ArrayExtensions.Contains(ref Shapes, shape);
        else
            return false;
    }

    public bool FindShape(int index)
    {
        if (Shapes != null && Shapes.Length != 0)
            return System.Array.Find(Shapes, x => x.Index == index) == null ? false : true;
        else
            return false;
    }

    [System.Serializable]
    public class Shape
    {
        public int Index = -1;
        public string Name = string.Empty;
        public float Weight = 0.0f;
        public TimelineClip Clip;
        public AnimationCurve Curve;

        public Shape(int index, string name, TimelineClip clip)
        {
            Index = index;
            Name = name;
            Weight = 0.0f;
            Clip = clip;
            Curve = AnimationCurve.Linear(0, 0, 1, 1);
        }

        public Shape(int index, string name, TimelineClip clip, AnimationCurve curve)
        {
            Index = index;
            Name = name;
            Weight = 0.0f;
            Clip = clip;
            Curve = curve;
        }

        public void CalculateWeight(double time)
        {
            Weight = Curve.Evaluate((float)(time / Clip.duration)) * 100.0f;
        }

        public void SetAnimationCurve(AnimationCurve curve)
        {
            Curve = curve;
        }
    }

    [CustomEditor(typeof(BlendShapeClip))]
    public class BlendShapeClipEditor : Editor
    {
        public BlendShapeClip Target;
        SerializedProperty MeshProperty;

        private void Awake()
        {
            Target = (BlendShapeClip)target;
            MeshProperty = serializedObject.FindProperty("Mesh");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(MeshProperty, new GUIContent("Skinned Mesh"));
            if (Target.Behaviour.FaceMesh == null)
                EditorGUILayout.Popup(0, new string[1] { "No BlendShape Found." });
            else
            {
                Target.ShapeIndex = EditorGUILayout.Popup(Target.ShapeIndex,
                    ArrayExtensions.Concat(new string[1] { "Add BlendShape" }, Target.Behaviour.GetShapeNames()));
                if(Target.ShapeIndex > 0)
                {
                    string name = Target.Behaviour.GetShapeName(Target.ShapeIndex - 1);
                    Shape shape = new Shape(Target.ShapeIndex - 1, name, Target.Clip);
                    if(!Target.FindShape(Target.ShapeIndex - 1))
                        Target.AddShape(shape);
                    Target.ShapeIndex = 0;
                }
            }      

            using (new EditorGUILayout.VerticalScope("Box"))
            {
                if (Target.Shapes == null || Target.Shapes.Length == 0)
                    EditorGUILayout.LabelField("No BlendShape Yet.");
                else
                {
                    for(int i = 0; i < Target.Shapes.Length; i++)
                    {                       
                        GUILayout.BeginHorizontal();
                        Target.Shapes[i].Weight = EditorGUILayout.Slider(
                            Target.Shapes[i].Name, Target.Shapes[i].Weight, 0.0f, 100.0f);
                        if (GUILayout.Button("X", GUILayout.Width(30.0f)))
                        {
                            Target.RemoveShape(Target.Shapes[i]);
                            continue;
                        };
                        GUILayout.EndHorizontal();

                        Target.Shapes[i].Curve = EditorGUILayout.CurveField("Curve", Target.Shapes[i].Curve);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(Target);
        }
    }
}
