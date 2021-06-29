using UnityEngine;
using UnityEditor;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class LabelClip : PlayableAsset
{
    public LabelBehaviour Behaviour = new LabelBehaviour();
    public string Label = string.Empty;
    public float Min = 0.0f;
    public float Max = 0.5f;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<LabelBehaviour>.Create(graph, Behaviour);
    }
}
