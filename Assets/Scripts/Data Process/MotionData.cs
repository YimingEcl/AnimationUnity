using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MotionData : ScriptableObject
{
    public Hierarchy Root = null;
    public Frame[] Frames = new Frame[0];
    public Module[] Modules = new Module[0];
    public float Framerate = 30.0f;
    public Vector3 Offset = Vector3.zero;
    public float Scale = 1.0f;
    public Axis MirrorAxis = Axis.XPositive; // Maya to Unity;

    public string GetName()
    {
        return name;
    }

    public float GetTotalTime()
    {
        return Frames.Length / Framerate;
    }

    public int GetTotalFrames()
    {
        return Frames.Length;
    }

    public Frame GetFrame(int index)
    {
        return Frames[Mathf.Clamp(index - 1, 0, Frames.Length - 1)];
    }

    public Frame GetFrame(float time)
    {
        return Frames[Mathf.Clamp(Mathf.RoundToInt(time * Framerate), 0, Frames.Length - 1)];
    }

    public Actor CreateActor()
    {
        Actor actor = new GameObject("Skeleton").AddComponent<Actor>();
        List<Transform> instances = new List<Transform>();

        for(int i = 0; i < Root.Bones.Length; i++)
        {
            Transform instance = new GameObject(Root.Bones[i].Name).transform;
            instance.SetParent(Root.Bones[i].Parent == "None" ? actor.GetRoot() : actor.FindTransform(Root.Bones[i].Parent));
            Matrix4x4 matrix = Frames.First().GetBoneTransformation(i, true);
            instance.position = matrix.GetPosition();
            instance.rotation = matrix.GetRotation();
            instance.localScale = Vector3.one;
            instances.Add(instance);
        }

        Transform root = actor.FindTransform(Root.Bones[0].Name);
        root.position = new Vector3(0f, root.position.y, 0f);
        root.rotation = Quaternion.Euler(root.eulerAngles.x, 0f, root.eulerAngles.z);
        actor.ExtractSkeleton(instances.ToArray());
        return actor;
    }

    public Module AddModule(Module.ID type)
    {
        Module module = System.Array.Find(Modules, x => x.GetID() == type);
        if (module != null)
            Debug.Log("Module of type " + type + " already exists in " + GetName() + ".");
        else
        {
            string id = type + "Module";
            module = (Module)ScriptableObject.CreateInstance(id);
            if (module == null)
                Debug.Log("Module of class type " + id + " could not be loaded in " + GetName() + ".");
            else
            {
                module.Init(this);
                ArrayExtensions.Add(ref Modules, module);
                AssetDatabase.AddObjectToAsset(Modules.Last(), this);
            }
        }
        return module;
    }

    public void RemoveModule(Module.ID type)
    {
        Module module = GetModule(type);
        if (!module)
            Debug.Log("Module of type " + type + " does not exist in " + GetName() + ".");
        else
        {
            ArrayExtensions.Remove(ref Modules, module);
            GameObject.DestroyImmediate(module, true);
        }
    }
            
    public Module GetModule(Module.ID type)
    {
        for (int i = 0; i < Modules.Length; i++)
        {
            if (Modules[i].GetID() == type)
                return Modules[i];
        }
        return null;
    }

    public void Save()
    {
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    [Serializable]
    public class Hierarchy
    {
        public Bone[] Bones;
        private string[] Names = null;

        public Hierarchy()
        {
            Bones = new Bone[0];
        }

        public void AddBone(string name, string parent)
        {
            ArrayExtensions.Add(ref Bones, new Bone(Bones.Length, name, parent));
        }

        public Bone FindBone(string name)
        {
            return Array.Find(Bones, x => x.Name == name);
        }

        public Bone FindBoneContains(string name)
        {
            return System.Array.Find(Bones, x => x.Name.Contains(name));
        }

        public string[] GetBoneNames()
        {
            if(Names == null || Names.Length != Bones.Length)
            {
                Names = new string[Bones.Length];
                for(int i = 0; i < Bones.Length; i++)
                    Names[i] = Bones[i].Name;
            }
            return Names;
        }

        [Serializable]
        public class Bone
        {
            public int Index = -1;
            public string Name = string.Empty;
            public string Parent = string.Empty;

            public Bone(int index, string name, string parent)
            {
                Index = index;
                Name = name;
                Parent = parent;
            }
        }
    }
}