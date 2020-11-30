using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public abstract class Module : ScriptableObject
{
    public enum ID { Phase, Emotion, Action, Velocity, Root, Head, Length };

    public MotionData Data;
    private bool Visiable = false;
    private static string[] Names = null;

	public static string[] GetNames()
	{
		if (Names == null)
		{
			Names = new string[(int)Module.ID.Length + 1];
			for (int i = 0; i < Names.Length - 1; i++)
			{
				Names[i] = ((Module.ID)i).ToString();
			}
		}
		return Names;
	}

	public void Inspector(MotionEditor editor)
	{
		using (new EditorGUILayout.VerticalScope("Box"))
		{
			EditorGUILayout.BeginHorizontal();
			Visiable = EditorGUILayout.Toggle(Visiable, GUILayout.Width(20.0f));
			EditorGUILayout.LabelField(GetID().ToString() + " Module");
			if (GUILayout.Button("Remove"))
				Data.RemoveModule(GetID());
			EditorGUILayout.EndHorizontal();

			if (Visiable)
				DerivedInspector(editor);
		}
	}

	public abstract ID GetID();
	public abstract Module Init(MotionData Data);
	public abstract void DerivedInspector(MotionEditor editor);
}
