using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class AutoGenerate : MonoBehaviour
{
    public float Min = 0.0f;
    public float Max = 1.0f;
    public double interval = 3.0d;

    private LabelTrack AiueoTrack = null;
    private LabelTrack BlinkTrack = null;
    private BlendShapeTrack AiueoShapeTrack = null;
    private BlendShapeTrack BlinkShapeTrack = null;
    private BlendShapeTrack UpperTeethShapeTracK = null;
    private BlendShapeTrack ButtomTeethShapeTracK = null;

    private PlayableDirector Director = null;
    private TimelineAsset MyTimelineAsset = null;

    private void Awake()
    {
        Initial();
    }

    public void OnClick()
    {
        if (Director == null)
            Initial();

        GenerateBlendShape();
    }

    private void GenerateBlendShape()
    {
        var clips = AiueoTrack.GetClips();

        foreach (var clip in clips)
        {
            LabelClip clipAsset = clip.asset as LabelClip;
            char[] labels = clipAsset.Label.ToCharArray();
            AnimationCurve[] curves = new AnimationCurve[0];

            for (int i = 0; i < 5; i++)
                ArrayExtensions.Add(ref curves, AnimationCurveExtensions.ZeroCurve());

            int begin = 0;
            int size = 1;
            for (int end = 0; end < labels.Length; end++)
            {
                if (end < labels.Length - 1 && labels[end] == labels[end + 1])
                    size++;
                else
                {
                    switch (labels[end])
                    {
                        case 'a':
                        case 'A':
                            AddKey(curves[0], clipAsset, begin, end, size);
                            break;
                        case 'i':
                        case 'I':
                            AddKey(curves[1], clipAsset, begin, end, size);
                            break;
                        case 'u':
                        case 'U':
                            AddKey(curves[2], clipAsset, begin, end, size);
                            break;
                        case 'e':
                        case 'E':
                            AddKey(curves[3], clipAsset, begin, end, size);
                            break;
                        case 'o':
                        case 'O':
                            AddKey(curves[4], clipAsset, begin, end, size);
                            break;
                    }
                    begin = end + 1;
                    size = 1;
                }
            }

            float min = clipAsset.Min;
            for (int i = 0; i < curves.Length; i++)
                PostProcess(ref curves[i], min);

            AddBlendShapeClip(clip.start, clip.duration, AiueoShapeTrack, curves);
            AddBlendShapeClip(clip.start, clip.duration, UpperTeethShapeTracK, curves);
            AddBlendShapeClip(clip.start, clip.duration, ButtomTeethShapeTracK, curves);
        }

        clips = BlinkTrack.GetClips();
        foreach (var clip in clips)
        {
            LabelClip clipAsset = clip.asset as LabelClip;
            AnimationCurve[] curves = new AnimationCurve[1] { AnimationCurveExtensions.UCurve(0.5f, 0.0f, clipAsset.Min, clipAsset.Max) };
            AddBlendShapeClip(clip.start, clip.duration, BlinkShapeTrack, curves);
        }
    }

    //private GenerateActivation()
    //{

    //}

    private void Initial()
    {
        Director = gameObject.GetComponent<PlayableDirector>();
        MyTimelineAsset = Director.playableAsset as TimelineAsset;

        AiueoTrack = GetTrack("AIUEO Label") as LabelTrack;
        BlinkTrack = GetTrack("Blink Label") as LabelTrack;
        AiueoShapeTrack = GetTrack("AIUEO") as BlendShapeTrack;
        BlinkShapeTrack = GetTrack("Blink") as BlendShapeTrack;
        UpperTeethShapeTracK = GetTrack("UpperTeeth") as BlendShapeTrack;
        ButtomTeethShapeTracK = GetTrack("ButtomTeeth") as BlendShapeTrack;
    }

    private void AddKey(AnimationCurve curve, LabelClip clip, int begin, int end, int size)
    {
        char[] labels = clip.Label.ToCharArray();
        float duration = 1.0f / labels.Length;

        if (curve.length == 2)
            curve.AddKey(new Keyframe(duration * begin, 0, 0, 0));
        else
            curve.AddKey(new Keyframe(duration * begin, clip.Min, 0, 0));

        curve.AddKey(new Keyframe(duration * begin + duration * 0.5f, clip.Max, 0, 0));

        if (size > 1)
            curve.AddKey(new Keyframe(duration * (begin + size) - duration * 0.5f, clip.Max, 0, 0));

        if (end < labels.Length - 1)
            curve.AddKey(new Keyframe(duration * (begin + size), clip.Min, 0, 0));
    }

    private TrackAsset GetTrack(string name)
    {
        return MyTimelineAsset.GetOutputTracks().ToList().Find(x => x.name == name);
    }

    private void PostProcess(ref AnimationCurve curve, float min)
    {
        if(curve.keys[curve.length - 2].value == min)
        {
            float time = curve.keys[curve.length - 2].time;
            curve.RemoveKey(curve.length - 2);
            curve.AddKey(new Keyframe(time, 0, 0, 0));
        }
    }

    private void AddBlendShapeClip(double start, double duration, BlendShapeTrack track, AnimationCurve[] curves)
    {
        TimelineClip timelineClip = track.CreateDefaultClip();
        timelineClip.start = start;
        timelineClip.duration = duration;
        BlendShapeClip clipAsset = timelineClip.asset as BlendShapeClip;
        PlayableGraph graph = Director.playableGraph;
        string[] name = new string[5] { "A", "I", "U", "E", "O" };

        if (track.name == "AIUEO")
            graph.GetResolver().SetReferenceValue(clipAsset.Mesh.exposedName, GameObject.Find("TKVR01_001_CH_Mesh_Face"));
        if (track.name == "UpperTeeth")
            graph.GetResolver().SetReferenceValue(clipAsset.Mesh.exposedName, GameObject.Find("TKVR01_001_CH_Mesh_Teeth_upper"));
        if (track.name == "ButtomTeeth")
            graph.GetResolver().SetReferenceValue(clipAsset.Mesh.exposedName, GameObject.Find("TKVR01_001_CH_Mesh_Teeth_buttom"));

        if (track.name == "Blink")
            clipAsset.AddShape(new BlendShapeClip.Shape(8, "BlinkBoth", timelineClip, curves[0]));
        else if (track.name == "AIUEO")
        {
            int startIndex = 11;
            for(int i = 0; i < 5; i++)
                clipAsset.AddShape(new BlendShapeClip.Shape(startIndex + i, name[i], timelineClip, curves[i]));
        }
        else
        {
            int startIndex = 8;
            for (int i = 0; i < 5; i++)
                clipAsset.AddShape(new BlendShapeClip.Shape(startIndex + i, name[i], timelineClip, curves[i]));
        }
    }

    [CustomEditor(typeof(AutoGenerate))]
    public class AutoGenerateEditor : Editor
    {
        public AutoGenerate Target;

        private void Awake()
        {
            Target = target as AutoGenerate;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Generate"))
            {
                Target.OnClick();
            }
        }
    }
}


