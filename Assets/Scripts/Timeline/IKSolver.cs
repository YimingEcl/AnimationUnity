using System;
using UnityEngine;
using UnityEditor;

public class IKSolver : MonoBehaviour
{
    public Actor MyActor;
    public IKChain[] Chains;
    public int MaxItr = 20;
    public float ErrThrehold = 1.0e-5f;
    public int SolverType = 0;

    private void Start()
    {
        for (int i = 0; i < Chains.Length; i++)
        {
            SetChainIndices(i);
        }
    }

    private void Update()
    {
        switch (SolverType)
        {
            case 0:
                for (int i = 0; i < Chains.Length; i++)
                    FABRIKSolver(i);
                break;
            case 1:
                for (int i = 0; i < Chains.Length; i++)
                    CCDSolver(i);
                break;
        }
    }

    public void CCDSolver(int index)
    {
        Vector3 targetPos = Chains[index].TargetObject.transform.position;
        Vector3 localEffectorPos = Vector3.zero;
        Vector3 localTargetPos = Vector3.zero;
        Actor.Bone root = MyActor.Bones[Chains[index].RootIndex];
        Actor.Bone effector = MyActor.Bones[Chains[index].EffectorIndex];

        for (int itr = 0; itr < MaxItr; itr++)
        {
            for (Actor.Bone joint = MyActor.Bones[effector.Parent]; joint != null; joint = MyActor.Bones[joint.Parent])
            {
                Vector3 effectorPos = effector.Transform.position;
                Matrix4x4 worldToLocal = joint.Transform.worldToLocalMatrix;

                localEffectorPos = worldToLocal.MultiplyPoint(effectorPos);
                localTargetPos = worldToLocal.MultiplyPoint(targetPos);

                Vector3 toEffectorNorm = Vector3.Normalize(localEffectorPos);
                Vector3 toTargetNorm = Vector3.Normalize(localTargetPos);

                float rotationAngle = Vector3.Angle(toEffectorNorm, toTargetNorm);
                if (rotationAngle > 1.0e-5f)
                {
                    Vector3 rotationAxis = Vector3.Cross(toEffectorNorm, toTargetNorm);
                    rotationAxis.Normalize();
                    joint.Transform.rotation = Quaternion.AngleAxis(rotationAngle, rotationAxis) * joint.Transform.rotation;
                }

                if (joint == root)
                    break;  
            }

            if (Vector3.Distance(localEffectorPos, localTargetPos) < ErrThrehold)
                return;
        }
    }

    public void FABRIKSolver(int index)
    {
        int rootIndex = Chains[index].RootIndex;
        int effectorIndex = Chains[index].EffectorIndex;
        Vector3 targetPos = Chains[index].TargetObject.transform.position;
        Vector3 rootPos = MyActor.Bones[rootIndex].Transform.position;
        Vector3 effectorPos = MyActor.Bones[effectorIndex].Transform.position;

        int[] indices = Chains[index].JointIndices;
        int length = indices.Length;
        float[] dists = new float[length];
        Vector3[] originPos = new Vector3[length];
        float totalLength = 0.0f;

        // get bone length
        for (int i = 0; i < length; i++)
        {
            dists[i] = MyActor.Bones[indices[i]].Length;
            originPos[i] = MyActor.Bones[indices[i]].Transform.position;
            if (indices[i] >= rootIndex)
                totalLength += dists[i];
        }

        // unreachable
        if (Vector3.Distance(targetPos, rootPos) > totalLength)
        {
            for (int i = 0; i < length - 1; i++)
            {
                float r = Vector3.Distance(targetPos, MyActor.Bones[indices[i]].Transform.position);
                float lambda = dists[i + 1] / r;
                MyActor.Bones[indices[i+1]].Transform.position = (1.0f - lambda) * MyActor.Bones[indices[i]].Transform.position + lambda * targetPos;
            }
        }
        else // reachable
        {
            for (int itr = 0; itr < MaxItr; itr++)
            {
                if (Vector3.Distance(effectorPos, targetPos) < ErrThrehold)
                    break;

                // forward reaching
                MyActor.Bones[effectorIndex].Transform.position = targetPos;
                for (int i = length - 2; i >= 0; i--)
                {
                    float r = Vector3.Distance(MyActor.Bones[indices[i + 1]].Transform.position, MyActor.Bones[indices[i]].Transform.position);
                    float lambda = dists[i + 1] / r;
                    MyActor.Bones[indices[i]].Transform.position = (1.0f - lambda) * MyActor.Bones[indices[i + 1]].Transform.position
                        + lambda * MyActor.Bones[indices[i]].Transform.position;
                }

                // backward reaching
                MyActor.Bones[rootIndex].Transform.position = rootPos;
                for (int i = 0; i < length - 1; i++)
                {
                    float r = Vector3.Distance(MyActor.Bones[indices[i + 1]].Transform.position, MyActor.Bones[indices[i]].Transform.position);
                    float lambda = dists[i + 1] / r;
                    MyActor.Bones[indices[i + 1]].Transform.position = (1.0f - lambda) * MyActor.Bones[indices[i]].Transform.position
                        + lambda * MyActor.Bones[indices[i + 1]].Transform.position;
                }
            }
        }

        // fix rotation
        if (length > 1)
        {
            for (int i = length - 2; i >= 0; i--)
            {
                Vector3 oldToward = originPos[i + 1] - originPos[i];
                Vector3 newToward = MyActor.Bones[indices[i + 1]].Transform.position - MyActor.Bones[indices[i]].Transform.position;
                float rotationAngle = Vector3.Angle(oldToward, newToward);
                if (rotationAngle > 1.0e-5f)
                {
                    Vector3 rotationAxis = Vector3.Cross(oldToward, newToward);
                    MyActor.Bones[indices[i]].Transform.RotateAround(MyActor.Bones[indices[i]].Transform.position, rotationAxis, rotationAngle);
                    MyActor.Bones[indices[i + 1]].Transform.RotateAround(MyActor.Bones[indices[i]].Transform.position, rotationAxis, -rotationAngle);
                }
            }
        }
    }

