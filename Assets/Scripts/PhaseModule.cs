﻿using UnityEngine;
using UnityEditor;
using System;

public class PhaseModule : Module
{
    public string[] PartName = {"Head", "LeftHand", "RightHand"};

    public float VelocityThreshold = 0.1f;
    public float PositionThreshold = 0.1f;
    public bool[] Bones = null;
    public float Window = 2.0f;

    public bool ShowVelocities = true;
    public bool ShowPositions = true;
    public bool ShowKeys = true;
    public bool ShowCycle = true;

    public LocalPhaseFunction[] Phases = new LocalPhaseFunction[3];

    public override ID GetID()
    {
        return Module.ID.Phase;
    }

    public override Module Init(MotionData data)
    {
        Data = data;
        Bones = new bool[Data.Root.Bones.Length];

        Phases[0] = new LocalPhaseFunction("Head", new int[2] { 4, 5 }, this);
        Phases[1] = new LocalPhaseFunction("Left Hand", new int[5] { 7, 8, 9, 10, 11 }, this);
        Phases[2] = new LocalPhaseFunction("Right Hand", new int[5] { 12, 13, 14, 15, 16 }, this);

        return this;
    }

    public void ToggleBone(int[] index, PhaseFunction phase)
    {
        for (int i = 0; i < index.Length; i++)
        {
            Bones[index[i]] = !Bones[index[i]];
        }
    }

    public override void DerivedInspector(MotionEditor editor)
    {
        ShowVelocities = EditorGUILayout.Toggle("Show Velocities", ShowVelocities);
        ShowPositions = EditorGUILayout.Toggle("Show Positions", ShowPositions);
        ShowKeys = EditorGUILayout.Toggle("Show Keys", ShowKeys);
        //ShowCycle = EditorGUILayout.Toggle("Show Cycle", ShowCycle);
        Window = EditorGUILayout.FloatField("Window Time", Window);
        VelocityThreshold = EditorGUILayout.FloatField("Velocity Threshold", VelocityThreshold);
        PositionThreshold = EditorGUILayout.FloatField("Position Threshold", PositionThreshold);

        if (GUILayout.Button("Compute"))
        {
            for (int i = 0; i < Phases.Length; i++)
            {
                ToggleBone(Phases[i].Index, Phases[i].LocalPhase);
                Phases[i].LocalPhase.ComputeVelocity();
                Phases[i].LocalPhase.ComputePosition();
                Phases[i].LocalPhase.ComputeKey();
                ToggleBone(Phases[i].Index, Phases[i].LocalPhase);
            }
        }

        for (int i = 0; i < Phases.Length; i++)
        {
            EditorGUILayout.LabelField(Phases[i].Name);
            Phases[i].LocalPhase.Inspector(editor);
        }
    }

    public class LocalPhaseFunction
    {
        public PhaseModule Module;
        public string Name;
        public int[] Index;
        public PhaseFunction LocalPhase;

        public LocalPhaseFunction(string name, int[] index, PhaseModule module)
        {
            Module = module;
            Name = name;
            Index = index;
            LocalPhase = new PhaseFunction(module);
        }
    }

    public class PhaseFunction
    {
        public PhaseModule Module;
        public float[] Phase;
        public bool[] Keys;
        public float[] Velocities;
        public float[] NVelocities;
        public float[] Positions;
        public float[] NPositions;

        public PhaseFunction(PhaseModule module)
        {
            Module = module;
            Phase = new float[module.Data.GetTotalFrames()];
            Keys = new bool[module.Data.GetTotalFrames()];
            ComputeVelocity();
            ComputePosition();
        }

