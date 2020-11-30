using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EmotionModule : Module
{
    public bool[] Keys;
    public Emotion[] Emotions;

    public override ID GetID()
    {
        return ID.Emotion;
    }

    public override Module Init(MotionData data)
    {
        Data = data;
        Emotions = new Emotion[0];
        Keys = new bool[data.GetTotalFrames()];
        Keys[0] = true;
        Keys[Keys.Length - 1] = true;
        DefaultEmotion();
        return this;
    }

    public void DefaultEmotion()
    {
        ArrayExtensions.Add(ref Emotions, new Emotion(this, "Happy"));
        ArrayExtensions.Add(ref Emotions, new Emotion(this, "Sad"));
        ArrayExtensions.Add(ref Emotions, new Emotion(this, "Scared"));
        ArrayExtensions.Add(ref Emotions, new Emotion(this, "Shocked"));
        ArrayExtensions.Add(ref Emotions, new Emotion(this, "Angry"));
    }

    public void AddEmotion(string name)
    {
        if (Array.Exists(Emotions, x => x.Name == name))
        {
            Debug.Log("Style with name " + name + " already exists.");
            return;
        }
        ArrayExtensions.Add(ref Emotions, new Emotion(this, name));
    }

    public void RemoveEmotion(string name)
    {
        int index = Array.FindIndex(Emotions, x => x.Name == name);
        if (index >= 0)
            ArrayExtensions.RemoveAt(ref Emotions, index);
        else
            Debug.Log("Style with name " + name + " does not exist.");
    }

    public void ToggleKey(Frame frame)
    {
        Keys[frame.Index - 1] = !Keys[frame.Index - 1];
        for (int i = 0; i < Emotions.Length; i++)
            Emotions[i].Compute(frame);
    }

    public bool IsKey(Frame frame)
    {
        return Keys[frame.Index - 1];
    }

    public Frame GetPreviousKey(Frame frame)
    {
        while (frame.Index > 1)
        {
            frame = Data.GetFrame(frame.Index - 1);
            if (IsKey(frame))
                return frame;
        }
        return Data.Frames.First();
    }

    public Frame GetNextKey(Frame frame)
    {
        while (frame.Index < Data.GetTotalFrames())
        {
            frame = Data.GetFrame(frame.Index + 1);
            if (IsKey(frame))
                return frame;
        }
        return Data.Frames.Last();
    }

    public override void DerivedInspector(MotionEditor editor)
    {
        Frame frame = editor.GetCurrentFrame();
        Color[] colors = UltiDraw.GetRainbowColors(Emotions.Length);

        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = IsKey(frame) ? Color.red : Color.white;
            if (GUILayout.Button("Key"))
                ToggleKey(frame);
            GUI.color = Color.white;
            if (GUILayout.Button("Add Emotion"))
            {
                AddEmotion("New Emotion " + (Emotions.Length + 1));
                EditorGUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.BeginDisabledGroup(!IsKey(frame));
        for (int i = 0; i < Emotions.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = colors[i];
            if (GUILayout.Button(Emotions[i].Name, GUILayout.Width(150.0f)))
                Emotions[i].Toggle(frame);
            GUI.color = Color.white;
            EditorGUILayout.FloatField(Emotions[i].GetValue(frame), GUILayout.Width(50.0f));
            Emotions[i].Name = EditorGUILayout.TextField(Emotions[i].Name);
            if (GUILayout.Button("Remove"))
                RemoveEmotion(Emotions[i].Name);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.EndDisabledGroup();

        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(25.0f), GUILayout.Height(50.0f)))
                editor.LoadFrame(GetPreviousKey(frame));
            EditorGUILayout.BeginVertical(GUILayout.Height(50f));
            Rect ctrl = EditorGUILayout.GetControlRect();
            Rect rect = new Rect(ctrl.x, ctrl.y, ctrl.width, 50f);
            EditorGUI.DrawRect(rect, UltiDraw.Black);
            {
                UltiDraw.Begin();

                float startTime = frame.Timestamp - editor.GetWindow() / 2f;
                float endTime = frame.Timestamp + editor.GetWindow() / 2f;
                if (startTime < 0f)
                {
                    endTime -= startTime;
                    startTime = 0f;
                }
                if (endTime > Data.GetTotalTime())
                {
                    startTime -= endTime - Data.GetTotalTime();
                    endTime = Data.GetTotalTime();
                }
                startTime = Mathf.Max(0f, startTime);
                endTime = Mathf.Min(Data.GetTotalTime(), endTime);
                int start = Data.GetFrame(startTime).Index;
                int end = Data.GetFrame(endTime).Index;
                int elements = end - start;

                Vector3 prevPos = Vector3.zero;
                Vector3 newPos = Vector3.zero;
                Vector3 bottom = new Vector3(0f, rect.yMax, 0f);
                Vector3 top = new Vector3(0f, rect.yMax - rect.height, 0f);

                for (int i = 0; i < Emotions.Length; i++)
                {
                    Frame current = Data.Frames.First();
                    while (current != Data.Frames.Last())
                    {
                        Frame next = GetNextKey(current);
                        float _start = (float)(Mathf.Clamp(current.Index, start, end) - start) / (float)elements;
                        float _end = (float)(Mathf.Clamp(next.Index, start, end) - start) / (float)elements;
                        float xStart = rect.x + _start * rect.width;
                        float xEnd = rect.x + _end * rect.width;
                        float yStart = rect.y + (1f - Emotions[i].Values[Mathf.Clamp(current.Index, start, end) - 1]) * rect.height;
                        float yEnd = rect.y + (1f - Emotions[i].Values[Mathf.Clamp(next.Index, start, end) - 1]) * rect.height;
                        UltiDraw.DrawLine(new Vector3(xStart, yStart, 0f), new Vector3(xEnd, yEnd, 0f), colors[i]);
                        current = next;
                    }
                }

                for (int i = 0; i < Keys.Length; i++)
                {
                    if (Keys[i])
                    {
                        top.x = rect.xMin + (float)(i + 1 - start) / elements * rect.width;
                        bottom.x = rect.xMin + (float)(i + 1 - start) / elements * rect.width;
                        UltiDraw.DrawLine(top, bottom, UltiDraw.White);
                    }
                }

                float pStart = (float)(Data.GetFrame(Mathf.Clamp(frame.Timestamp - 1f, 0f, Data.GetTotalTime())).Index - start) / (float)elements;
                float pEnd = (float)(Data.GetFrame(Mathf.Clamp(frame.Timestamp + 1f, 0f, Data.GetTotalTime())).Index - start) / (float)elements;
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

                UltiDraw.End();
            }
            EditorGUILayout.EndVertical();
            if (GUILayout.Button(">", GUILayout.Width(25.0f), GUILayout.Height(50.0f)))
                editor.LoadFrame(GetNextKey(frame));
            EditorGUILayout.EndHorizontal();
        }
    }

    [Serializable]
    public class Emotion
    {
        public EmotionModule Module;
        public string Name;
        public float[] Values;

        public Emotion(EmotionModule module, string name)
        {
            Module = module;
            Name = name;
            Values = new float[module.Data.GetTotalFrames()];
        }

        public void SetValue(Frame frame, float value)
        {
            if (Values[frame.Index - 1] != value)
            {
                Values[frame.Index - 1] = value;
                Compute(frame);
            }
        }

        public float GetValue(Frame frame)
        {
            return Values[frame.Index - 1];
        }

        public void Toggle(Frame frame)
        {
            if(Module.IsKey(frame))
            {
                Values[frame.Index - 1] = GetValue(frame) == 1.0f ? 0.0f : 1.0f;
                Compute(frame);
            }
        }

        public void Compute(Frame frame)
        {
            Frame current = frame;
            Frame previous = Module.GetPreviousKey(current);
            Frame next = Module.GetNextKey(current);

            if (Module.IsKey(frame))
            {
                Values[current.Index - 1] = GetValue(current);

                if (previous != frame)
                {
                    float valA = GetValue(previous);
                    float valB = GetValue(current);
                    for (int i = previous.Index; i < current.Index; i++)
                    {
                        float weight = (float)(i - previous.Index) / (float)(frame.Index - previous.Index);
                        Values[i - 1] = (1f - weight) * valA + weight * valB;
                    }
                }

                if (next != frame)
                {
                    float valA = GetValue(current);
                    float valB = GetValue(next);
                    for (int i = current.Index + 1; i <= next.Index; i++)
                    {
                        float weight = (float)(i - current.Index) / (float)(next.Index - current.Index);
                        Values[i - 1] = (1f - weight) * valA + weight * valB;
                    }
                }
            }
            else
            {
                float valA = GetValue(previous);
                float valB = GetValue(next);
                for (int i = previous.Index; i <= next.Index; i++)
                {
                    float weight = (float)(i - previous.Index) / (float)(next.Index - previous.Index);
                    Values[i - 1] = (1f - weight) * valA + weight * valB;
                }
            }
        }
    }
}