    //public void ParticleIK()
    //{

    //}

    private void SetChainIndices(int index)
    {
        int[] indices = new int[0];
        Actor.Bone root = MyActor.Bones[Chains[index].RootIndex];
        Actor.Bone effector = MyActor.Bones[Chains[index].EffectorIndex];
        Actor.Bone current = effector;

        while(current != root)
        {
            ArrayExtensions.Add(ref indices, current.Index);
            current = MyActor.Bones[current.Parent];
        }

        ArrayExtensions.Add(ref indices, root.Index);
        Array.Reverse(indices, 0, indices.Length);

        Chains[index].JointIndices = indices;
    }


    [System.Serializable]
    public class IKChain
    {
        public GameObject TargetObject = null;
        public int RootIndex = 0;
        public int EffectorIndex = 0;
        public int[] JointIndices;

        public IKChain(int rootIndex, int effectorIndex)
        {
            RootIndex = rootIndex;
            EffectorIndex = effectorIndex;
        }
    }


    [CustomEditor(typeof(IKSolver))]
    public class IKSolverEditor : Editor
    {
        public IKSolver Target;

        private string[] SolverNames = { "FABRIK" , "CCD IK" };
        private string[] BoneNames = new string[0];

        private void Awake()
        {
            Target = target as IKSolver;
        }

        public override void OnInspectorGUI()
        {
            Target.SolverType = EditorGUILayout.Popup("IK Solver", Target.SolverType, SolverNames);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            Target.MyActor = EditorGUILayout.ObjectField("Actor:", Target.MyActor, typeof(Actor), true) as Actor;
            if(EditorGUI.EndChangeCheck())
            {
                Target.Chains = new IKChain[1];
                Target.Chains[0] = new IKChain(0, Target.MyActor.Bones.Length - 1);
            }

            if (GUILayout.Button("New Chain", GUILayout.Width(80.0f)))
                ArrayExtensions.Add(ref Target.Chains, new IKChain(0, Target.MyActor.Bones.Length - 1));
            EditorGUILayout.EndHorizontal();

            if (Target.MyActor != null && Target.Chains.Length > 0)
            {
                BoneNames = Target.MyActor.GetBoneNames();
                for (int i = 0; i < Target.Chains.Length; i++)
                {
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("IK Chain " + (i + 1));
                        if (GUILayout.Button("Delete", GUILayout.Width(80.0f)))
                        {
                            ArrayExtensions.RemoveAt(ref Target.Chains, i);
                            continue;
                        }
                        EditorGUILayout.EndHorizontal();

                        Target.Chains[i].RootIndex = EditorGUILayout.Popup("Root Joint", Target.Chains[i].RootIndex, BoneNames);
                        Target.Chains[i].EffectorIndex = EditorGUILayout.Popup("Effector Joint:", Target.Chains[i].EffectorIndex, BoneNames);
                        Target.Chains[i].TargetObject = EditorGUILayout.ObjectField("Target:", Target.Chains[i].TargetObject, typeof(GameObject), true) as GameObject;
                    }
                }
            }

            Target.MaxItr = EditorGUILayout.IntField("Iteration Time:", Target.MaxItr);
            Target.ErrThrehold = EditorGUILayout.FloatField("Accuracy:", Target.ErrThrehold);

            EditorUtility.SetDirty(target);
        }
    }
}
