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

    private int[] BoneMapping = new int[0];
    private MotionData Data = null;
    
    public void LoadBoneMapping()
    {
        BoneMapping = new int[GetActor().Bones.Length];
        for(int i = 0; i < GetActor().Bones.Length; i++)
        {
            MotionData.Hierarchy.Bone bone = Data.Root.FindBone(GetActor().Bones[i].GetName());
            BoneMapping[i] = bone == null ? -1 : bone.Index;
        }
    }

    public void UpdateBoneMapping()
    {
        if (Data == null)
            BoneMapping = new int[0];
        else
        {
            if (BoneMapping == null || BoneMapping.Length != GetActor().Bones.Length)
            {
                LoadBoneMapping();
            }
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
            LoadBoneMapping();
            LoadFrame(0.0f);
        }
    }

    public void LoadData(string name)
    {
        if (Data != null && Data.GetName() == name)
            return;
        MotionData data = System.Array.Find(Files, x => x.GetName() == name);
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
        LoadData(Files[Mathf.Max(System.Array.FindIndex(Files, x => x == Data) - 1, 0)]);
    }

    public void LoadNextData()
    {
        if (Data == null)
            return;
        LoadData(Files[Mathf.Min(System.Array.FindIndex(Files, x => x == Data) + 1, Files.Length - 1)]);
    }

    public void LoadFrame(float timestamp)
    {
        Timestamp = timestamp;
        Actor avatar = GetActor();
        Frame frame = GetCurrentFrame();
        Matrix4x4 root = frame.GetBoneTransformation(0, true);
        avatar.transform.position = root.GetPosition();
        avatar.transform.rotation = root.GetRotation();
        UpdateBoneMapping();

        for (int i = 0; i < avatar.Bones.Length; i++)
        {
            //if (BoneMapping[i] == -1)
            //    Debug.Log("Bone " + avatar.Bones[i].GetName() + " could not be mapped.");
            //else
            //{
            Matrix4x4 transformation = frame.GetBoneTransformation(BoneMapping[i], true);
            avatar.Bones[i].Transform.position = transformation.GetPosition();
            avatar.Bones[i].Transform.rotation = transformation.GetRotation();
            //}
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
        {
            return;
        }
        Playing = true;
        EditorCoroutines.StartCoroutine(Play(), this);
    }

    public void StopAnimation()
    {
        if (!Playing)
        {
            return;
        }
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
        public bool IsOK = false;

        private void Awake()
        {
            Target = (MotionEditor)target;
            InitNames();
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

        public override void OnInspectorGUI()
        {
            Target.Actor = (Actor)EditorGUILayout.ObjectField("Actor", Target.Actor, typeof(Actor), true);

            EditorGUILayout.BeginHorizontal();
            Target.Folder = EditorGUILayout.TextField("Folder", "Assets/" + Target.Folder.Substring(Mathf.Min(7, Target.Folder.Length)));
            if(GUILayout.Button("Load"))
            {
                Target.Load();
                if(Target.Data == null)
                    Target.LoadData(Target.Files[0]);
            }
            EditorGUILayout.EndHorizontal();

            if (Target.Data != null)
            {
                {
                    EditorGUILayout.BeginHorizontal();
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
                    EditorGUILayout.EndHorizontal();
                }

                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Data", GUILayout.Width(50.0f));
                    EditorGUILayout.ObjectField(Target.Data, typeof(MotionData), true);
                    EditorGUILayout.EndHorizontal();
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
            }
        }
    }
}