﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class Actor : MonoBehaviour
{
	public Bone[] Bones = new Bone[0];
	public bool DrawSkeleton = true;
	public bool DrawVelocities = true;
	public bool DrawTransforms = true;
	public bool TargetRoot = true;
	public float BoneSize = 0.035f;

	private Color BoneColor = Color.cyan;
	private Color JointColor = Color.red;
    private void Reset()
	{
		ExtractSkeleton();
	}

	private void OnRenderObject()
	{
		Draw();
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying)
			OnRenderObject();
	}

	public void Draw()
	{
		Draw(BoneColor, JointColor, 1.0f);
	}

	public void Draw(Color boneColor, Color jointColor, float alpha)
	{
		UltiDraw.Begin();

		if(DrawSkeleton)
		{
			Action<Bone> recursion = null;
			recursion = new Action<Bone>((bone) => 
			{
				if(bone.Visiable)
                {
					if (bone.GetParent() != null)
					{
						UltiDraw.DrawBone
						(
							bone.GetParent().Transform.position,
							Quaternion.FromToRotation(bone.GetParent().Transform.forward, bone.Transform.position - bone.GetParent().Transform.position) * bone.GetParent().Transform.rotation,
							12.5f * BoneSize * bone.GetLength(), bone.GetLength(),
							boneColor.Transparent(alpha)
						);
					}
					UltiDraw.DrawSphere(
						bone.Transform.position,
						Quaternion.identity,
						3f / 8f * BoneSize,
						jointColor.Transparent(alpha)
					);
				}
				for (int i = 0; i < bone.Childs.Length; i++)
					recursion(bone.GetChild(i));
			});
			if (Bones.Length > 0)
				recursion(Bones[0]);
		}

		if (DrawVelocities)
		{
			for (int i = 0; i < Bones.Length; i++)
			{
				if (Bones[i].Visiable)
				{
					UltiDraw.DrawArrow(
					Bones[i].Transform.position,
					Bones[i].Transform.position + Bones[i].Velocity,
					0.25f,
					0.025f,
					0.2f,
					UltiDraw.DarkGreen.Transparent(0.5f)
				    );
				}
			}
		}

		if (DrawTransforms)
		{
			Action<Bone> recursion = null;
			recursion = new Action<Bone>((bone) => 
			{
				if(bone.Visiable)
                {
					UltiDraw.DrawTranslateGizmo(bone.Transform.position, bone.Transform.rotation, 0.05f);
					for (int i = 0; i < bone.Childs.Length; i++)
						recursion(bone.GetChild(i));
				}
			});
			if (Bones.Length > 0)
			{
				recursion(Bones[0]);
			}
		}

		UltiDraw.End();
	}

	public Transform GetRoot()
	{
		return transform;
	}

	public Transform FindTransform(string name)
	{
		Transform element = null;
		Action<Transform> recursion = null;
		recursion = new Action<Transform>((transform) => {
			if (transform.name == name)
			{
				element = transform;
				return;
			}
			for (int i = 0; i < transform.childCount; i++)
			{
				recursion(transform.GetChild(i));
			}
		});
		recursion(GetRoot());
		return element;
	}

	public void ExtractSkeleton()
	{
		ArrayExtensions.Clear(ref Bones);
		Action<Transform, Bone> recursion = null;
		recursion = new Action<Transform, Bone>((transform, parent) => {
			Bone bone = new Bone(this, transform, Bones.Length);
			ArrayExtensions.Add(ref Bones, bone);
			if (parent != null)
			{
				bone.Parent = parent.Index;
				bone.Level = parent.Level + 1;
				bone.ComputeLength();
				ArrayExtensions.Add(ref parent.Childs, bone.Index);
			}
			parent = bone;
			for (int i = 0; i < transform.childCount; i++)
			{
				recursion(transform.GetChild(i), parent);
			}
		});
		recursion(GetRoot(), null);
	}

	public void ExtractSkeleton(Transform[] bones)
	{
		ArrayExtensions.Clear(ref Bones);
		Action<Transform, Bone> recursion = null;
		recursion = new Action<Transform, Bone>((transform, parent) => {
			if (System.Array.Find(bones, x => x == transform))
			{
				Bone bone = new Bone(this, transform, Bones.Length);
				ArrayExtensions.Add(ref Bones, bone);
				if (parent != null)
				{
					bone.Parent = parent.Index;
					bone.ComputeLength();
					ArrayExtensions.Add(ref parent.Childs, bone.Index);
				}
				parent = bone;
			}
			for (int i = 0; i < transform.childCount; i++)
			{
				recursion(transform.GetChild(i), parent);
			}
		});
		recursion(GetRoot(), null);
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
		string[] names = new string[Bones.Length];
		for(int i = 0; i < Bones.Length; i++)
			names[i] = Bones[i].Name;

		return names;
    }

	[Serializable]
	public class Bone
	{
		public Actor Actor;
		public Transform Transform;
		public Vector3 Velocity;
		public string Name;
		public int Index;
		public int Parent;
		public int[] Childs;
		public int Level;
		public float Length;
		public bool Visiable;


		public Bone(Actor actor, Transform transform, int index)
		{
			Actor = actor;
			Transform = transform;
			Velocity = Vector3.zero;
			Name = transform.name;
			Index = index;
			Parent = -1;
			Childs = new int[0];
			Level = 0;
			Length = 0.0f;
			Visiable = true;
		}

		public string GetName()
		{
			return Name;
		}

		public string GetFixedName()
        {
			char[] space = new char[] { ' ', ':', '_' };
			string[] fixedName = Name.Split(space);
			return fixedName[fixedName.Length - 1];
        }

		public Bone GetParent()
		{
			return Parent == -1 ? null : Actor.Bones[Parent];
		}

		public Bone GetChild(int index)
		{
			return index >= Childs.Length ? null : Actor.Bones[Childs[index]];
		}

		public void SetLength(float value)
		{
			Length = Mathf.Max(float.MinValue, value);
		}

		public float GetLength()
		{
			return Length;
		}

		public void ComputeLength()
		{
			Length = GetParent() == null ? 0f : Vector3.Distance(GetParent().Transform.position, Transform.position);
		}

		public void ApplyLength()
		{
			if (GetParent() != null)
			{
				Transform.position = GetParent().Transform.position + Length * (Transform.position - GetParent().Transform.position).normalized;
			}
		}

		public Actor GetActor()
        {
			return Actor;
        }
	}

	[CustomEditor(typeof(Actor))]
	public class ActorEditor : Editor
	{
		public Actor Target;
		private bool Visiable = false;
		private bool BoneVisiable = false;

		private void Awake()
		{
			Target = (Actor)target;
		}

		public override void OnInspectorGUI()
		{
			Target.DrawSkeleton = EditorGUILayout.Toggle("Draw Skeleton", Target.DrawSkeleton);
			Target.DrawVelocities = EditorGUILayout.Toggle("Draw Velocities", Target.DrawVelocities);
			Target.DrawTransforms = EditorGUILayout.Toggle("Draw Transforms", Target.DrawTransforms);
			Target.TargetRoot = EditorGUILayout.Toggle("Set Root by BVH Data", Target.TargetRoot);
			Target.BoneSize = EditorGUILayout.FloatField("Bone Size:", Target.BoneSize);
			using (new EditorGUILayout.VerticalScope("Box"))
			{
				EditorGUILayout.BeginHorizontal();
				Visiable = EditorGUILayout.Toggle(Visiable, GUILayout.Width(20.0f));
				EditorGUILayout.LabelField("Show All Bone", GUILayout.Width(300.0f));
				EditorGUILayout.EndHorizontal();

				if(Visiable)
                {
					EditorGUILayout.BeginHorizontal();
					if (GUILayout.Button("Enable All"))
					{
						for (int i = 0; i < Target.Bones.Length; i++)
							Target.Bones[i].Visiable = true;
					}
					if (GUILayout.Button("Disable All"))
					{
						for (int i = 0; i < Target.Bones.Length; i++)
							Target.Bones[i].Visiable = false;
					}
					EditorGUILayout.EndHorizontal();

					for(int i = 0; i < Target.Bones.Length; i++)
                    {
						EditorGUILayout.BeginHorizontal();
						BoneVisiable = EditorGUILayout.Toggle(Target.Bones[i].Visiable, GUILayout.Width(20.0f));

						if(Target.Bones[i].Visiable != BoneVisiable)
							ToggleBoneVisiable(Target.Bones[i]);

						for (int j = 0; j < Target.Bones[i].Level; j++)
							EditorGUILayout.LabelField("--", GUILayout.Width(15.0f));
						EditorGUILayout.LabelField(Target.Bones[i].Name);
						EditorGUILayout.EndHorizontal();
					}
				}
			}
		}

		public void ToggleBoneVisiable(Bone bone)
        {
			bone.Visiable = BoneVisiable;
			for (int i = 0; i < bone.Childs.Length; i++)
			{
				ToggleBoneVisiable(bone.GetChild(i));
			}
		}
	}
}
