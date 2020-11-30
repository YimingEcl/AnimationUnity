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
    private bool showAcceleration;

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
        showAcceleration = false;
        return this;
    }

    public void GetTransformations(Frame frame)
    {
        Transformations = frame.GetBoneTransformations(true);
    }

    public void GetVelocities(Frame frame)
    {
        Velocities = frame.GetBoneVelocities(true, 1.0f);
    }

    public void GetAccelerations(Frame frame)
    {
        Accelerations = frame.GetBoneAccelerations(true, 1.0f);
    }

    public void GetAngularVelocities(Frame frame)
    {
        AngularVelocities = frame.GetAngularBoneVelocities(true, 1.0f);
    }

    public override void DerivedInspector(MotionEditor editor)
    {
        Frame frame = editor.GetCurrentFrame();
        GetVelocities(frame);
        GetAccelerations(frame);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable All"))
        {
            for (int i = 0; i < Selected.Length; i++)
                Selected[i] = true;
        }
        if(GUILayout.Button("Disable All"))
        {
            for (int i = 0; i < Selected.Length; i++)
                Selected[i] = false;
        }
        if(GUILayout.Button("Show Velocity"))
        {
            showAcceleration = false;
        }
        if (GUILayout.Button("Show Acceleration"))
        {
            showAcceleration = true;
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < Selected.Length; i++)
        {
            Vector3 vector;
            if (!showAcceleration)
                vector = Velocities[i];
            else
                vector = Accelerations[i];

            EditorGUILayout.BeginHorizontal();
            Selected[i] = EditorGUILayout.Toggle(Selected[i], GUILayout.Width(20.0f));
            EditorGUILayout.LabelField(Data.Root.Bones[i].Name, GUILayout.Width(100.0f));
            EditorGUILayout.LabelField("X: " + vector.x, GUILayout.Width(100.0f));
            EditorGUILayout.LabelField("Y: " + vector.y, GUILayout.Width(100.0f));
            EditorGUILayout.LabelField("Z: " + vector.z, GUILayout.Width(100.0f));
            EditorGUILayout.EndHorizontal();
        }
    }
}