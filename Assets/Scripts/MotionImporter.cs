using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class MotionImporter : MonoBehaviour
{
    public string Folder = string.Empty;
    public string Destination = string.Empty;
    public string[] Files = null;
    public bool[] Imports = null;

    private char[] TextSpace = new char[] { ' ', '\t', '\n', '\r' };
    private char[] NameSpace = new char[] { '_', ':', '.' };
    private MotionData Data;

    private void Awake()
    {
        Data = ScriptableObject.CreateInstance<MotionData>();
    }


    public void Import()
    {
        for(int i = 0; i < Files.Length; i++)
        {
            if(Imports[i])
                ImportFile(Files[i]);
        }
    }

    public void ImportFile(string FileName)
    {
        string[] lines = File.ReadAllLines(Folder + "\\" + FileName);
        int index = 0;
        string name = string.Empty;
        string parent = string.Empty;

        Data = ScriptableObject.CreateInstance<MotionData>();
        Data.name = FileName;
        if(!AssetDatabase.IsValidFolder(Destination) && Destination != "Assets/")
        {
            AssetDatabase.CreateFolder("Assets", Destination.Substring(Mathf.Min(7, Destination.Length)));
            Debug.Log("No Directory Found, New Created!");
        }
        AssetDatabase.CreateAsset(Data, Destination + "\\" + FixedFileName(Data.name) + ".asset");

        List<Vector3> offsets = new List<Vector3>();
        Vector3 offset = Vector3.zero;
        List<int[]> channels = new List<int[]>();
        int[] channel = null;
        List<float[]> motions = new List<float[]>();

        for (index = 0; index < lines.Length; index++)
        {
            if (lines[index] == "MOTION")
            {
                break;
            }

            string[] entries = lines[index].Split(TextSpace);

            for (int entry = 0; entry < entries.Length; entry++)
            {
                if (entries[entry].Contains("ROOT"))
                {
                    parent = "None";
                    name = FixedBoneName(entries[entry + 1]);
                    break;
                }
                else if (entries[entry].Contains("JOINT"))
                {
                    parent = name;
                    name = FixedBoneName(entries[entry + 1]);
                    break;
                }
                else if (entries[entry].Contains("End"))
                {
                    parent = name;
                    name = FixedBoneName(parent + entries[entry + 1]);

                    string[] offsetEntries = lines[index + 2].Split(TextSpace);
                    for (int offsetEntry = 0; offsetEntry < offsetEntries.Length; offsetEntry++)
                    {
                        if (offsetEntries[offsetEntry].Contains("OFFSET"))
                        {
                            offset.x = float.Parse(offsetEntries[offsetEntry + 1]);
                            offset.y = float.Parse(offsetEntries[offsetEntry + 2]);
                            offset.z = float.Parse(offsetEntries[offsetEntry + 3]);
                            break;
                        }
                    }

                    Data.Root.AddBone(name, parent);
                    offsets.Add(offset);
                    channels.Add(new int[0]);
                    index += 2;
                    break;
                }
                else if (entries[entry].Contains("OFFSET"))
                {
                    offset.x = float.Parse(entries[entry + 1]); 
                    offset.y = float.Parse(entries[entry + 2]);
                    offset.z = float.Parse(entries[entry + 3]);
                    break;
                }
                else if (entries[entry].Contains("CHANNEL"))
                {
                    channel = new int[int.Parse(entries[entry + 1])];

                    for (int i = 0; i < channel.Length; i++)
                    {
                        if (entries[entry + 2 + i] == "Xposition")
                        {
                            channel[i] = 1;
                        }
                        else if (entries[entry + 2 + i] == "Yposition")
                        {
                            channel[i] = 2;
                        }
                        else if (entries[entry + 2 + i] == "Zposition")
                        {
                            channel[i] = 3;
                        }
                        else if (entries[entry + 2 + i] == "Xrotation")
                        {
                            channel[i] = 4;
                        }
                        else if (entries[entry + 2 + i] == "Yrotation")
                        {
                            channel[i] = 5;
                        }
                        else if (entries[entry + 2 + i] == "Zrotation")
                        {
                            channel[i] = 6;
                        }
                    }

                    Data.Root.AddBone(name, parent);
                    offsets.Add(offset);
                    channels.Add(channel);
                    break;
                }
                else if (entries[entry].Contains("}"))
                {
                    name = parent;
                    parent = name == "None" ? "None" : Data.Root.FindBone(name).Parent;
                    break;
                }
            }
        }

        index += 1;
        while (lines[index].Length == 0)
        {
            index += 1;
        }
        ArrayExtensions.Resize(ref Data.Frames, int.Parse(lines[index].Substring(8)));
        index += 1;
        Data.Framerate = Mathf.RoundToInt(1.0f / float.Parse(lines[index].Substring(12)));
        index += 1;

        for (int i = index; i < lines.Length; i++)
        {
            motions.Add(ParseFloatArray(lines[i]));
        }

        for (int i = 0; i < Data.GetTotalFrames(); i++)
        {
            Data.Frames[i] = new Frame(Data, i + 1, (float)i / Data.Framerate);
            int channelIndex = 0;

            for(int j = 0; j < Data.Root.Bones.Length; j++)
            {
                MotionData.Hierarchy.Bone bone = Data.Root.Bones[j];
                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;

                for(int k = 0; k < channels[j].Length; k++)
                {
                    if(channels[j][k] == 1)
                    {
                        position.x = motions[i][channelIndex];
                        channelIndex++;
                    }
                    if (channels[j][k] == 2)
                    {
                        position.y = motions[i][channelIndex];
                        channelIndex++;
                    }
                    if (channels[j][k] == 3)
                    {
                        position.z = motions[i][channelIndex];
                        channelIndex++;
                    }
                    if (channels[j][k] == 4)
                    {
                        rotation *= Quaternion.AngleAxis(motions[i][channelIndex], Vector3.right);
                        channelIndex++;
                    }
                    if (channels[j][k] == 5)
                    {
                        rotation *= Quaternion.AngleAxis(motions[i][channelIndex], Vector3.up);
                        channelIndex++;
                    }
                    if (channels[j][k] == 6)
                    {
                        rotation *= Quaternion.AngleAxis(motions[i][channelIndex], Vector3.forward);
                        channelIndex++;
                    }
                }

                position = (position == Vector3.zero ? offsets[j] : position) / 100.0f;
                Matrix4x4 local = Matrix4x4.TRS(position, rotation, Vector3.one);
                Data.Frames[i].World[j] = bone.Parent == "None" ? local : Data.Frames[i].World[Data.Root.FindBone(bone.Parent).Index] * local;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Import File: " + FileName + " Successfully!");
    }

    public void Load()
    {
        if (Directory.Exists(Folder))
        { 
            DirectoryInfo info = new DirectoryInfo(Folder);
            FileInfo[] items = info.GetFiles("*.bvh");
            Files = new string[items.Length];
            Imports = new bool[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                Files[i] = items[i].Name;
                Imports[i] = true;
            }
        }
        else
        {
            Files = new string[0];
            Imports = new bool[0];
            Debug.Log("Folder Not Found!");
        }     
    }

    public string FixedBoneName(string str)
    {
        string[] name = str.Split(NameSpace);
        return name[name.Length - 1];
    }

    public string FixedFileName(string str)
    {
        string[] name = str.Split(NameSpace);
        return name[0];
    }

    public float[] ParseFloatArray(string str)
    {
        if (str.StartsWith(" "))
        {
            str = str.Substring(1);
        }
        if (str.EndsWith(" ") || str.EndsWith(""))
        {
            str = str.Substring(0, str.Length - 1);
        }

        string[] multiStr = str.Split(TextSpace);
        float[] result = new float[multiStr.Length];
        for (int i = 0; i < multiStr.Length; i++)
        {
            result[i] = float.Parse(multiStr[i]);
        }
        return result;
    }

    [CustomEditor(typeof(MotionImporter))]
    public class MotionImporterEditor : Editor
    {
        public MotionImporter Target;

        private bool IsExist = false;

        private void Awake()
        {
            Target = (MotionImporter)target;
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
                    Debug.Log("No BVH Motion Data File!");
                    return;
                }
                else
                    IsExist = true;
            }
            EditorGUILayout.EndHorizontal();

            if(IsExist)
            {
                Target.Destination = EditorGUILayout.TextField("Import Folder", "Assets/" + Target.Destination.Substring(Mathf.Min(7, Target.Destination.Length)));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Enable All"))
                {
                    for (int i = 0; i < Target.Files.Length; i++)
                        Target.Imports[i] = true;
                }
                if (GUILayout.Button("Disable All"))
                {
                    for (int i = 0; i < Target.Files.Length; i++)
                        Target.Imports[i] = false;
                }
                EditorGUILayout.EndHorizontal();

                for (int i = 0; i < Target.Files.Length; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    Target.Imports[i] = EditorGUILayout.Toggle(Target.Imports[i], GUILayout.Width(20.0f));
                    EditorGUILayout.LabelField(Target.Files[i]);
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Import"))
                    Target.Import();
            }
        }
    }
}



