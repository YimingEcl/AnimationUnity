using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class MotionExporter : MonoBehaviour
{
    public MotionData[] Files = null;
    public string[] FileNames = null;
    public string Folder = string.Empty;
    public string OutFolder = string.Empty;
    public string[] Lines = null;

    public MotionEditor Editor = null;

    public void Load()
    {
        string[] guids = AssetDatabase.FindAssets("t:MotionData", new string[1] { Folder });
        if (guids.Length == 0)
            return;
        Files = new MotionData[guids.Length];
        FileNames = new string[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            Files[i] = (MotionData)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[i]), typeof(MotionData));
            FileNames[i] = Files[i].GetName();
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
                        case Module.ID.Emotion:
                        {
                            Lines[i] += "0 0 0 1 0 ";
                            Lines[i] += "0 0 0 1 0 ";
                            break;
                        }
                        case Module.ID.Velocity:
                        {
                            VelocityModule module = (VelocityModule)data.GetModule(Module.ID.Velocity);
                            module.GetVelocities(data.Frames[i]);
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
        string InputFile = OutFolder + "/" + data.GetName() + "Input.txt";

        using (var outputFile = new StreamWriter(InputFile))
        {
            foreach (string line in Lines)
                outputFile.WriteLine(line);
        }

        Debug.Log("Test");
    }

    [CustomEditor(typeof(MotionExporter))]
    public class MotionExporterEditor : Editor
    {
        MotionExporter Target;
        int Selected = 0;
        MotionData data;

        private void Awake()
        {
            Target = (MotionExporter)target;
        }

        public override void OnInspectorGUI()
        {
            Target.Editor = GameObject.FindObjectOfType<MotionEditor>();

            EditorGUILayout.BeginHorizontal();
            Target.Folder = EditorGUILayout.TextField("Data Folder", "Assets/" + Target.Folder.Substring(Mathf.Min(7, Target.Folder.Length)));
            if (GUILayout.Button("Load"))
                Target.Load();
            EditorGUILayout.EndHorizontal();

            if(Target.Files != null)
            {
                Selected = EditorGUILayout.Popup("Select Data", Selected, Target.FileNames);
                data = Target.Files[Selected];

                EditorGUILayout.BeginHorizontal();
                Target.OutFolder = EditorGUILayout.TextField("Export Folder", "Assets/" + Target.OutFolder.Substring(Mathf.Min(7, Target.OutFolder.Length)));
                if (GUILayout.Button("Export"))
                    Target.Export(data);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
