using System;
using System.IO;
using UnityEngine;
using UnityEditor;

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

    public string[] GenerateLines(MotionData data)
    {
        string[] result = new string[data.Frames.Length - 60];

        for (int i = 30; i < data.Frames.Length-30; i++)
        {
            if (data.Modules != null)
            {
                for (int k = 0; k < data.Modules.Length; k++)
                {
                    switch (data.Modules[k].GetID())
                    {
                        case Module.ID.Velocity:
                            {
                                VelocityModule module = (VelocityModule)data.GetModule(Module.ID.Velocity);
                                module.GetTransformations(data.GetFrame(i));
                                module.GetVelocities(data.GetFrame(i), 1.0f);
                                for (int j = 0; j < data.Root.Bones.Length; j++)
                                {
                                    if (module.Selected[j])
                                    {
                                        result[i - 30] += data.Frames[i].GetBoneTransformation(j, data.Mirrored).GetPosition().x.ToString() + " ";
                                        result[i - 30] += data.Frames[i].GetBoneTransformation(j, data.Mirrored).GetPosition().y.ToString() + " ";
                                        result[i - 30] += data.Frames[i].GetBoneTransformation(j, data.Mirrored).GetPosition().z.ToString() + " ";
                                        result[i - 30] += data.Frames[i].GetBoneTransformation(j, data.Mirrored).GetForward().x.ToString() + " ";
                                        result[i - 30] += data.Frames[i].GetBoneTransformation(j, data.Mirrored).GetForward().y.ToString() + " ";
                                        result[i - 30] += data.Frames[i].GetBoneTransformation(j, data.Mirrored).GetForward().z.ToString() + " ";
                                        result[i - 30] += data.Frames[i].GetBoneTransformation(j, data.Mirrored).GetUp().x.ToString() + " ";
                                        result[i - 30] += data.Frames[i].GetBoneTransformation(j, data.Mirrored).GetUp().y.ToString() + " ";
                                        result[i - 30] += data.Frames[i].GetBoneTransformation(j, data.Mirrored).GetUp().z.ToString() + " ";
                                        result[i - 30] += module.Velocities[j].x.ToString() + " ";
                                        result[i - 30] += module.Velocities[j].y.ToString() + " ";
                                        result[i - 30] += module.Velocities[j].z.ToString() + " ";
                                    }
                                }
                                break;
                            }

                        case Module.ID.Action:
                            {
                                ActionModule module = (ActionModule)data.GetModule(Module.ID.Action);
                                result[i - 30] += module.GetHotVector(i) + " ";

                                break;
                            }

                        case Module.ID.Phase:
                            {
                                PhaseModule module = (PhaseModule)data.GetModule(Module.ID.Phase);
                                result[i - 30] += module.Phases[0].LocalPhase.Phase[i] + " ";
                                result[i - 30] += module.Phases[1].LocalPhase.Phase[i] + " ";
                                break;
                            }

                        case Module.ID.Trajectory:
                            {
                                TrajectoryModule module = (TrajectoryModule)data.GetModule(Module.ID.Trajectory);
                                for (int j = 0; j < module.Pivots.Length; j++)
                                {
                                    for (int p = 0; p < module.Pivots[j].Transformations.Length; p++)
                                    {
                                        result[i - 30] += module.Pivots[j].Transformations[p].GetPosition().x.ToString() + " ";
                                        result[i - 30] += module.Pivots[j].Transformations[p].GetPosition().y.ToString() + " ";
                                        result[i - 30] += module.Pivots[j].Transformations[p].GetPosition().z.ToString() + " ";
                                        result[i - 30] += module.Pivots[j].Velocities[p].x.ToString() + " ";
                                        result[i - 30] += module.Pivots[j].Velocities[p].y.ToString() + " ";
                                        result[i - 30] += module.Pivots[j].Velocities[p].z.ToString() + " ";
                                        result[i - 30] += module.Pivots[j].HotVectors[p] + " ";
                                        result[i - 30] += module.Pivots[j].Phases[p].ToString() + " ";
                                    }
                                }
                                break;
                            }
                    }
                }
            }
        }

        return result;
    }

    public void ExportNormal(MotionData data)
    {
        Lines = GenerateLines(data);

        string InputData = Destination + "/" + "Input.txt";
        string OutputData = Destination + "/" + "Output.txt";

        if (!File.Exists(InputData))
        {
            using (FileStream fs = File.Create(InputData)) { }
            Debug.Log("Create new file: " + InputData);
            File.AppendAllLines(InputData, Lines);
        }
        else
        {
            File.AppendAllLines(InputData, Lines);
        }

        if (!File.Exists(OutputData))
        {
            using (FileStream fs = File.Create(OutputData)) { }
            Debug.Log("Create new file: " + OutputData);
            File.AppendAllLines(OutputData, Lines);
        }
        else
        {
            File.AppendAllLines(OutputData, Lines);
        }

        Debug.Log("Export Training MotionData: " + data.GetName() + "Successfully!");
    }

    public void ExportTest(MotionData data)
    {
        Lines = GenerateLines(data);
        string InputData = Destination + "/" + "TestInput.txt";
        string OutputData = Destination + "/" + "TestOutput.txt";

        if (!File.Exists(InputData))
        {
            using (FileStream fs = File.Create(InputData)) { }
            Debug.Log("Create new file: " + InputData);
            File.AppendAllLines(InputData, Lines);
        }
        else
        {
            File.AppendAllLines(InputData, Lines);
        }

        if (!File.Exists(OutputData))
        {
            using (FileStream fs = File.Create(OutputData)) { }
            Debug.Log("Create new file: " + OutputData);
            File.AppendAllLines(OutputData, Lines);
        }
        else
        {
            File.AppendAllLines(OutputData, Lines);
        }

        Debug.Log("Export Test MotionData: " + data.GetName() + "Successfully!");
    }

    public void ExportLabels(MotionData data)
    {
        Lines = new string[0];
        if (data.Modules != null)
        {
            for (int k = 0; k < data.Modules.Length; k++)
            {
                switch (data.Modules[k].GetID())
                {
                    case Module.ID.Velocity:
                        {
                            VelocityModule module = (VelocityModule)data.GetModule(Module.ID.Velocity);
                            module.GetTransformations(data.GetFrame(0));
                            module.GetVelocities(data.GetFrame(0), 1.0f);
                            for (int j = 0; j < data.Root.Bones.Length; j++)
                            {
                                if (module.Selected[j])
                                {
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "PositionX");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "PositionY");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "PositionZ");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "ForwardX");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "ForwardY");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "ForwardZ");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "UpX");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "UpY");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "UpZ");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "VelocityX");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "VelocityY");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + data.Root.Bones[j].Name + "VelocityZ");
                                }
                            }
                            break;
                        }

                    case Module.ID.Action:
                        {
                            ActionModule module = (ActionModule)data.GetModule(Module.ID.Action);
                            ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Action Neutral");
                            ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Action LHOnHip");
                            ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Action RHOnHip");

                            break;
                        }

                    case Module.ID.Phase:
                        {
                            PhaseModule module = (PhaseModule)data.GetModule(Module.ID.Phase);
                            ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Phase LeftHand");
                            ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Phase RightHand");
                            break;
                        }

                    case Module.ID.Trajectory:
                        {
                            TrajectoryModule module = (TrajectoryModule)data.GetModule(Module.ID.Trajectory);
                            for (int j = 0; j < module.Pivots.Length; j++)
                            {
                                for (int p = 0; p < module.Pivots[j].Transformations.Length; p++)
                                {
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() + 
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "PositionX");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() +
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "PositionY");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() +
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "PositionZ");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() +
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "VelocityX");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() +
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "VelocityY");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() +
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "VelocityZ");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() +
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "Action Neutral");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() +
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "Action LHOnHip");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() +
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "Action RHOnHip");
                                    ArrayExtensions.Add(ref Lines, "[" + Lines.Length.ToString() + "] " + "Pivot" + j.ToString() +
                                        module.Pivots[j].Name + "Trajectory" + p.ToString() + "Phase");
                                }
                            }
                            break;
                        }
                }
            }
        }

        string FileName = Destination + "/" + "Labels.txt";

        if (!File.Exists(FileName))
        {
            using (FileStream fs = File.Create(FileName)) { }
            Debug.Log("Create new file: " + FileName);
            File.AppendAllLines(FileName, Lines);
        }
        else
        {
            File.WriteAllLines(FileName, Lines);
        }

        Debug.Log("Export Labels Successfully!");
    }


    [CustomEditor(typeof(MotionExporter))]
    public class MotionExporterEditor : Editor
    {
        public MotionExporter Target;

        private bool IsExist = false;
        private bool Existed = false;

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

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Export Training Data"))
                {
                    Existed = false;
                    for(int i = 0; i < Target.Files.Length; i++)
                    {
                        if (Target.Exports[i])
                        {
                            if(!Existed)
                            {
                                Target.ExportNormal(Target.Files[i]);
                                Target.ExportLabels(Target.Files[i]);
                                Existed = true;
                            }
                            else
                                Target.ExportNormal(Target.Files[i]);
                        }
                    }
                }

                if (GUILayout.Button("Export Test Data"))
                {
                    Existed = false;
                    for (int i = 0; i < Target.Files.Length; i++)
                    {
                        if (Target.Exports[i])
                        {
                            if (!Existed)
                            {
                                Target.ExportTest(Target.Files[i]);
                                Target.ExportLabels(Target.Files[i]);
                                Existed = true;
                            }
                            else
                                Target.ExportTest(Target.Files[i]);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
