#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

public class BVHCutter : MonoBehaviour
{
    public string BvhName = string.Empty;
    public string SavedName = string.Empty;
    public string FolderPath = string.Empty;
    public void CutData()
    {
        string[] lines = File.ReadAllLines(BvhName);
        int index = 0;

        for (index = 0; index < lines.Length; index++)
        {
            if(lines[index].Contains("LeftHand") || lines[index].Contains("RightHand"))
            {
                index += 4;

                for (int i = 0; i < 14; i++)
                    ArrayExtensions.RemoveAt(ref lines, index);

                if(lines[index].Contains("Left"))
                    lines[index] = lines[index].Replace("JOINT\tCharacter1_LeftHandMiddle1", "End Site");
                else
                    lines[index] = lines[index].Replace("JOINT\tCharacter1_RightHandMiddle1", "End Site");

                lines[index + 3] = lines[index + 13];

                index += 4;

                for(int i = 0; i < 52; i++)
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
            string[] entries = lines[index].Split('\t');
            string str = string.Empty;

            for(int i = 0; i < 30; i++)
                ArrayExtensions.RemoveAt(ref entries, 45);

            for (int i = 0; i < 30; i++)
                ArrayExtensions.RemoveAt(ref entries, 60);

            for(int i = 0; i < entries.Length - 1; i++)
                str += entries[i] + "\t";

            str += entries[entries.Length - 1];
            lines[index] = str;
        }

        string filePath = FolderPath + "\\" + SavedName;
        Debug.Log(filePath);

        using (var outputFile = new StreamWriter(filePath))
        {
            foreach (string line in lines)
                outputFile.WriteLine(line);
        }

        Debug.Log("Cut Successfully!");
    }

    [CustomEditor(typeof(BVHCutter))]
    public class BVHCutterEditor : Editor
    {
        public BVHCutter Target;

        private void Awake()
        {
            Target = (BVHCutter)target;
        }

        public override void OnInspectorGUI()
        {
            Target.BvhName = EditorGUILayout.TextField("File", "Assets/" + Target.BvhName.Substring(Mathf.Min(7, Target.BvhName.Length)));
            Target.SavedName = EditorGUILayout.TextField("Saved Name", Target.SavedName);
            Target.FolderPath = EditorGUILayout.TextField("Saved Folder", "Assets/" + Target.FolderPath.Substring(Mathf.Min(7, Target.FolderPath.Length)));

            if (GUILayout.Button("Cut"))
            {
                Target.CutData();
            }
        }
    }
}
#endif 