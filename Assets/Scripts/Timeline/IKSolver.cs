using UnityEngine;
using UnityEditor;

public class IKSolver : MonoBehaviour
{
    public Actor MyActor;
    public GameObject TargetObject;
    public int MaxItr = 20;
    public float ErrThrehold = 0.01f;
    public int RootIndex = 0;
    public int SolverType = 0;

    private void LateUpdate()
    {
        switch (SolverType)
        {
            case 0:
                FABRIKSolver(MyActor, TargetObject.transform.position, MaxItr, ErrThrehold);    
                break;
            case 1:
                CCDSolver(MyActor, TargetObject.transform.position, MaxItr, ErrThrehold);
                break;
        }
    }

    public void CCDSolver(Actor actor, Vector3 targetPos, int maxItr, float errThrehold)
    {
        Vector3 localEffectorPos = Vector3.zero;
        Vector3 localTargetPos = Vector3.zero;
        Actor.Bone root = actor.Bones[RootIndex];
        Actor.Bone effector = actor.Bones[actor.Bones.Length - 1];

        for (int itr = 0; itr < maxItr; itr++)
        {
            for (Actor.Bone joint = actor.Bones[effector.Parent]; joint != null; joint = actor.Bones[joint.Parent])
            {
                Vector3 effectorPos = effector.Transform.position;
                Matrix4x4 worldToLocal = joint.Transform.worldToLocalMatrix;

                localEffectorPos = worldToLocal.MultiplyPoint(effectorPos);
                localTargetPos = worldToLocal.MultiplyPoint(targetPos);

                Vector3 toEffectorNorm = Vector3.Normalize(localEffectorPos);
                Vector3 toTargetNorm = Vector3.Normalize(localTargetPos);

                float rotationRad = Vector3.Dot(toEffectorNorm, toTargetNorm);
                float rotationAngle = Mathf.Acos(Mathf.Clamp(rotationRad, -1.0f, 1.0f));
                if (rotationAngle > 1.0e-5f)
                {
                    Vector3 rotationAxis = Vector3.Cross(toEffectorNorm, toTargetNorm);
                    rotationAxis.Normalize();
                    joint.Transform.rotation = Quaternion.AngleAxis(rotationAngle, rotationAxis) * joint.Transform.rotation;
                }

                if (joint == root)
                    break;  
            }

            if (Vector3.Distance(localEffectorPos, localTargetPos) < errThrehold)
                return;
        }
    }

    public void FABRIKSolver(Actor actor, Vector3 targetPos, int maxItr, float errThrehold)
    {
        int length = actor.Bones.Length;
        Vector3 rootPos = actor.Bones[RootIndex].Transform.position;
        float[] dists = new float[length];
        float totalLength = 0.0f;

        // get bone length
        for (int i = 0; i < length; i++)
        {
            dists[i] = actor.Bones[i].Length;
            if(i >= RootIndex)
                totalLength += dists[i];
        }

        // unreachable
        if (Vector3.Distance(targetPos, rootPos) > totalLength)
        {
            for (int i = RootIndex; i < length - 1; i++)
            {
                float r = Vector3.Distance(targetPos, actor.Bones[i].Transform.position);
                float lambda = dists[i + 1] / r;
                actor.Bones[i + 1].Transform.position = (1.0f - lambda) * actor.Bones[i].Transform.position + lambda * targetPos;
            }
        }
        else // reachable
        {
            for (int itr = 0; itr < maxItr; itr++)
            {
                if (Vector3.Distance(actor.Bones[length - 1].Transform.position, targetPos) < errThrehold)
                    break;

                // forward reaching
                actor.Bones[length - 1].Transform.position = targetPos;
                for (int i = length - 2; i >= RootIndex; i--)
                {
                    float r = Vector3.Distance(actor.Bones[i + 1].Transform.position, actor.Bones[i].Transform.position);
                    float lambda = dists[i + 1] / r;
                    actor.Bones[i].Transform.position = (1.0f - lambda) * actor.Bones[i + 1].Transform.position
                        + lambda * actor.Bones[i].Transform.position;
                }

                // backward reaching
                actor.Bones[RootIndex].Transform.position = rootPos;
                for (int i = RootIndex; i < length - 1; i++)
                {
                    float r = Vector3.Distance(actor.Bones[i + 1].Transform.position, actor.Bones[i].Transform.position);
                    float lambda = dists[i + 1] / r;
                    actor.Bones[i + 1].Transform.position = (1.0f - lambda) * actor.Bones[i].Transform.position
                        + lambda * actor.Bones[i + 1].Transform.position;
                }
            }
        }

        // fix rotation
        if (length > 1)
        {
            for (int i = length - 2; i >= RootIndex; i--)
            {
                Vector3 forward = actor.Bones[i + 1].Transform.position - actor.Bones[i].Transform.position;
                float rotationAngle = Vector3.Angle(actor.Bones[i].Transform.up, forward);
                if (rotationAngle > 1.0e-5f)
                {
                    Vector3 rotationAxis = Vector3.Cross(actor.Bones[i].Transform.up, forward);
                    actor.Bones[i].Transform.RotateAround(actor.Bones[i].Transform.position, rotationAxis, rotationAngle);
                    actor.Bones[i + 1].Transform.RotateAround(actor.Bones[i].Transform.position, rotationAxis, -rotationAngle);
                }
            }
        }
    }

    //public void ParticleIK()
    //{

    //}

    [CustomEditor(typeof(IKSolver))]
    public class IKSolverEditor : Editor
    {
        public IKSolver Target;

        private string[] SolverNames = { "FABRIK" , "CCD IK" };
        private string[] BoneNames = new string[0];
        private Actor PreActor = null;

        private void Awake()
        {
            Target = target as IKSolver;
        }

        public override void OnInspectorGUI()
        {
            Target.SolverType = EditorGUILayout.Popup("IK Solver", Target.SolverType, SolverNames);
            Target.MyActor = EditorGUILayout.ObjectField("Actor:", Target.MyActor, typeof(Actor), true) as Actor;

            if (Target.MyActor != null)
            {
                if (PreActor == null || PreActor != Target.MyActor)
                {
                    PreActor = Target.MyActor;
                    BoneNames = Target.MyActor.GetBoneNames();
                }

                Target.RootIndex = EditorGUILayout.Popup("Root Joint", Target.RootIndex, BoneNames);
            }
  
            Target.TargetObject = EditorGUILayout.ObjectField("Target:", Target.TargetObject, typeof(GameObject), true) as GameObject;
            Target.MaxItr = EditorGUILayout.IntField("Iteration Time:", Target.MaxItr);
            Target.ErrThrehold = EditorGUILayout.FloatField("Accuracy:", Target.ErrThrehold);
        }
    }
}
