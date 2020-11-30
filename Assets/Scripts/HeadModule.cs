using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class HeadModule : Module
{
	public override ID GetID()
	{
		return ID.Head;
	}

	public override Module Init(MotionData data)
	{
		Data = data;
		return this;
	}

	public override void DerivedInspector(MotionEditor editor)
	{
	}
}
