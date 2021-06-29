using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(210.0f / 255.0f, 25.0f / 255.0f, 221.0f / 255.0f)]
[TrackBindingType(typeof(SkinnedMeshRenderer))]
[TrackClipType(typeof(BlendShapeClip))]
public class BlendShapeTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        foreach (TimelineClip clip in GetClips())
        {
            var playableAsset = clip.asset as BlendShapeClip;
            playableAsset.Clip = clip;
        }

        return ScriptPlayable<BlendShapeMixerBehaviour>.Create(graph, inputCount);
    }
}
