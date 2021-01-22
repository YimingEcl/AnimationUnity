#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

public class MotionCutter : MonoBehaviour
{
    public string[] FileNames = null;
    public string Folder = string.Empty;
    public string SavedName = string.Empty;
    public string Destination = string.Empty;

    private char[] NameSpace = new char[] { '_', ':', '.' };

    public void Load()
    {
        if (Directory.Exists(Folder))
        {
            DirectoryInfo info = new DirectoryInfo(Folder);
            FileInfo[] items = info.GetFiles("*.bvh");
            FileNames = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                FileNames[i] = items[i].Name;
            }
        }
        else
        {
            Debug.Log("Folder Not Found!");
        }
    }

    public void CutData(string name)
    {
        string[] lines = File.ReadAllLines(Folder + "/" + name);
        int index = 0;

        for (index = 0; index < lines.Length; index++)
        {
            if(lines[index].Contains("LeftHand") || lines[index].Contains("RightHand"))
            {
                index += 4;

                for (int i = 0; i < 48; i++)
                    ArrayExtensions.RemoveAt(ref lines, index);

                if(lines[index].Contains("Left"))
                    lines[index] = lines[index].Replace("JOINT TKVR01_LeftHandMiddle1", "End Site");
                else
                    lines[index] = lines[index].Replace("JOINT TKVR01_RightHandMiddle1", "End Site");

                lines[index + 3] = lines[index + 23];

                index += 4;

                for(int i = 0; i < 77; i++)
                    ArrayExtensions.RemoveAt(ref lines, index);
            }

            if (lines[index].Contains("MOTION"))
            {
                index += 3;
                break;
            }
        }

        for(; index < lines.Length; index++)
        {
            string[] entries = lines[index].Split(' ');
            string str = string.Empty;

            for(int i = 0; i < 63; i++)
                ArrayExtensions.RemoveAt(ref entries, 33);

            for (int i = 0; i < 63; i++)
                ArrayExtensions.RemoveAt(ref entries, 45);

            for(int i = 0; i < entries.Length - 1; i++)
                str += entries[i] + " ";

            str += entries[entries.Length - 1];
            lines[index] = str;
        }

        string filePath = Destination + "/" + FixedFileName(name) + SavedName + ".bvh";

        using (var outputFile = new StreamWriter(filePath))
        {
            foreach (string line in lines)
                outputFile.WriteLine(line);
        }

        Debug.Log("Cut File: " + name + " Successfully!");
    }

    public string FixedFileName(string str)
    {
        string[] name = str.Split(NameSpace);
        return name[0];
    }

    [CustomEditor(typeof(MotionCutter))]
    public class MotionCutterEditor : Editor
    {
        public MotionCutter Target;

        private int Selected;

        private void Awake()
        {
            Target = (MotionCutter)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();
            Target.Folder = EditorGUILayout.TextField("BVH Folder", "Assets/" + Target.Folder.Substring(Mathf.Min(7, Target.Folder.Length)));
            if (GUILayout.Button("Load"))
                Target.Load();
            EditorGUILayout.EndHorizontal();

            if (Target.FileNames != null)
            {
                Target.Destination = EditorGUILayout.TextField("Saved Folder", "Assets/" + Target.Destination.Substring(Mathf.Min(7, Target.Destination.Length)));
                Target.SavedName = EditorGUILayout.TextField("Saved Name", Target.SavedName);

                EditorGUILayout.BeginHorizontal();
                Selected = EditorGUILayout.Popup("Select Data", Selected, Target.FileNames);
                if (GUILayout.Button("Cut"))
                    Target.CutData(Target.FileNames[Selected]);
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif 