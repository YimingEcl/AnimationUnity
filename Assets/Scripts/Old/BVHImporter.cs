using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BVHImporter
{
	public string fileName;
	public List<Bone> boneList;

	public int frames;
	public float frameTime;
	public int frameRate;
	private List<float[]> motions;
	private char[] space = new char[] { ' ', '\t', '\n', '\r' };

	public BVHImporter(string name)
	{
		this.fileName = name;
	}

	public class Bone
	{
		public string name;
		public string parent;
		public List<string> children;
		public Vector3 offsets;
		public int[] channelType;
		public Channel[] channels;

		public struct Channel
		{
			public bool enabled;
			public float[] value;
		}

		public Bone(string name, string parent)
		{
			this.name = name;
			this.parent = parent;
			children = new List<string>();
			offsets = Vector3.zero;
			channelType = new int[0];
			channels = new Channel[6];
		}
	}

	public void read()
	{
		boneList = new List<Bone>();
		motions = new List<float[]>();
		string[] lines = File.ReadAllLines(fileName);
		int index = 0;
		string name = string.Empty;
		string parent = string.Empty;

		for (index = 0; index < lines.Length; index++)
		{
			if (lines[index] == "MOTION")
			{
				break;
			}

			string[] entries = lines[index].Split(space);

			for (int entry = 0; entry < entries.Length; entry++)
			{
				if (entries[entry].Contains("ROOT"))
				{
					parent = "None";
					name = entries[entry + 1];
					name = fixedName(name);
					boneList.Add(new Bone(name, parent));
					break;
				}
				else if (entries[entry].Contains("JOINT"))
				{
					parent = name;
					name = entries[entry + 1];
					name = fixedName(name);
					boneList.Add(new Bone(name, parent));
					break;
				}
				else if (entries[entry].Contains("End"))
				{
					parent = name;
					name = name + entries[entry + 1];
					Bone endBone = new Bone(name, parent);
					string[] offsetEntries = lines[index + 2].Split(space);
					for (int offsetEntry = 0; offsetEntry < offsetEntries.Length; offsetEntry++)
					{
						if (offsetEntries[offsetEntry].Contains("OFFSET"))
						{
							endBone.offsets.x = float.Parse(offsetEntries[offsetEntry + 1]);
							endBone.offsets.y = float.Parse(offsetEntries[offsetEntry + 2]);
							endBone.offsets.z = float.Parse(offsetEntries[offsetEntry + 3]);
							break;
						}
					}
					boneList.Add(endBone);
					index += 2;
					break;
				}
				else if (entries[entry].Contains("OFFSET"))
				{
					getBone(name).offsets.x = float.Parse(entries[entry + 1]);
					getBone(name).offsets.y = float.Parse(entries[entry + 2]);
					getBone(name).offsets.z = float.Parse(entries[entry + 3]);
					break;
				}
				else if (entries[entry].Contains("CHANNEL"))
				{
					getBone(name).channelType = new int[int.Parse(entries[entry + 1])];
					for (int i = 0; i < getBone(name).channelType.Length; i++)
					{
						if (entries[entry + 2 + i] == "Xposition")
						{
							getBone(name).channelType[i] = 1;
							getBone(name).channels[0].enabled = true;
						}
						else if (entries[entry + 2 + i] == "Yposition")
						{
							getBone(name).channelType[i] = 2;
							getBone(name).channels[1].enabled = true;
						}
						else if (entries[entry + 2 + i] == "Zposition")
						{
							getBone(name).channelType[i] = 3;
							getBone(name).channels[2].enabled = true;
						}
						else if (entries[entry + 2 + i] == "Xrotation")
						{
							getBone(name).channelType[i] = 4;
							getBone(name).channels[3].enabled = true;
						}
						else if (entries[entry + 2 + i] == "Yrotation")
						{
							getBone(name).channelType[i] = 5;
							getBone(name).channels[4].enabled = true;
						}
						else if (entries[entry + 2 + i] == "Zrotation")
						{
							getBone(name).channelType[i] = 6;
							getBone(name).channels[5].enabled = true;
						}
					}
					break;
				}
				else if (entries[entry].Contains("}"))
				{
					if (parent != "None")
					{
						getBone(parent).children.Add(name);
					}
					name = parent;
					parent = name == "None" ? "None" : boneList.Find(x => x.name == name).parent;
					break;
				}
			}
		}

		index += 1;
		while (lines[index].Length == 0)
		{
			index += 1;
		}

		frames = int.Parse(lines[index].Substring(8));
		index += 1;
		frameTime = float.Parse(lines[index].Substring(12));
		frameRate = (int)(1.0f / frameTime);
		index += 1;

		for (int i = index; i < lines.Length; i++)
		{
			motions.Add(parseFloatArray(lines[i]));
		}

		int channelIndex = 0;

		foreach (Bone bone in boneList)
		{
			for (int i = 0; i < bone.channelType.Length; i++)
			{
				bone.channels[bone.channelType[i] - 1].value = new float[frames];
				for (int j = 0; j < frames; j++)
				{
					bone.channels[bone.channelType[i] - 1].value[j] = motions[j][channelIndex];
				}
				channelIndex++;
			}
		}
	}

	public float[] parseFloatArray(string str)
	{
		if (str.StartsWith(" "))
		{
			str = str.Substring(1);
		}
		if (str.EndsWith(" ") || str.EndsWith(""))
		{
			str = str.Substring(0, str.Length - 1);
		}

		string[] multiStr = str.Split(space);
		float[] result = new float[multiStr.Length];
		for (int i = 0; i < multiStr.Length; i++)
		{
			result[i] = float.Parse(multiStr[i]);
		}
		return result;
	}

	public Bone getBone(string name)
	{
		return boneList.Find(x => x.name == name);
	}

	public string fixedName(string str)
	{
		char[] option = { '_', ':' };
		string[] name = str.Split(option);
		return name[name.Length - 1];
	}

	public int getCount()
	{
		return boneList.Count;
	}
	
}

