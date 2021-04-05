using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public abstract class NeuralAnimation : MonoBehaviour
{
	public enum FPS { Thirty, Sixty }

	public string FilePath;
	public NeuralNetwork NeuralNetwork = null;
	public Actor Actor;

	public float AnimationTime { get; private set; }
	public float PostprocessingTime { get; private set; }
	public FPS Framerate = FPS.Sixty;

	private int Index;
	protected string[] Lines = null;

	public abstract void Setup();
	public abstract void Feed(int index);
	public abstract void Read();

	void Start()
	{
		Lines = File.ReadAllLines(FilePath);
		Index = 0;
		Setup();
	}

	void LateUpdate()
	{
		Utility.SetFPS(Mathf.RoundToInt(GetFramerate()));
		if (NeuralNetwork != null && NeuralNetwork.Setup)
		{
			System.DateTime t1 = Utility.GetTimestamp();
			NeuralNetwork.ResetPivot(); 
			Feed(Index);
			NeuralNetwork.Predict();
			NeuralNetwork.ResetPivot(); 
			Read();
			AnimationTime = (float)Utility.GetElapsedTime(t1);

			Index++;
			if (Index == Lines.Length)
				Index = 0;

			System.DateTime t2 = Utility.GetTimestamp();
		}
	}
	public float GetFramerate()
	{
		switch (Framerate)
		{
			case FPS.Thirty:
				return 30f;
			case FPS.Sixty:
				return 60f;
		}
		return 1f;
	}


	[CustomEditor(typeof(NeuralAnimation), true)]
	public class NeuralAnimation_Editor : Editor
	{

		public NeuralAnimation Target;

		void Awake()
		{
			Target = (NeuralAnimation)target;
		}

		public override void OnInspectorGUI()
		{
			Undo.RecordObject(Target, Target.name);

			DrawDefaultInspector();

			EditorGUILayout.HelpBox("Animation: " + 1000f * Target.AnimationTime + "ms", MessageType.None);

			if (GUI.changed)
			{
				EditorUtility.SetDirty(Target);
			}
		}
	}
}
