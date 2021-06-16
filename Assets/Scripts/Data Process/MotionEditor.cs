using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MotionEditor : MonoBehaviour
{
    public Actor Actor = null;
    public MotionData[] Files = new MotionData[0];
    public string Folder = string.Empty;
    public int Index = 0;
    public bool Playing = false;

    private float Timestamp = 0.0f;
    private BoneMap[] Map = null;
    private MotionData Data = null;

    [Serializable]
    private struct BoneMap
    {
        public string BvhBoneName;
        public Transform ActorBoneTransform;
    }

    public void AutoMap()
    {
        Map = new BoneMap[Data.Root.Bones.Length];
        for (int i = 0; i < Data.Root.Bones.Length; i++)
        {
            Map[i].BvhBoneName = Data.Root.Bones[i].Name;
            string name = Data.Root.Bones[i].Name;

            if (!Data.Mirrored)
            {
                if (name.Contains("Left"))
                    name = name.Replace("Left", "Right");
                else if (name.Contains("Right"))
                    name = name.Replace("Right", "Left");
            }
            
            Actor.Bone bone = GetActor().FindBoneContains(name);
            if (bone == null)
                Map[i].ActorBoneTransform = null;
            else
                Map[i].ActorBoneTransform = bone.Transform;
        }
    }

    public void UpdateBoneMapping()
    {
        if (Data == null)
            Map = new BoneMap[0];
        else if (Map == null)
            AutoMap();
        else if (Data.MirrorChanged)
        {
            AutoMap();
            Data.MirrorChanged = false;
        }
    }

    public MotionData GetData()
    {
        return Data;
    }

    public Actor GetActor()
    {
        if (Actor != null)
            return Actor;
        if (Actor == null)
        {
            Actor = Data.CreateActor();
        }
        return Actor;
    }

    public Frame GetCurrentFrame()
    {
        return Data == null ? null : Data.GetFrame(Timestamp);
    }

    public void Load()
    {
        string[] guids = AssetDatabase.FindAssets("t:MotionData", new string[1] { Folder });
        Files = new MotionData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            Files[i] = (MotionData)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(MotionData));
    }

    public void LoadData(MotionData data)
    {
        Data = data;

        if(Data != null)
        {
            AutoMap();
            LoadFrame(0.0f);
        }
    }

    public void LoadData(string name)
    {
        if (Data != null && Data.GetName() == name)
            return;
        MotionData data = Array.Find(Files, x => x.GetName() == name);
        if (data == null)
        {
            Debug.Log("Data " + name + " could not be found.");
            return;
        }
        LoadData(data);
    }

    public void LoadPreviousData()
    {
        if (Data == null)
            return;
        LoadData(Files[Mathf.Max(Array.FindIndex(Files, x => x == Data) - 1, 0)]);
    }

    public void LoadNextData()
    {
        if (Data == null)
            return;
        LoadData(Files[Mathf.Min(Array.FindIndex(Files, x => x == Data) + 1, Files.Length - 1)]);
    }

    public void LoadFrame(float timestamp)
    {
        Timestamp = timestamp;
        Actor actor = GetActor();
        Frame frame = GetCurrentFrame();
        Matrix4x4 root = frame.GetBoneTransformation(0, Data.Mirrored);
        actor.transform.position = root.GetPosition(); 
        actor.transform.rotation = root.GetRotation();
        UpdateBoneMapping();

        for (int i = 0; i < actor.Bones.Length; i++)
        {
            BoneMap map = Array.Find(Map, x => x.ActorBoneTransform == actor.Bones[i].Transform);
            if (map.BvhBoneName != null)
            {
                Matrix4x4 transformation = frame.GetBoneTransformation(map.BvhBoneName, Data.Mirrored);
                actor.Bones[i].Transform.position = transformation.GetPosition();
                actor.Bones[i].Transform.rotation = transformation.GetRotation();
                actor.Bones[i].Velocity = frame.GetBoneVelocity(map.BvhBoneName, Data.Mirrored, 1.0f / 30);
            }
        }
    }

    public void LoadFrame(int index)
    {
        LoadFrame(Data.GetFrame(index).Timestamp);
    }

    public void LoadFrame(Frame frame)
    {
        LoadFrame(frame.Index);
    }

    public void PlayAnimation()
    {
        if (Playing)
            return;
        Playing = true;
        EditorCoroutines.StartCoroutine(Play(), this);
    }

    public void StopAnimation()
    {
        if (!Playing)
            return;
        Playing = false;
        EditorCoroutines.StopCoroutine(Play(), this);
    }

    private IEnumerator Play()
    {
        System.DateTime previous = Utility.GetTimestamp();
        while (Data != null)
        {
            float delta = 1.0f * (float)Utility.GetElapsedTime(previous);
            if (delta > 1f / 60.0f)
            {
                previous = Utility.GetTimestamp();
                LoadFrame(Mathf.Repeat(Timestamp + delta, Data.GetTotalTime()));
            }
            yield return new WaitForSeconds(0f);
        }
    }

    public float GetWindow()
    {
        return Data == null ? 0f : Data.GetTotalTime();
    }

    [CustomEditor(typeof(MotionEditor))]
    public class MotionEditor_Editor : Editor
    {
        public MotionEditor Target;
        public string[] Names = new string[0];
        public string[] EnumNames = new string[0];

        private bool Visiable = false;

        private void Awake()
        {
            Target = (MotionEditor)target;
            InitNames();
            EditorApplication.update += EditorUpdate;
        }

        private void OnDestroy()
        {
            EditorApplication.update -= EditorUpdate;
            if (Target.Data != null)
            {
                Target.Data.Save();
            }
        }

        public void InitNames()
        {
            List<string> names = new List<string>();
            List<string> enumNames = new List<string>();

            for(int i = 0; i < Target.Files.Length; i++)
            {
                names.Add(Target.Files[i].GetName());
                enumNames.Add("[" + (i + 1) + "]" + " " + Target.Files[i].GetName());
            }

            Names = names.ToArray();
            EnumNames = enumNames.ToArray();
        }

        public void EditorUpdate()
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            Target.Actor = (Actor)EditorGUILayout.ObjectField("Actor", Target.Actor, typeof(Actor), true);

            EditorGUILayout.BeginHorizontal();
            Target.Folder = EditorGUILayout.TextField("Folder", "Assets/" + Target.Folder.Substring(Mathf.Min(7, Target.Folder.Length)));
            if(GUILayout.Button("Load"))
            {
                Target.Load();
                if(Target.Files.Length == 0)
                {
                    Debug.Log("Motion data could not be found!");
                    return;
                }
                if(Target.Data == null)
                    Target.LoadData(Target.Files[0]);
                InitNames();
            }
            EditorGUILayout.EndHorizontal();

            if (Target.Data != null)
            {
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Data", GUILayout.Width(50.0f));
                    int selectIndex = EditorGUILayout.Popup(System.Array.FindIndex(Names, x => x == Target.Data.GetName()), EnumNames);
                    if (selectIndex != -1)
                    {
                        Target.LoadData(Names[selectIndex]);
                    }
                    if (GUILayout.Button("<"))
                        Target.LoadPreviousData();
                    if (GUILayout.Button(">"))
                        Target.LoadNextData();
                    int sliderIndex = EditorGUILayout.IntSlider(System.Array.FindIndex(Target.Files, x => x == Target.Data) + 1, 1, Target.Files.Length);
                    if (Event.current.type == EventType.Used)
                        Target.LoadData(Target.Files[sliderIndex - 1]);
                    EditorGUILayout.LabelField("/ " + Target.Files.Length, GUILayout.Width(60.0f));
                    GUI.color = Target.Data.Mirrored ? Color.red : Color.white;
                    if (GUILayout.Button("Mirror"))
                    {
                        Target.Data.Mirrored = !Target.Data.Mirrored;
                        Target.Data.MirrorChanged = true;
                    }
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }

                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Data", GUILayout.Width(50.0f));
                    EditorGUILayout.ObjectField(Target.Data, typeof(MotionData), true);
                    EditorGUILayout.EndHorizontal();
                }

                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    {
                        EditorGUILayout.BeginHorizontal();
                        Visiable = EditorGUILayout.Toggle(Visiable, GUILayout.Width(20.0f));
                        EditorGUILayout.LabelField("Show Bone Map", GUILayout.Width(300.0f));
                        if (GUILayout.Button("Auto"))
                            Target.AutoMap();
                        EditorGUILayout.EndHorizontal();
                    }

                    if (Visiable)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("BVH Bone Name", GUILayout.Width(200.0f));
                        EditorGUILayout.LabelField("Actor Bone Name");
                        EditorGUILayout.EndHorizontal();

                        for (int i = 0; i < Target.Data.Root.Bones.Length; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(Target.Map[i].BvhBoneName, GUILayout.Width(200.0f));
                            EditorGUILayout.ObjectField(Target.Map[i].ActorBoneTransform, typeof(Transform), true);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                {
                    Frame frame = Target.GetCurrentFrame();
                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button("<<"))
                        Target.LoadFrame(Mathf.Max(frame.Timestamp - 1.0f, 0.0f));
                    if(GUILayout.Button("<"))
                        Target.LoadFrame(Mathf.Max(frame.Timestamp - 1.0f / Target.Data.Framerate, 0.0f));
                    if (GUILayout.Button(">"))
                        Target.LoadFrame(Mathf.Min(frame.Timestamp + 1.0f / Target.Data.Framerate, Target.Data.GetTotalTime()));
                    if (GUILayout.Button(">>"))
                        Target.LoadFrame(Mathf.Min(frame.Timestamp + 1.0f, Target.Data.GetTotalTime()));
                    int index = EditorGUILayout.IntSlider(frame.Index, 1, Target.Data.GetTotalFrames());
                    if (index != frame.Index)
                        Target.LoadFrame(index);
                    EditorGUILayout.LabelField(frame.Timestamp.ToString("F3") + "s", GUILayout.Width(50f));
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Create Actor"))
                    Target.Data.CreateActor();

                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Play"))
                        Target.PlayAnimation();
                    if (GUILayout.Button("Stop"))
                        Target.StopAnimation();
                    EditorGUILayout.EndHorizontal();
                }

                int module = EditorGUILayout.Popup(0, ArrayExtensions.Concat(new string[1] { "Add Module" }, Module.GetNames()));
                if (module > 0)
                    Target.Data.AddModule(((Module.ID)(module - 1)));

                for (int i = 0; i < Target.Data.Modules.Length; i++)
                    Target.Data.Modules[i].Inspector(Target);

                EditorUtility.SetDirty(this);
            }
        }
    }
}