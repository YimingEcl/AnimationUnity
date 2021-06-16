using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class VelocityModule : Module
{
    public bool[] Selected;
    public Matrix4x4[] Transformations;
    public Vector3[] Velocities;
    public Vector3[] Accelerations;
    public float[] AngularVelocities;
    public bool[] PartSelected;
    public int[] JointCount;
    public string[] Local;
    public float Delta;
    private bool ShowAcceleration;  

    public override ID GetID()
    {
        return Module.ID.Velocity;
    }

    public override Module Init(MotionData data)
    {
        Data = data;
        Selected = new bool[data.Root.Bones.Length];
        Transformations = new Matrix4x4[data.Root.Bones.Length];
        Velocities = new Vector3[data.Root.Bones.Length];
        Accelerations = new Vector3[data.Root.Bones.Length];
        AngularVelocities = new float[data.Root.Bones.Length];
        ShowAcceleration = false;
        JointCount = new int[6] { 4, 7, 12, 17, 22, 27 };
        Local = new string[6] { "Body", "Head", "Left Hand", "Right Hand", "Left Leg", "Right Leg" };
        Delta = 1.0f;
        PartSelected = new bool[JointCount.Length];
        return this;
    }

    public void GetTransformations(Frame frame)
    {
        Transformations = frame.GetBoneTransformations(Data.Mirrored);
    }

    public void GetVelocities(Frame frame, float delta)
    {
        Velocities = frame.GetBoneVelocities(Data.Mirrored, delta);
    }

    public void GetAccelerations(Frame frame, float delta)
    {
        Accelerations = frame.GetBoneAccelerations(Data.Mirrored, delta);
    }

    public void GetAngularVelocities(Frame frame, float delta)
    {
        AngularVelocities = frame.GetAngularBoneVelocities(Data.Mirrored, delta);
    }

    public override void DerivedInspector(MotionEditor editor)
    {
        Frame frame = editor.GetCurrentFrame();
        GetVelocities(frame, Delta);
        GetAccelerations(frame, Delta);

        Delta = EditorGUILayout.FloatField("Delta Time", Delta);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable All"))
        {
            for (int i = 0; i < PartSelected.Length; i++)
                PartSelected[i] = true;
            for (int i = 0; i < Selected.Length; i++)
                Selected[i] = true;
        }
        if(GUILayout.Button("Disable All"))
        {
            for (int i = 0; i < PartSelected.Length; i++)
                PartSelected[i] = false;
            for (int i = 0; i < Selected.Length; i++)
                Selected[i] = false;
        }
        if(GUILayout.Button("Show Velocity"))
        {
            ShowAcceleration = false;
        }
        if (GUILayout.Button("Show Acceleration"))
        {
            ShowAcceleration = true;
        }
        EditorGUILayout.EndHorizontal();

        int index = 0;

        for (int i = 0; i < JointCount.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            bool partSelect = EditorGUILayout.Toggle(PartSelected[i], GUILayout.Width(20.0f));
            if(PartSelected[i] != partSelect)
            {
                PartSelected[i] = partSelect;
                if(i == 0)
                {
                    for (int j = 0; j < JointCount[i]; j++)
                        Selected[j] = partSelect;
                }
                else
                {
                    for(int j = JointCount[i-1]; j < JointCount[i]; j++)
                        Selected[j] = partSelect;
                }
            }
            EditorGUILayout.LabelField(Local[i] + " Part", GUILayout.Width(200.0f));
            EditorGUILayout.EndHorizontal();

            for (; index < JointCount[i]; index++)
            {
                Vector3 vector;
                if (!ShowAcceleration)
                    vector = Velocities[index];
                else
                    vector = Accelerations[index];

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("--", GUILayout.Width(15.0f));
                Selected[index] = EditorGUILayout.Toggle(Selected[index], GUILayout.Width(20.0f));
                EditorGUILayout.LabelField(Data.Root.Bones[index].Name, GUILayout.Width(100.0f));
                EditorGUILayout.LabelField("X: " + vector.x, GUILayout.Width(100.0f));
                EditorGUILayout.LabelField("Y: " + vector.y, GUILayout.Width(100.0f));
                EditorGUILayout.LabelField("Z: " + vector.z, GUILayout.Width(100.0f));
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}