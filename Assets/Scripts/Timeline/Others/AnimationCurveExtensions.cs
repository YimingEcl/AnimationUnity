using UnityEngine;

public class AnimationCurveExtensions : AnimationCurve
{
    public static AnimationCurve UCurve(float time, float duration)
    {
        AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.1f, 0), new Keyframe(0.9f, 0), new Keyframe(1, 0));
        curve.AddKey(new Keyframe(time, 1));
        curve.AddKey(new Keyframe(time + duration, 1));

        return curve;
    }

    public static AnimationCurve UCurve(float time, float duration, float min, float max)
    {
        AnimationCurve curve = new AnimationCurve(new Keyframe(0, min), new Keyframe(0.1f, min), new Keyframe(0.9f, min), new Keyframe(1, min));
        curve.AddKey(new Keyframe(time, max));
        curve.AddKey(new Keyframe(time + duration, max));

        return curve;
    }

    public static AnimationCurve ZeroCurve()
    {
        return new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
    }

    public static AnimationCurve ConstantCurve(float value)
    {
        return new AnimationCurve(new Keyframe(0, value), new Keyframe(1, value));
    }
}
