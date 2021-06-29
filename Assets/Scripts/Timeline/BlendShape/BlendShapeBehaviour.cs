using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class BlendShapeBehaviour : PlayableBehaviour
{
    public SkinnedMeshRenderer FaceMesh;
    public BlendShapeClip.Shape[] Shapes;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        FaceMesh = playerData as SkinnedMeshRenderer;

        if (Shapes != null && Shapes.Length != 0)
        {
            for (int i = 0; i < Shapes.Length; i++)
            {
                Shapes[i].CalculateWeight(playable.GetTime());
                FaceMesh.SetBlendShapeWeight(Shapes[i].Index, Shapes[i].Weight);
            }
        }
    }
    public string[] GetShapeNames()
    {
        int count = FaceMesh.sharedMesh.blendShapeCount;
        string[] names = new string[count];
        for (int i = 0; i < count; ++i)
            names[i] = FaceMesh.sharedMesh.GetBlendShapeName(i);

        return names;
    }

    public string GetShapeName(int index)
    {
        return FaceMesh.sharedMesh.GetBlendShapeName(index);
    }
}
