using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeSeries
{
	public Sample[] Samples = new Sample[0];
	public Series[] Data = new Series[0];
	public int Pivot = 0;
	public int Resolution = 0;
	public float PastWindow = 0f;
	public float FutureWindow = 0f;

	public int PastSampleCount
	{
		get { return Pivot; }
	}
	public int FutureSampleCount
	{
		get { return Samples.Length - Pivot - 1; }
	}
	public int KeyCount
	{
		get { return PastKeyCount + FutureKeyCount + 1; }
	}
	public int PivotKey
	{
		get { return Pivot / Resolution; }
	}
	public int PastKeyCount
	{
		get { return Pivot / Resolution; }
	}
	public int FutureKeyCount
	{
		get { return (Samples.Length - Pivot - 1) / Resolution; }
	}

	public TimeSeries(int pastKeys, int futureKeys, float pastWindow, float futureWindow, int resolution)
	{
		int samples = pastKeys + futureKeys + 1;
		if (samples == 1 && resolution != 1)
		{
			resolution = 1;
			Debug.Log("Resolution corrected to 1 because only one sample is available.");
		}

		Samples = new Sample[(samples - 1) * resolution + 1];
		Pivot = pastKeys * resolution;
		Resolution = resolution;

		for (int i = 0; i < Pivot; i++)
		{
			Samples[i] = new Sample(i, -pastWindow + i * pastWindow / (pastKeys * resolution));
		}
		Samples[Pivot] = new Sample(Pivot, 0f);
		for (int i = Pivot + 1; i < Samples.Length; i++)
		{
			Samples[i] = new Sample(i, (i - Pivot) * futureWindow / (futureKeys * resolution));
		}

		PastWindow = pastWindow;
		FutureWindow = futureWindow;
	}

	private void Add(Series series)
	{
		ArrayExtensions.Add(ref Data, series);
		if (series.TimeSeries != null)
		{
			Debug.Log("Data is already added to another time series.");
		}
		else
		{
			series.TimeSeries = this;
		}
	}

	public Series GetSeries(string type)
	{
		for (int i = 0; i < Data.Length; i++)
		{
			if (Data[i].GetID().ToString() == type)
			{
				return Data[i];
			}
		}
		Debug.Log("Series of type " + type + " could not be found.");
		return null;
	}

	public Sample GetPivot()
	{
		return Samples[Pivot];
	}

	public Sample GetKey(int index)
	{
		if (index < 0 || index >= KeyCount)
		{
			Debug.Log("Given key was " + index + " but must be within 0 and " + (KeyCount - 1) + ".");
			return null;
		}
		return Samples[index * Resolution];
	}

	public Sample GetPreviousKey(int sample)
	{
		if (sample < 0 || sample >= Samples.Length)
		{
			Debug.Log("Given index was " + sample + " but must be within 0 and " + (Samples.Length - 1) + ".");
			return null;
		}
		return GetKey(sample / Resolution);
	}

	public Sample GetNextKey(int sample)
	{
		if (sample < 0 || sample >= Samples.Length)
		{
			Debug.Log("Given index was " + sample + " but must be within 0 and " + (Samples.Length - 1) + ".");
			return null;
		}
		if (sample % Resolution == 0)
		{
			return GetKey(sample / Resolution);
		}
		else
		{
			return GetKey(sample / Resolution + 1);
		}
	}

	public float GetWeight01(int index, float power)
	{
		return Mathf.Pow((float)index / (float)(Samples.Length - 1), power);
	}

	public float GetWeight1byN1(int index, float power)
	{
		return Mathf.Pow((float)(index + 1) / (float)Samples.Length, power);
	}

	public class Sample
	{
		public int Index;
		public float Timestamp;

		public Sample(int index, float timestamp)
		{
			Index = index;
			Timestamp = timestamp;
		}
	}

	public abstract class Series
	{
		public enum ID { Velocity, Action, Phase, Trajectory, Length };
		public TimeSeries TimeSeries;
		public abstract ID GetID();
	}

	public class Velocity : Series
	{
		public Matrix4x4[] Transformations;
		public Vector3[] Velocities;

		public override ID GetID()
		{
			return Series.ID.Velocity;
		}

		public Velocity(TimeSeries timeSeries)
		{
			timeSeries.Add(this);
			Transformations = new Matrix4x4[TimeSeries.Samples.Length];
			Velocities = new Vector3[TimeSeries.Samples.Length];
			for (int i = 0; i < Transformations.Length; i++)
			{
				Transformations[i] = Matrix4x4.identity;
				Velocities[i] = Vector3.zero;
			}
		}

		public void SetTransformation(int index, Matrix4x4 transformation)
		{
			Transformations[index] = transformation;
		}

		public Matrix4x4 GetTransformation(int index)
		{
			return Transformations[index];
		}

		public void SetPosition(int index, Vector3 position)
		{
			Matrix4x4Extensions.SetPosition(ref Transformations[index], position);
		}

		public Vector3 GetPosition(int index)
		{
			return Transformations[index].GetPosition();
		}

		public void SetRotation(int index, Quaternion rotation)
		{
			Matrix4x4Extensions.SetRotation(ref Transformations[index], rotation);
		}

		public Quaternion GetRotation(int index)
		{
			return Transformations[index].GetRotation();
		}

		public void SetDirection(int index, Vector3 direction)
		{
			Matrix4x4Extensions.SetRotation(ref Transformations[index], Quaternion.LookRotation(direction == Vector3.zero ? Vector3.forward : direction, Vector3.up));
		}

		public Vector3 GetDirection(int index)
		{
			return Transformations[index].GetForward();
		}

		public void SetVelocity(int index, Vector3 velocity)
		{
			Velocities[index] = velocity;
		}

		public Vector3 GetVelocity(int index)
		{
			return Velocities[index];
		}

		public float ComputeSpeed()
		{
			float length = 0f;
			for (int i = TimeSeries.Pivot + TimeSeries.Resolution; i < TimeSeries.Samples.Length; i += TimeSeries.Resolution)
			{
				length += Vector3.Distance(GetPosition(i - TimeSeries.Resolution), GetPosition(i));
			}
			return length / TimeSeries.FutureWindow;
		}
	}


	public class Action : Series
	{
		public string[] Actions;
		public float[][] Values;

		public override ID GetID()
		{
			return Series.ID.Action;
		}

		public Action(TimeSeries timeSeries, params string[] actions)
		{
			timeSeries.Add(this);
			Actions = actions;
			Values = new float[TimeSeries.Samples.Length][];
			for (int i = 0; i < Values.Length; i++)
			{
				Values[i] = new float[Actions.Length];
			}
		}

		public void SetAction(int index, string name, float value)
		{
			int idx = ArrayExtensions.FindIndex(ref Actions, name);
			if (idx == -1)
			{
				// Debug.Log("Action " + name + " could not be found.");
				return;
			}
			Values[index][idx] = value;
		}

		public float GetAction(int index, string name)
		{
			int idx = ArrayExtensions.FindIndex(ref Actions, name);
			if (idx == -1)
			{
				Debug.Log("Action " + name + " could not be found.");
				return 0f;
			}
			return Values[index][idx];
		}
	}

	public class Phase : Series
	{
		public float[] Values;

		public override ID GetID()
		{
			return Series.ID.Phase;
		}

		public Phase(TimeSeries timeSeries)
		{
			timeSeries.Add(this);
			Values = new float[TimeSeries.Samples.Length];
		}

		public void Draw()
		{
			float[] values = new float[TimeSeries.KeyCount];
			for (int i = 0; i < TimeSeries.KeyCount; i++)
			{
				values[i] = Values[TimeSeries.GetKey(i).Index];
			}
			UltiDraw.Begin();
			UltiDraw.DrawGUIBars(new Vector2(0.875f, 0.510f), new Vector2(0.2f, 0.1f), values, 01f, 1f, 0.01f, UltiDraw.DarkGrey, UltiDraw.White);
			UltiDraw.End();
		}

		public void GUI()
		{
			UltiDraw.Begin();
			UltiDraw.DrawGUILabel(0.85f, 0.4f, 0.0175f, "Phase", UltiDraw.Black);
			UltiDraw.End();
		}
	}

	public class Trajectory : Series
	{
		public string[] Bones;
		public float[][] Magnitudes;
		public float[][] Phases;

		public float[][] _PhaseMagnitudes;
		public float[][] _PhaseUpdateValues;
		public Vector2[][] _PhaseUpdateVectors;
		public Vector2[][] _PhaseStates;

		public override ID GetID()
		{
			return Series.ID.Trajectory;
		}

		public Trajectory(TimeSeries timeSeries)
		{
			timeSeries.Add(this);
		}

	}
}
