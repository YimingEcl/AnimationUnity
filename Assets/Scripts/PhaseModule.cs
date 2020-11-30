using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhaseModule : Module
{
	public PhaseFunction RegularPhaseFunction = null;
	public bool[] Variables = new bool[0];

    public override ID GetID()
    {
        return ID.Phase;
    }

    public override Module Init(MotionData data)
    {
        Data = data;
		RegularPhaseFunction = new PhaseFunction(this);
		Variables = new bool[Data.Root.Bones.Length];
        return this;
    }

    public override void DerivedInspector(MotionEditor editor)
    {

    }

    [System.Serializable]

    public class PhaseFunction
    {
        public PhaseModule Module;
        public float[] Phase;
        public bool[] Keys;

		[System.NonSerialized] public float[] Cycle;
		[System.NonSerialized] public float[] NCycle;
		[System.NonSerialized] public float[] Velocities;
		[System.NonSerialized] public float[] NVelocities;

		public PhaseFunction(PhaseModule module)
		{
			Module = module;
			Phase = new float[module.Data.GetTotalFrames()];
			Keys = new bool[module.Data.GetTotalFrames()];
			Compute();
		}

		//public IEnumerator Optimise()
		//{
		//	PhaseEvolution optimiser = new PhaseEvolution(this);
		//	while (true)
		//	{
		//		optimiser.Optimise();
		//		yield return new WaitForSeconds(0f);
		//	}
		//}

		public void Clear()
		{
			Phase = new float[Module.Data.GetTotalFrames()];
			Keys = new bool[Module.Data.GetTotalFrames()];
			Cycle = new float[Module.Data.GetTotalFrames()];
			NCycle = new float[Module.Data.GetTotalFrames()];
		}

		public void Setup()
		{
			if (Cycle == null || Cycle.Length != Module.Data.GetTotalFrames() || NCycle == null || NCycle.Length != Module.Data.GetTotalFrames() || Velocities == null || Velocities.Length != Module.Data.GetTotalFrames() || NVelocities == null || NVelocities.Length != Module.Data.GetTotalFrames())
			{
				Compute();
			}
		}

		public void Compute()
		{
			float min, max;
			Cycle = new float[Module.Data.GetTotalFrames()];
			NCycle = new float[Module.Data.GetTotalFrames()];
			Velocities = new float[Module.Data.GetTotalFrames()];
			NVelocities = new float[Module.Data.GetTotalFrames()];
			min = float.MaxValue;
			max = float.MinValue;
			for (int i = 0; i < Velocities.Length; i++)
			{
				for (int j = 0; j < Module.Variables.Length; j++)
				{
					if (Module.Variables[j])
					{
						float boneVelocity = Mathf.Min(Module.Data.Frames[i].GetBoneVelocity(j, (this == Module.RegularPhaseFunction ? false : true), 1f / Module.Data.Framerate).magnitude, 10.0f);
						Velocities[i] += boneVelocity;
					}
				}
				if (Velocities[i] < 0.1f)
				{
					Velocities[i] = 0f;
				}
				if (Velocities[i] < min)
				{
					min = Velocities[i];
				}
				if (Velocities[i] > max)
				{
					max = Velocities[i];
				}
			}
			for (int i = 0; i < Velocities.Length; i++)
			{
				NVelocities[i] = Utility.Normalise(Velocities[i], min, max, 0f, 1f);
			}
		}

		public void SetKey(Frame frame, bool value)
		{
			if (value)
			{
				if (IsKey(frame))
				{
					return;
				}
				Keys[frame.Index - 1] = true;
				Phase[frame.Index - 1] = 1f;
				Interpolate(frame);
			}
			else
			{
				if (!IsKey(frame))
				{
					return;
				}
				Keys[frame.Index - 1] = false;
				Phase[frame.Index - 1] = 0f;
				Interpolate(frame);
			}
		}

		public bool IsKey(Frame frame)
		{
			return Keys[frame.Index - 1];
		}

		public void SetPhase(Frame frame, float value)
		{
			if (Phase[frame.Index - 1] != value)
			{
				Phase[frame.Index - 1] = value;
				Interpolate(frame);
			}
		}

		public float GetPhase(Frame frame)
		{
			return Phase[frame.Index - 1];
		}

		public Frame GetPreviousKey(Frame frame)
		{
			if (frame != null)
			{
				for (int i = frame.Index - 1; i >= 1; i--)
				{
					if (Keys[i - 1])
					{
						return Module.Data.Frames[i - 1];
					}
				}
			}
			return Module.Data.Frames.First();
		}

		public Frame GetNextKey(Frame frame)
		{
			if (frame != null)
			{
				for (int i = frame.Index + 1; i <= Module.Data.GetTotalFrames(); i++)
				{
					if (Keys[i - 1])
					{
						return Module.Data.Frames[i - 1];
					}
				}
			}
			return Module.Data.Frames.Last();
		}

		public void Recompute()
		{
			for (int i = 0; i < Module.Data.Frames.Length; i++)
			{
				if (IsKey(Module.Data.Frames[i]))
				{
					Phase[i] = 1f;
				}
			}
			Frame A = Module.Data.Frames[0];
			Frame B = GetNextKey(A);
			while (A != B)
			{
				Interpolate(A, B);
				A = B;
				B = GetNextKey(A);
			}
		}

		private void Interpolate(Frame frame)
		{
			if (IsKey(frame))
			{
				Interpolate(GetPreviousKey(frame), frame);
				Interpolate(frame, GetNextKey(frame));
			}
			else
			{
				Interpolate(GetPreviousKey(frame), GetNextKey(frame));
			}
		}

		private void Interpolate(Frame a, Frame b)
		{
			if (a == null || b == null)
			{
				Debug.Log("A given frame was null.");
				return;
			}
			if (a == b)
			{
				return;
			}
			if (a == Module.Data.Frames[0] && b == Module.Data.Frames[Module.Data.Frames.Length - 1])
			{
				return;
			}
			int dist = b.Index - a.Index;
			if (dist >= 2)
			{
				for (int i = a.Index + 1; i < b.Index; i++)
				{
					float rateA = (float)((float)i - (float)a.Index) / (float)dist;
					float rateB = (float)((float)b.Index - (float)i) / (float)dist;
					Phase[i - 1] = rateB * Mathf.Repeat(Phase[a.Index - 1], 1f) + rateA * Phase[b.Index - 1];
				}
			}

			if (a.Index == 1)
			{
				Frame first = Module.Data.Frames[0];
				Frame next1 = GetNextKey(first);
				Frame next2 = GetNextKey(next1);
				if (next2 == Module.Data.Frames[Module.Data.Frames.Length - 1])
				{
					float ratio = 1f - next1.Timestamp / Module.Data.GetTotalTime();
					SetPhase(first, ratio);
					SetPhase(next2, ratio);
				}
				else
				{
					float xFirst = next1.Timestamp - first.Timestamp;
					float mFirst = next2.Timestamp - next1.Timestamp;
					SetPhase(first, Mathf.Clamp(1f - xFirst / mFirst, 0f, 1f));
				}
			}
			if (b.Index == Module.Data.GetTotalFrames())
			{
				Frame last = Module.Data.Frames[Module.Data.GetTotalFrames() - 1];
				Frame previous1 = GetPreviousKey(last);
				Frame previous2 = GetPreviousKey(previous1);
				if (previous2 == Module.Data.Frames[0])
				{
					float ratio = 1f - previous1.Timestamp / Module.Data.GetTotalTime();
					SetPhase(last, ratio);
					SetPhase(previous2, ratio);
				}
				else
				{
					float xLast = last.Timestamp - previous1.Timestamp;
					float mLast = previous1.Timestamp - previous2.Timestamp;
					SetPhase(last, Mathf.Clamp(xLast / mLast, 0f, 1f));
				}
			}
		}
	}
}
