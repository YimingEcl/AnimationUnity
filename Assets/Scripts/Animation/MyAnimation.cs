using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MyAnimation : NeuralAnimation
{
	private char[] TextSpace = new char[] { ' ', '\t', '\n', '\r' };

	private TimeSeries TimeSeries;
	private TimeSeries.Velocity VelocitySeries;
	private TimeSeries.Action ActionSeries;
	private TimeSeries.Phase PhaseSeries;
	private TimeSeries.Trajectory TrajectorySeries;

	public TimeSeries GetTimeSeries()
	{
		return TimeSeries;
	}

	public override void Setup()
	{
		TimeSeries = new TimeSeries(6, 6, 1f, 1f, 5);
		VelocitySeries = new TimeSeries.Velocity(TimeSeries);
		ActionSeries = new TimeSeries.Action(TimeSeries, "Neutral", "LH on Hip", "RH on Hip");
		PhaseSeries = new TimeSeries.Phase(TimeSeries);
		TrajectorySeries = new TimeSeries.Trajectory(TimeSeries);
	}

    public override void Feed(int index)
    {
		string[] entries = Lines[index].Split(TextSpace, System.StringSplitOptions.RemoveEmptyEntries);
		foreach(string entry in entries)
        {
			NeuralNetwork.Feed(float.Parse(entry));
        }
    }

	public override void Read()
    {
        string[] name = new string[] {"Hips", "Spine", "Spine1", "Spine2", "Neck", "Head", "LeftShoulder", "LeftArm", "LeftForeArm", "LeftHand",
                                      "RightShoulder", "RightArm", "RightForeArm", "RightHand", "LeftUpLeg", "LeftLeg", "LeftFoot", "LeftToeBase",
                                      "RightUpLeg", "RightLeg", "RightFoot", "RightToeBase"};
        Vector3[] positions = new Vector3[name.Length];
        Vector3[] forwards = new Vector3[name.Length];
        Vector3[] upwards = new Vector3[name.Length];
        Vector3[] velocities = new Vector3[name.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = NeuralNetwork.ReadVector3();
            forwards[i] = NeuralNetwork.ReadVector3().normalized;
            upwards[i] = NeuralNetwork.ReadVector3().normalized;
            velocities[i] = NeuralNetwork.ReadVector3();
        }

        for (int i = 0; i < positions.Length; i++)
        {
            Actor.Bone bone = Actor.FindBoneContains(name[i]);
            bone.Transform.position = positions[i];
            bone.Transform.rotation = Quaternion.LookRotation(forwards[i], upwards[i]);
        }
    }
}
