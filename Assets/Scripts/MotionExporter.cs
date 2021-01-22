using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class MotionExporter : MonoBehaviour
{
    public MotionData[] Files = null;
    public string[] FileNames = null;
    public bool[] Exports = null;
    public string Folder = string.Empty;
    public string Destination = string.Empty;
    public string[] Lines = null;

    public MotionEditor Editor = null;

    public void Load()
    {
        string[] guids = AssetDatabase.FindAssets("t:MotionData", new string[1] { Folder });
        if (guids.Length == 0)
            return;
        Files = new MotionData[guids.Length];
        FileNames = new string[guids.Length];
        Exports = new bool[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            Files[i] = (MotionData)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(MotionData));
            FileNames[i] = Files[i].GetName();
            Exports[i] = false;
        }
    }

    public void Export(MotionData data)
    {
        Lines = new string[data.Frames.Length];
        for(int i = 0; i < data.Frames.Length; i++)
        {
            for(int j = 0; j < data.Root.Bones.Length; j++)
            {
                Lines[i] += data.Frames[i].GetBoneTransformation(j, true).GetPosition().x.ToString() + " ";
                Lines[i] += data.Frames[i].GetBoneTransformation(j, true).GetPosition().y.ToString() + " ";
                Lines[i] += data.Frames[i].GetBoneTransformation(j, true).GetPosition().z.ToString() + " ";
            }

            for(int j = 0; j < data.Root.Bones.Length; j++)
            {
                Vector3 euler = data.Frames[i].GetBoneTransformation(j, true).GetRotation().eulerAngles;
                Lines[i] += euler.x.ToString() + " ";
                Lines[i] += euler.y.ToString() + " ";
                Lines[i] += euler.z.ToString() + " ";
            }

            if(data.Modules != null)
            {
                for(int k = 0; k < data.Modules.Length; k++)
                {
                    switch(data.Modules[k].GetID())
                    {
                        case Module.ID.Action:
                        {
                            Lines[i] += "0 0 0 1 0 ";
                            Lines[i] += "0 0 0 1 0 ";
                            break;
                        }
                        case Module.ID.Velocity:
                        {
                            VelocityModule module = (VelocityModule)data.GetModule(Module.ID.Velocity);
                            module.GetVelocities(data.Frames[i], 1.0f);
                            for (int j = 0; j < data.Root.Bones.Length; j++)
                            {
                                Lines[i] += module.Velocities[j].x.ToString() + " ";
                                Lines[i] += module.Velocities[j].y.ToString() + " ";
                                Lines[i] += module.Velocities[j].z.ToString() + " ";
                            }

                            for(int j = 0; j < data.Root.Bones.Length; j++)
                            {
                                Lines[i] += module.AngularVelocities[j].ToString() + " ";
                            }
                            break;
                        }
                        case Module.ID.Root:
                        {
                            RootModule module = (RootModule)data.GetModule(Module.ID.Root);
                            Vector3 pos = module.GetRootPosition(data.Frames[i], true);
                            Lines[i] += pos.x.ToString() + " ";
                            Lines[i] += pos.y.ToString() + " ";
                            Lines[i] += pos.z.ToString() + " ";
                            break;
                        }
                    }
                }
            }
        }
        string InputFile = Destination + "/" + data.GetName() + "Input.txt";

        using (var outputFile = new StreamWriter(InputFile))
        {
            foreach (string line in Lines)
                outputFile.WriteLine(line);
        }

        Debug.Log("Export MotionData: " + data.GetName() + "Successfully!");
    }

    [CustomEditor(typeof(MotionExporter))]
    public class MotionExporterEditor : Editor
    {
        public MotionExporter Target;

        private bool IsExist = false;

        private void Awake()
        {
            Target = (MotionExporter)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();
            Target.Folder = EditorGUILayout.TextField("Data Folder", "Assets/" + Target.Folder.Substring(Mathf.Min(7, Target.Folder.Length)));
            if (GUILayout.Button("Load"))
            {
                Target.Load();
                if (Target.Files.Length == 0)
                {
                    Debug.Log("No Motion Data File!");
                    return;
                }
                else
                    IsExist = true;
            }
            EditorGUILayout.EndHorizontal();

            if (IsExist)
            {
                Target.Destination = EditorGUILayout.TextField("Export Folder", "Assets/" + Target.Destination.Substring(Mathf.Min(7, Target.Destination.Length)));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Enable All"))
                {
                    for (int i = 0; i < Target.Files.Length; i++)
                        Target.Exports[i] = true;
                }
                if (GUILayout.Button("Disable All"))
                {
                    for (int i = 0; i < Target.Files.Length; i++)
                        Target.Exports[i] = false;
                }
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < Target.Files.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    Target.Exports[i] = EditorGUILayout.Toggle(Target.Exports[i], GUILayout.Width(20.0f));
                    EditorGUILayout.LabelField(Target.Files[i].GetName());
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Export"))
                {
                    for(int i = 0; i < Target.Files.Length; i++)
                    {
                        if (Target.Exports[i])
                            Target.Export(Target.Files[i]);
                    }
                }
            }
        }
    }
}
