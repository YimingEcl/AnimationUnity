using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TrajectoryModule : Module
{
    public int SampleCount;
    public int SampleSize;
    public int[] SampleFrame;
    public PivotBone[] Pivots;
    public string[] PartName;
    public bool[] Selected;

    public override ID GetID()
    {
        return Module.ID.Trajectory;
    }

    public override Module Init(MotionData data)
    {
        Data = data;
        SampleCount = 6;
        SampleSize = 10;
        SampleFrame = new int[SampleCount + 1];
        Pivots = new PivotBone[3];
        PartName = new string[3] { "Head", "Left Hand", "Right Hand" };
        Selected = new bool[Pivots.Length];

        InitPivot();

        return this;
    }

    public void InitPivot()
    {
        int index = 0;
        string name = string.Empty;
        PivotBone bone = null;
        MotionData.Hierarchy.Bone head = Data.Root.FindBoneContains("Head");
        index = head == null ? 0 : head.Index;
        name = head == null ? string.Empty : head.Name;
        bone = new PivotBone(this, index, name);
        Pivots[0] = bone;
        MotionData.Hierarchy.Bone lh = Data.Root.FindBoneContains("LeftHand");
        index = head == null ? 0 : lh.Index;
        name = head == null ? string.Empty : lh.Name;
        bone = new PivotBone(this, index, name);
        Pivots[1] = bone;
        MotionData.Hierarchy.Bone rh = Data.Root.FindBoneContains("RightHand");
        index = head == null ? 0 : rh.Index;
        name = head == null ? string.Empty : rh.Name;
        bone = new PivotBone(this, index, name);
        Pivots[2] = bone;
    }

    public void InitFrame(int index)
    {
        index = index - SampleCount / 2 * SampleSize;
        for(int i = 0; i < SampleFrame.Length; i++)
        {
            SampleFrame[i] = index;
            index += 10;
        }
    }

    public void ComputePivot()
    {
        for(int i = 0; i < Pivots.Length; i++)
        {
            Pivots[i].GetTransformations();
            Pivots[i].GetVelocities();
        }
    }

    public override void DerivedInspector(MotionEditor editor)
    {
        Frame frame = editor.GetCurrentFrame();
        InitFrame(frame.Index);
        ComputePivot();

        SampleCount = EditorGUILayout.IntField("Sample Count", SampleCount);
        SampleSize = EditorGUILayout.IntField("Sample Size", SampleSize);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable All"))
        {
            for (int i = 0; i < Selected.Length; i++)
                Selected[i] = true;
        }
        if (GUILayout.Button("Disable All"))
        {
            for (int i = 0; i < Selected.Length; i++)
                Selected[i] = false;
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < Pivots.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            Selected[i] = EditorGUILayout.Toggle(Selected[i], GUILayout.Width(20.0f));
            EditorGUILayout.LabelField(PartName[i] + " Part");
            EditorGUILayout.EndHorizontal();

            if (Selected[i])
            {
                Pivots[i].Index = EditorGUILayout.Popup("Pivot Bone", Pivots[i].Index, Data.Root.GetBoneNames());

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Index", GUILayout.Width(40.0f));
                EditorGUILayout.LabelField("PositionX", GUILayout.Width(100.0f));
                EditorGUILayout.LabelField("PositionY", GUILayout.Width(100.0f));
                EditorGUILayout.LabelField("PositionZ", GUILayout.Width(100.0f));
                EditorGUILayout.LabelField("VelocityX", GUILayout.Width(100.0f));
                EditorGUILayout.LabelField("VelocityY", GUILayout.Width(100.0f));
                EditorGUILayout.LabelField("VelocityZ", GUILayout.Width(100.0f));
                EditorGUILayout.EndHorizontal();

                for (int j = 0; j < SampleCount; j++)
                {
                    Matrix4x4 tf = Pivots[i].Transformations[j];
                    Vector3 v = Pivots[i].Velocities[j];

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(j.ToString(), GUILayout.Width(40.0f));
                    EditorGUILayout.LabelField(tf.GetPosition().x.ToString(), GUILayout.Width(100.0f));
                    EditorGUILayout.LabelField(tf.GetPosition().y.ToString(), GUILayout.Width(100.0f));
                    EditorGUILayout.LabelField(tf.GetPosition().z.ToString(), GUILayout.Width(100.0f));
                    EditorGUILayout.LabelField(v.x.ToString(), GUILayout.Width(100.0f));
                    EditorGUILayout.LabelField(v.y.ToString(), GUILayout.Width(100.0f));
                    EditorGUILayout.LabelField(v.z.ToString(), GUILayout.Width(100.0f));
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }

    [Serializable]
    public class PivotBone
    {
        public TrajectoryModule Module;
        public int Index = -1;
        public string Name = string.Empty;
        public Matrix4x4[] Transformations;
        public Vector3[] Velocities;

        public PivotBone(TrajectoryModule module, int index, string name)
        {
            Module = module;
            Index = index;
            Name = name;
            Transformations = new Matrix4x4[Module.SampleFrame.Length];
            Velocities = new Vector3[Module.SampleFrame.Length];
        }

        public void GetTransformations()
        {
            for(int i = 0; i < Transformations.Length; i++)
            {
                Transformations[i] = Module.Data.GetFrame(Module.SampleFrame[i]).GetBoneTransformation(Index, true);
            }
        }

        public void GetVelocities()
        {
            for (int i = 0; i < Velocities.Length; i++)
            {
                Velocities[i] = Module.Data.GetFrame(Module.SampleFrame[i]).GetBoneVelocity(Index, true, 1.0f);
            }
        }
    }
}