        public void ComputeVelocity()
        {
            float min, max;
            Velocities = new float[Module.Data.GetTotalFrames()];
            NVelocities = new float[Module.Data.GetTotalFrames()];
            min = float.MaxValue;
            max = float.MinValue;
            for (int i = 0; i < Velocities.Length; i++)
            {
                for (int j = 0; j < Module.Bones.Length; j++)
                {
                    if (Module.Bones[j])
                        Velocities[i] = Mathf.Min(Module.Data.Frames[i].GetBoneVelocity(j, true, 1f / Module.Data.Framerate).magnitude, 1.0f);
                }
                if (Velocities[i] < Module.VelocityThreshold)
                {
                    Velocities[i] = 0.0f;
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

        public void ComputePosition()
        {
            float min, max;
            Positions = new float[Module.Data.GetTotalFrames()];
            NPositions = new float[Module.Data.GetTotalFrames()];
            Matrix4x4[] Posture = new Matrix4x4[Module.Bones.Length];
            min = float.MaxValue;
            max = float.MinValue;
            Frame toFrame = Module.Data.GetFrame(5);
            Matrix4x4[] toPosture = toFrame.GetBoneTransformations(true);

            for (int i = 0; i < Positions.Length; i++)
            {
                Posture = Module.Data.Frames[i].GetBoneTransformations(true);
                for (int j = 0; j < Module.Bones.Length; j++)
                {
                    if (Module.Bones[j])
                    {
                        float bonePosition = Mathf.Min(Posture[j].GetPosition().GetRelativePositionTo(toPosture[j]).magnitude, 1.0f);
                        Positions[i] += bonePosition;
                    }
                }
                if (Positions[i] < Module.PositionThreshold)
                {
                    Positions[i] = 0.0f;
                }
                if (Positions[i] < min)
                {
                    min = Positions[i];
                }
                if (Positions[i] > max)
                {
                    max = Positions[i];
                }

                float velocities = 0.0f;

                for(int k = i; k < Mathf.Min(i + Module.Data.Framerate * Module.Window, Module.Data.GetTotalFrames()); k++)
                {
                    velocities += Velocities[k];
                }

                if(velocities < Module.VelocityThreshold && Positions[i] != 0.0f)
                {
                    toFrame = Module.Data.GetFrame(i);
                    toPosture = toFrame.GetBoneTransformations(true);
                }
            }
            for (int i = 0; i < Positions.Length; i++)
            {
                NPositions[i] = Utility.Normalise(Positions[i], min, max, 0f, 1f);
            }
        }

        public void ComputeKey()
        {
            Keys = new bool[Module.Data.GetTotalFrames()];
            for (int i = 1; i < Keys.Length; i++)
            {
                if((Positions[i-1] == 0.0f && Positions[i] != 0.0f) || (Positions[i-1] != 0.0f && Positions[i] == 0.0f))
                {
                    SetKey(Module.Data.GetFrame(i), true);
                }
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

        public void Inspector(MotionEditor editor)
        {
            UltiDraw.Begin();
            using (new EditorGUILayout.VerticalScope("Box"))
            { 
                Frame frame = editor.GetCurrentFrame();

                EditorGUILayout.BeginHorizontal();

                if (IsKey(frame))
                {
                    GUI.color = Color.red;
                    if (GUILayout.Button("Key"))
                        SetKey(frame, false);
                }
                else
                {
                    GUI.color = Color.white;
                    if (GUILayout.Button("Key"))
                        SetKey(frame, true);
                }
                GUI.color = Color.white;

                if (IsKey(frame))
                {
                    SetPhase(frame, EditorGUILayout.Slider("Phase", GetPhase(frame), 0f, 1f));
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    SetPhase(frame, EditorGUILayout.Slider("Phase", GetPhase(frame), 0f, 1f));
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("<", GUILayout.Width(25.0f), GUILayout.Height(50.0f)))
                {
                    editor.LoadFrame((GetPreviousKey(frame)));
                }

                EditorGUILayout.BeginVertical(GUILayout.Height(50f));
                Rect ctrl = EditorGUILayout.GetControlRect();
                Rect rect = new Rect(ctrl.x, ctrl.y, ctrl.width, 50f);
                EditorGUI.DrawRect(rect, UltiDraw.Black);

                float startTime = frame.Timestamp - editor.GetWindow() / 2f;
                float endTime = frame.Timestamp + editor.GetWindow() / 2f;
                if (startTime < 0f)
                {
                    endTime -= startTime;
                    startTime = 0f;
                }
                if (endTime > Module.Data.GetTotalTime())
                {
                    startTime -= endTime - Module.Data.GetTotalTime();
                    endTime = Module.Data.GetTotalTime();
                }
                startTime = Mathf.Max(0f, startTime);
                endTime = Mathf.Min(Module.Data.GetTotalTime(), endTime);
                int start = Module.Data.GetFrame(startTime).Index;
                int end = Module.Data.GetFrame(endTime).Index;
                int elements = end - start;

                Vector3 prevPos = Vector3.zero;
                Vector3 newPos = Vector3.zero;
                Vector3 bottom = new Vector3(0f, rect.yMax, 0f);
                Vector3 top = new Vector3(0f, rect.yMax - rect.height, 0f);

                if (Module.ShowVelocities)
                {
                    for (int i = 1; i < elements; i++)
                    {
                        prevPos.x = rect.xMin + (float)(i - 1) / (elements - 1) * rect.width;
                        prevPos.y = rect.yMax - NVelocities[i + start - 1] * rect.height;
                        newPos.x = rect.xMin + (float)(i) / (elements - 1) * rect.width;
                        newPos.y = rect.yMax - NVelocities[i + start] * rect.height;
                        UltiDraw.DrawLine(prevPos, newPos, Color.red);
                    }
                }

                if (Module.ShowPositions)
                {
                    for (int i = 1; i < elements; i++)
                    {
                        prevPos.x = rect.xMin + (float)(i - 1) / (elements - 1) * rect.width;
                        prevPos.y = rect.yMax - NPositions[i + start - 1] * rect.height;
                        newPos.x = rect.xMin + (float)(i) / (elements - 1) * rect.width;
                        newPos.y = rect.yMax - NPositions[i + start] * rect.height;
                        UltiDraw.DrawLine(prevPos, newPos, Color.green);
                    }
                }

                //if (Module.ShowCycle)
                //{
                //    //Cycle
                //    for (int i = 1; i < elements; i++)
                //    {
                //        prevPos.x = rect.xMin + (float)(i - 1) / (elements - 1) * rect.width;
                //        prevPos.y = rect.yMax - NCycle[i + start - 1] * rect.height;
                //        newPos.x = rect.xMin + (float)(i) / (elements - 1) * rect.width;
                //        newPos.y = rect.yMax - NCycle[i + start] * rect.height;
                //        UltiDraw.DrawLine(prevPos, newPos, UltiDraw.Yellow);
                //    }
                //}

                //Phase
                //for(int i=1; i<Module.Data.Frames.Length; i++) {
                //	Frame A = Module.Data.Frames[i-1];
                //	Frame B = Module.Data.Frames[i];
                //	prevPos.x = rect.xMin + (float)(A.Index-start)/elements * rect.width;
                //	prevPos.y = rect.yMax - Mathf.Repeat(Phase[A.Index-1], 1f) * rect.height;
                //	newPos.x = rect.xMin + (float)(B.Index-start)/elements * rect.width;
                //	newPos.y = rect.yMax - Phase[B.Index-1] * rect.height;
                //	UltiDraw.DrawLine(prevPos, newPos, UltiDraw.White);
                //	bottom.x = rect.xMin + (float)(B.Index-start)/elements * rect.width;
                //	top.x = rect.xMin + (float)(B.Index-start)/elements * rect.width;
                //}

                if (Module.ShowKeys)
                {
                    Frame A = Module.Data.GetFrame(start);
                    if (A.Index == 1)
                    {
                        bottom.x = rect.xMin;
                        top.x = rect.xMin;
                        UltiDraw.DrawLine(bottom, top, UltiDraw.Magenta.Transparent(0.5f));
                    }
                    Frame B = GetNextKey(A);
                    while (A != B)
                    {
                        prevPos.x = rect.xMin + (float)(A.Index - start) / elements * rect.width;
                        prevPos.y = rect.yMax - Mathf.Repeat(Phase[A.Index - 1], 1f) * rect.height;
                        newPos.x = rect.xMin + (float)(B.Index - start) / elements * rect.width;
                        newPos.y = rect.yMax - Phase[B.Index - 1] * rect.height;
                        UltiDraw.DrawLine(prevPos, newPos, UltiDraw.White);
                        bottom.x = rect.xMin + (float)(B.Index - start) / elements * rect.width;
                        top.x = rect.xMin + (float)(B.Index - start) / elements * rect.width;
                        UltiDraw.DrawLine(bottom, top, UltiDraw.Magenta.Transparent(0.5f));
                        A = B;
                        B = GetNextKey(A);
                        if (B.Index > end)
                        {
                            break;
                        }
                    }
                }

                //Seconds
                float timestamp = startTime;
                while (timestamp <= endTime)
                {
                    float floor = Mathf.FloorToInt(timestamp);
                    if (floor >= startTime && floor <= endTime)
                    {
                        top.x = rect.xMin + (float)(Module.Data.GetFrame(floor).Index - start) / elements * rect.width;
                        UltiDraw.DrawWireCircle(top, 5f, UltiDraw.White);
                    }
                    timestamp += 1f;
                }
                //

                //Current Pivot
                float pStart = (float)(Module.Data.GetFrame(Mathf.Clamp(frame.Timestamp - 1f, 0f, Module.Data.GetTotalTime())).Index - start) / (float)elements;
                float pEnd = (float)(Module.Data.GetFrame(Mathf.Clamp(frame.Timestamp + 1f, 0f, Module.Data.GetTotalTime())).Index - start) / (float)elements;
                float pLeft = rect.x + pStart * rect.width;
                float pRight = rect.x + pEnd * rect.width;
                Vector3 pA = new Vector3(pLeft, rect.y, 0f);
                Vector3 pB = new Vector3(pRight, rect.y, 0f);
                Vector3 pC = new Vector3(pLeft, rect.y + rect.height, 0f);
                Vector3 pD = new Vector3(pRight, rect.y + rect.height, 0f);
                UltiDraw.DrawTriangle(pA, pC, pB, UltiDraw.White.Transparent(0.1f));
                UltiDraw.DrawTriangle(pB, pC, pD, UltiDraw.White.Transparent(0.1f));
                top.x = rect.xMin + (float)(frame.Index - start) / elements * rect.width;
                bottom.x = rect.xMin + (float)(frame.Index - start) / elements * rect.width;
                UltiDraw.DrawLine(top, bottom, UltiDraw.Yellow);

                Handles.DrawLine(Vector3.zero, Vector3.zero); //Somehow needed to get it working...
                EditorGUILayout.EndVertical();

                if (GUILayout.Button(">", GUILayout.Width(25.0f), GUILayout.Height(50.0f)))
                {
                    editor.LoadFrame(GetNextKey(frame));
                }
                EditorGUILayout.EndHorizontal();

                //EditorGUILayout.BeginHorizontal();
                //if (Utility.GUIButton("Fill Next", UltiDraw.DarkGrey, UltiDraw.White))
                //{
                //    editor.LoadFrame(Mathf.Clamp(editor.GetCurrentFrame().Timestamp + Module.Delta, 0f, Module.Data.GetTotalTime()));
                //    SetKey(editor.GetCurrentFrame(), true);
                //}
                //if (Utility.GUIButton("Clear Next", UltiDraw.DarkGrey, UltiDraw.White))
                //{
                //    editor.LoadFrame(GetNextKey(editor.GetCurrentFrame()));
                //    SetKey(editor.GetCurrentFrame(), false);
                //}
                //if (Utility.GUIButton("Separate", UltiDraw.DarkGrey, UltiDraw.White))
                //{
                //    Frame current = editor.GetCurrentFrame();
                //    Frame next = GetNextKey(current);
                //    Frame pivot = this == Module.RegularPhaseFunction ? Module.InversePhaseFunction.GetNextKey(current) : Module.RegularPhaseFunction.GetNextKey(current);
                //    Frame previous = Module.Data.GetFrame(pivot.Timestamp - (next.Timestamp - pivot.Timestamp));
                //    editor.LoadFrame(previous);
                //}
                //EditorGUILayout.EndHorizontal();
            }
            UltiDraw.End();
        }
    }
}
