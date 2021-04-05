using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class ParaImporter : MonoBehaviour
{
    public string Folder = string.Empty;
    public string[] Files = null;
    public void Load()
    {
        if (Directory.Exists(Folder))
        {
            DirectoryInfo info = new DirectoryInfo(Folder);
            FileInfo[] items = info.GetFiles("*.bin");
            Files = new string[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                Files[i] = items[i].Name;
            }
        }
        else
        {
            Files = new string[0];
            Debug.Log("Folder Not Found!");
        }
    }

    [CustomEditor(typeof(ParaImporter))]
    public class ParaImporterEditor : Editor
    {
        public ParaImporter Target;
        private bool IsExist = false;

        private void Awake()
        {
            Target = (ParaImporter)target;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();
            Target.Folder = EditorGUILayout.TextField("Parameter Folder", "Assets/" + Target.Folder.Substring(Mathf.Min(7, Target.Folder.Length)));
            if (GUILayout.Button("Load"))
            {
                Target.Load();
                if (Target.Files.Length == 0)
                {
                    Debug.Log("No Parameter Data!");
                    return;
                }
                else
                    IsExist = true;
            }
            EditorGUILayout.EndHorizontal();

            if (IsExist)
            {
                for (int i = 0; i < Target.Files.Length; i++)
                {
                    EditorGUILayout.LabelField(Target.Files[i]);
                }

                if (GUILayout.Button("Import"))
                    Parameters.Import(Target.Folder);
            }
        }
    }
}
