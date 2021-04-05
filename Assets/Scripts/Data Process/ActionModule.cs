using System;
using UnityEngine;
using UnityEditor;

public class ActionModule : Module
{
    public bool[] Keys;
    public Action[] Actions;

    public override ID GetID()
    {
        return ID.Action;
    }

    public override Module Init(MotionData data)
    {
        Data = data;
        Actions = new Action[0];
        Keys = new bool[data.GetTotalFrames()];
        Keys[0] = true;
        Keys[Keys.Length - 1] = true;
        DefaultAction();
        return this;
    }

    public void DefaultAction()
    {
        ArrayExtensions.Add(ref Actions, new Action(this, "Neutral"));
        ArrayExtensions.Add(ref Actions, new Action(this, "Cross Hands"));
        ArrayExtensions.Add(ref Actions, new Action(this, "Cross Arms"));
        ArrayExtensions.Add(ref Actions, new Action(this, "LH on Hip"));
        ArrayExtensions.Add(ref Actions, new Action(this, "RH on Hip"));
        ArrayExtensions.Add(ref Actions, new Action(this, "Thank"));
    }

    public void AddAction(string name)
    {
        if (Array.Exists(Actions, x => x.Name == name))
        {
            Debug.Log("Action with name " + name + " already exists.");
            return;
        }
        ArrayExtensions.Add(ref Actions, new Action(this, name));
    }

    public void RemoveAction(string name)
    {
        int index = Array.FindIndex(Actions, x => x.Name == name);
        if (index >= 0)
            ArrayExtensions.RemoveAt(ref Actions, index);
        else
            Debug.Log("Action with name " + name + " does not exist.");
    }

    public void ToggleKey(Frame frame)
    {
        Keys[frame.Index - 1] = !Keys[frame.Index - 1];
        for (int i = 0; i < Actions.Length; i++)
            Actions[i].Compute(frame);
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

    public string GetHotVector(Frame frame)
    {
        return GetHotVector(frame.Index);
    }

    public float[] GetHotVectorArray(int index)
    {
        float[] result = new float[Actions.Length];

        for (int i = 0; i < Actions.Length; i++)
        {
            switch (Actions[i].Name)
            {
                case "Neutral":
                {
                    if (Actions[i].Values[index] > 0)
                        result[0] = 1.0f;
                    else
                        result[0] = 0.0f;
                    break;
                }

                case "LH on Hip":
                {
                    if (Actions[i].Values[index] > 0)
                        result[1] = 1.0f;
                    else
                        result[1] = 0.0f;
                    break;
                }

                case "RH on Hip":
                {
                    if (Actions[i].Values[index] > 0)
                        result[2] = 1.0f;
                    else
                        result[2] = 0.0f;
                    break;
                }
            }
        }

        return result;
    }

    public string GetHotVector(int index)
    {
        string result = string.Empty;
        float[] Labels = new float[3];

        for (int i = 0; i < Actions.Length; i++)
        {
            switch (Actions[i].Name)
            {
                case "Neutral":
                    {
                        if (Actions[i].Values[index] > 0)
                            Labels[0] = 1.0f;
                        else
                            Labels[0] = 0.0f;
                        break;
                    }

                case "LH on Hip":
                    {
                        if (Actions[i].Values[index] > 0)
                            Labels[1] = 1.0f;
                        else
                            Labels[1] = 0.0f;
                        break;
                    }

                case "RH on Hip":
                    {
                        if (Actions[i].Values[index] > 0)
                            Labels[2] = 1.0f;
                        else
                            Labels[2] = 0.0f;
                        break;
                    }
            }
        }

        for (int i = 0; i < Labels.Length - 1; i++)
        {
            result += Labels[i].ToString() + " ";
        }
        result += Labels[Labels.Length - 1].ToString();

        return result;
    }

    public override void DerivedInspector(MotionEditor editor)
    {
        Frame frame = editor.GetCurrentFrame();
        Color[] colors = UltiDraw.GetRainbowColors(Actions.Length);

        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = IsKey(frame) ? Color.red : Color.white;
            if (GUILayout.Button("Key"))
                ToggleKey(frame);
            GUI.color = Color.white;
            if (GUILayout.Button("Add Action"))
            {
                AddAction("New Action " + (Actions.Length + 1));
                EditorGUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUI.BeginDisabledGroup(!IsKey(frame));
        for (int i = 0; i < Actions.Length; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = colors[i];
            if (GUILayout.Button(Actions[i].Name, GUILayout.Width(150.0f)))
                Actions[i].Toggle(frame);
            GUI.color = Color.white;
            EditorGUILayout.FloatField(Actions[i].GetValue(frame), GUILayout.Width(50.0f));
            Actions[i].Name = EditorGUILayout.TextField(Actions[i].Name);
            if (GUILayout.Button("Remove"))
                RemoveAction(Actions[i].Name);
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
            EditorGUI.DrawRect(rect, Color.black);
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

                for (int i = 0; i < Actions.Length; i++)
                {
                    Frame current = Data.Frames.First();
                    while (current != Data.Frames.Last())
                    {
                        Frame next = GetNextKey(current);
                        float _start = (float)(Mathf.Clamp(current.Index, start, end) - start) / (float)elements;
                        float _end = (float)(Mathf.Clamp(next.Index, start, end) - start) / (float)elements;
                        float xStart = rect.x + _start * rect.width;
                        float xEnd = rect.x + _end * rect.width;
                        float yStart = rect.y + (1f - Actions[i].Values[Mathf.Clamp(current.Index, start, end) - 1]) * rect.height;
                        float yEnd = rect.y + (1f - Actions[i].Values[Mathf.Clamp(next.Index, start, end) - 1]) * rect.height;
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
    public class Action
    {
        public ActionModule Module;
        public string Name;
        public float[] Values;

        public Action(ActionModule module, string name)
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
