using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class RootModule : Module
{
	public int RightShoulder, LeftShoulder, RightUpLeg, LeftUpLeg, Neck, Hips;
	public Axis ForwardAxis = Axis.ZPositive;

	public override ID GetID()
	{
		return ID.Root;
	}

	public override Module Init(MotionData data)
	{
		Data = data;
		Setup();
		return this;
	}

	public override void DerivedInspector(MotionEditor editor)
	{
		RightShoulder = EditorGUILayout.Popup("Right Shoulder", RightShoulder, Data.Root.GetBoneNames());
		LeftShoulder = EditorGUILayout.Popup("Left Shoulder", LeftShoulder, Data.Root.GetBoneNames());
		RightUpLeg = EditorGUILayout.Popup("Right Up Leg", RightUpLeg, Data.Root.GetBoneNames());
		LeftUpLeg = EditorGUILayout.Popup("Left Up Leg", LeftUpLeg, Data.Root.GetBoneNames());
		Neck = EditorGUILayout.Popup("Neck", Neck, Data.Root.GetBoneNames());
		Hips = EditorGUILayout.Popup("Hips", Hips, Data.Root.GetBoneNames());
		ForwardAxis = (Axis)EditorGUILayout.EnumPopup("Forward Axis", ForwardAxis);
	}

	public void Setup()
	{
		MotionData.Hierarchy.Bone rs = Data.Root.FindBoneContains("RightShoulder");
		RightShoulder = rs == null ? 0 : rs.Index;
		MotionData.Hierarchy.Bone ls = Data.Root.FindBoneContains("LeftShoulder");
		LeftShoulder = ls == null ? 0 : ls.Index;
		MotionData.Hierarchy.Bone rh = Data.Root.FindBoneContains("RightUpLeg");
		RightUpLeg = rh == null ? 0 : rh.Index;
		MotionData.Hierarchy.Bone lh = Data.Root.FindBoneContains("LeftUpLeg");
		LeftUpLeg = lh == null ? 0 : lh.Index;
		MotionData.Hierarchy.Bone n = Data.Root.FindBoneContains("Neck");
		Neck = n == null ? 0 : n.Index;
		MotionData.Hierarchy.Bone h = Data.Root.FindBoneContains("Hips");
		Hips = h == null ? 0 : h.Index;
	}

	public Matrix4x4 GetRootTransformation(Frame frame, bool mirrored)
	{
		return Matrix4x4.TRS(GetRootPosition(frame, mirrored), GetRootRotation(frame, mirrored), Vector3.one);
	}

	public Vector3 GetRootPosition(Frame frame, bool mirrored)
	{
		return frame.GetBoneTransformation(0, mirrored).GetPosition();
	}

	public Quaternion GetRootRotation(Frame frame, bool mirrored)
	{
		Vector3 v1 = Vector3.ProjectOnPlane(frame.GetBoneTransformation(RightUpLeg, mirrored).GetPosition() - frame.GetBoneTransformation(LeftUpLeg, mirrored).GetPosition(), Vector3.up).normalized;
		Vector3 v2 = Vector3.ProjectOnPlane(frame.GetBoneTransformation(RightShoulder, mirrored).GetPosition() - frame.GetBoneTransformation(LeftShoulder, mirrored).GetPosition(), Vector3.up).normalized;
		Vector3 v = (v1 + v2).normalized;
		Vector3 forward = Vector3.ProjectOnPlane(-Vector3.Cross(v, Vector3.up), Vector3.up).normalized;
		return forward == Vector3.zero ? Quaternion.identity : Quaternion.LookRotation(forward, Vector3.up);
	}

	public Vector3 GetRootVelocity(Frame frame, bool mirrored, float delta)
	{
		return Vector3.ProjectOnPlane(frame.GetBoneVelocity(0, mirrored, delta), Vector3.up);
	}

	public Matrix4x4 GetEstimatedRootTransformation(Frame reference, float offset, bool mirrored)
	{
		return Matrix4x4.TRS(GetEstimatedRootPosition(reference, offset, mirrored), GetEstimatedRootRotation(reference, offset, mirrored), Vector3.one);
	}

	public Vector3 GetEstimatedRootPosition(Frame reference, float offset, bool mirrored)
	{
		float t = reference.Timestamp + offset;
		if (t < 0f || t > Data.GetTotalTime())
		{
			float boundary = Mathf.Clamp(t, 0f, Data.GetTotalTime());
			float pivot = 2f * boundary - t;
			float clamped = Mathf.Clamp(pivot, 0f, Data.GetTotalTime());
			return 2f * GetRootPosition(Data.GetFrame(boundary), mirrored) - GetRootPosition(Data.GetFrame(clamped), mirrored);
		}
		else
		{
			return GetRootPosition(Data.GetFrame(t), mirrored);
		}
	}

	public Quaternion GetEstimatedRootRotation(Frame reference, float offset, bool mirrored)
	{
		float t = reference.Timestamp + offset;
		if (t < 0f || t > Data.GetTotalTime())
		{
			float boundary = Mathf.Clamp(t, 0f, Data.GetTotalTime());
			float pivot = 2f * boundary - t;
			float clamped = Mathf.Clamp(pivot, 0f, Data.GetTotalTime());
			return GetRootRotation(Data.GetFrame(clamped), mirrored);
		}
		else
		{
			return GetRootRotation(Data.GetFrame(t), mirrored);
		}
	}

	public Vector3 GetEstimatedRootVelocity(Frame reference, float offset, bool mirrored, float delta)
	{
		return (GetEstimatedRootPosition(reference, offset + delta, mirrored) - GetEstimatedRootPosition(reference, offset, mirrored)) / delta;
	}
}