/* old function
 * 
public void OnClick()
{
    Director = GetComponent<PlayableDirector>();
    MyTimelineAsset = Director.playableAsset as TimelineAsset;
    LabelTrack aiueoTrack = GetTrack("AIUEO Label") as LabelTrack;
    LabelTrack blinkTrack = GetTrack("Blink Label") as LabelTrack;
    BlendShapeTrack aiueoShapeTrack = GetTrack("AIUEO") as BlendShapeTrack;
    BlendShapeTrack blinkShapeTrack = GetTrack("Blink") as BlendShapeTrack;
    BlendShapeTrack upperTeethShapeTracK = GetTrack("UpperTeeth") as BlendShapeTrack;
    BlendShapeTrack buttomTeethShapeTracK = GetTrack("ButtomTeeth") as BlendShapeTrack;

    var clips = aiueoTrack.GetClips();
    foreach(var clip in clips)
    {
        LabelClip clipAsset = clip.asset as LabelClip;
        char[] labels = clipAsset.Label.ToCharArray();
        double clipDuration = clip.duration / labels.Length;

        float duration = 1.0f / labels.Length;
        AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));

        int size = 1;
        int index = 0;
        for(int i = 0; i < labels.Length; i++)
        {
            if (i < labels.Length - 1 && labels[i] == labels[i + 1])
                size++;
            else
            {
                int type = -1;
                switch (labels[i])
                {
                    case 'a':
                    case 'A':
                        type = 1; break;
                    case 'i':
                    case 'I':
                        type = 2; break;
                    case 'u':
                    case 'U':
                        type = 3; break;
                    case 'e':
                    case 'E':
                        type = 4; break;
                    case 'o':
                    case 'O':
                        type = 5; break;
                }

                AddBlendShapeClip(type, clip.start + clipDuration * index, clipDuration * size, 
                    aiueoShapeTrack, AnimationCurveExtensions.UCurve(0.5f, 0.0f, clipAsset.Min, clipAsset.Max));
                AddBlendShapeClip(type, clip.start + clipDuration * index, clipDuration * size,
                    upperTeethShapeTracK, AnimationCurveExtensions.UCurve(0.5f, 0.0f, clipAsset.Min, clipAsset.Max));
                AddBlendShapeClip(type, clip.start + clipDuration * index, clipDuration * size,
                    buttomTeethShapeTracK, AnimationCurveExtensions.UCurve(0.5f, 0.0f, clipAsset.Min, clipAsset.Max));

                size = 1;
                index = i + 1;
            }
        }
    }

    clips = blinkTrack.GetClips();
    foreach (var clip in clips)
    {
        LabelClip clipAsset = clip.asset as LabelClip;
        AddBlendShapeClip(0, clip.start, clip.duration, blinkShapeTrack, 
            AnimationCurveExtensions.UCurve(0.5f, 0.0f, clipAsset.Min, clipAsset.Max));
    }
}


private void ProcessBlendShape(LabelTrack track)
{
    var clips = track.GetClips();
    foreach (var clip in clips)
    {
        LabelClip clipAsset = clip.asset as LabelClip;
        char[] labels = clipAsset.Label.ToCharArray();
        float duration = 1.0f / labels.Length;

        AnimationCurve curveA = AnimationCurveExtensions.ZeroCurve();
        AnimationCurve curveI = AnimationCurveExtensions.ZeroCurve();
        AnimationCurve curveU = AnimationCurveExtensions.ZeroCurve();
        AnimationCurve curveE = AnimationCurveExtensions.ZeroCurve();
        AnimationCurve curveO = AnimationCurveExtensions.ZeroCurve();

        int size = 1;
        int index = 0;
        for (int i = 0; i < labels.Length; i++)
        {
            if (i < labels.Length - 1 && labels[i] == labels[i + 1])
                size++;
            else
            {
                int type = -1;
                switch (labels[i])
                {
                    case 'a':
                    case 'A':
                        curveA.AddKey(duration * index + duration * 0.5f, clipAsset.Max);
                        if (size > 1)
                            curveA.AddKey(duration * (index + size) - duration * 0.5f, clipAsset.Max);
                        if (i < labels.Length - 1)
                            curveA.AddKey(duration * (index + size), clipAsset.Min);
                        break;
                    case 'i':
                    case 'I':
                        curveI.AddKey(duration * index + duration * 0.5f, clipAsset.Max);
                        if (size > 1)
                            curveI.AddKey(duration * (index + size) - duration * 0.5f, clipAsset.Max);
                        if (i < labels.Length - 1)
                            curveI.AddKey(duration * (index + size), clipAsset.Min);
                        break;
                    case 'u':
                    case 'U':
                        curveU.AddKey(duration * index + duration * 0.5f, clipAsset.Max);
                        if (size > 1)
                            curveU.AddKey(duration * (index + size) - duration * 0.5f, clipAsset.Max);
                        if (i < labels.Length - 1)
                            curveU.AddKey(duration * (index + size), clipAsset.Min);
                        break;
                    case 'e':
                    case 'E':
                        curveE.AddKey(duration * index + duration * 0.5f, clipAsset.Max);
                        if (size > 1)
                            curveE.AddKey(duration * (index + size) - duration * 0.5f, clipAsset.Max);
                        if (i < labels.Length - 1)
                            curveE.AddKey(duration * (index + size), clipAsset.Min);
                        break;
                    case 'o':
                    case 'O':
                        curveO.AddKey(duration * index + duration * 0.5f, clipAsset.Max);
                        if (size > 1)
                            curveO.AddKey(duration * (index + size) - duration * 0.5f, clipAsset.Max);
                        if (i < labels.Length - 1)
                            curveO.AddKey(duration * (index + size), clipAsset.Min);
                        break;
                }
            }
            size = 1;
            index = i + 1;
        }

        AnimationCurve[] curves = new AnimationCurve[5] { curveA, curveI, curveU, curveE, curveO };
        AddBlendShapeClip(clip.start, clip.duration, track, curves);
    }

    clips = track.GetClips();
    foreach (var clip in clips)
    {
        LabelClip clipAsset = clip.asset as LabelClip;
        AnimationCurve[] curves = new AnimationCurve[1] { AnimationCurveExtensions.UCurve(0.5f, 0.0f, clipAsset.Min, clipAsset.Max) };
        AddBlendShapeClip(clip.start, clip.duration, track, curves);
    }

    private void AddBlendShapeClip(int type, double start, double duration, BlendShapeTrack track, AnimationCurve curve)
    {
        TimelineClip timelineClip = track.CreateDefaultClip();
        timelineClip.start = start;
        timelineClip.duration = duration;

        BlendShapeClip clipAsset = timelineClip.asset as BlendShapeClip;

        PlayableGraph graph = Director.playableGraph;

        if (track.name == "AIUEO")
            graph.GetResolver().SetReferenceValue(clipAsset.Mesh.exposedName, GameObject.Find("TKVR01_001_CH_Mesh_Face"));
        if (track.name == "UpperTeeth")
            graph.GetResolver().SetReferenceValue(clipAsset.Mesh.exposedName, GameObject.Find("TKVR01_001_CH_Mesh_Teeth_upper"));
        if (track.name == "ButtomTeeth")
            graph.GetResolver().SetReferenceValue(clipAsset.Mesh.exposedName, GameObject.Find("TKVR01_001_CH_Mesh_Teeth_buttom"));

        if (type == 0)
            clipAsset.AddShape(new BlendShapeClip.Shape(8, "BlinkBoth", timelineClip, curve));
        else if (track.name == "AIUEO")
        {
            clipAsset.AddShape(new BlendShapeClip.Shape(11, "A", timelineClip, AnimationCurveExtensions.ZeroCurve()));
            clipAsset.AddShape(new BlendShapeClip.Shape(12, "I", timelineClip, AnimationCurveExtensions.ZeroCurve()));
            clipAsset.AddShape(new BlendShapeClip.Shape(13, "U", timelineClip, AnimationCurveExtensions.ZeroCurve()));
            clipAsset.AddShape(new BlendShapeClip.Shape(14, "E", timelineClip, AnimationCurveExtensions.ZeroCurve()));
            clipAsset.AddShape(new BlendShapeClip.Shape(15, "O", timelineClip, AnimationCurveExtensions.ZeroCurve()));
        }
        else
        {
            clipAsset.AddShape(new BlendShapeClip.Shape(8, "A", timelineClip, AnimationCurveExtensions.ZeroCurve()));
            clipAsset.AddShape(new BlendShapeClip.Shape(9, "I", timelineClip, AnimationCurveExtensions.ZeroCurve()));
            clipAsset.AddShape(new BlendShapeClip.Shape(10, "U", timelineClip, AnimationCurveExtensions.ZeroCurve()));
            clipAsset.AddShape(new BlendShapeClip.Shape(11, "E", timelineClip, AnimationCurveExtensions.ZeroCurve()));
            clipAsset.AddShape(new BlendShapeClip.Shape(12, "O", timelineClip, AnimationCurveExtensions.ZeroCurve()));
        }

        switch (type)
        {
            case 0:
                break;
            case 1:
                clipAsset.Shapes[0].SetAnimationCurve(curve);
                break;
            case 2:
                clipAsset.Shapes[1].SetAnimationCurve(curve);
                break;
            case 3:
                clipAsset.Shapes[2].SetAnimationCurve(curve);
                break;
            case 4:
                clipAsset.Shapes[3].SetAnimationCurve(curve);
                break;
            case 5:
                clipAsset.Shapes[4].SetAnimationCurve(curve);
                break;
        }
    }
}
*
*/