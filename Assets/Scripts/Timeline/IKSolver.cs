using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKSolver : MonoBehaviour
{
    public Actor MyActor;
    public GameObject Target;
    public int MaxItr = 50;
    public float ErrThrehold = 1.0e-5f;

    private void Awake()
    {
        MyActor = GetComponent<Actor>();
    }

    private void Start()
    {
        if (MyActor == null)
            MyActor = GetComponent<Actor>();

        CCDSolver(MyActor, Target.transform.position, MaxItr, ErrThrehold);
        //FABRIKSolver(MyActor, Target.transform.position, MaxItr, ErrThrehold);
    }

    private void Update()
    {
        CCDSolver(MyActor, Target.transform.position, MaxItr, ErrThrehold);
        //FABRIKSolver(MyActor, Target.transform.position, MaxItr, ErrThrehold);
    }

    public void CCDSolver(Actor actor, Vector3 targetPos, int maxItr, float errThrehold)
    {
        Vector3 localEffectorPos = Vector3.zero;
        Vector3 localTargetPos = Vector3.zero;
        Actor.Bone effector = actor.Bones[actor.Bones.Length - 1];

        for(int i = 0; i < maxItr; i++)
        {
            for(Actor.Bone joint = actor.Bones[effector.Parent]; joint.Parent != -1 && joint != null;)
            {
                Vector3 effectorPos = effector.Transform.position;
                Matrix4x4 worldToLocal = joint.Transform.worldToLocalMatrix;

                localEffectorPos = worldToLocal.MultiplyPoint(effectorPos);
                localTargetPos = worldToLocal.MultiplyPoint(targetPos);

                Vector3 toEffectorNorm = Vector3.Normalize(localEffectorPos);
                Vector3 toTargetNorm = Vector3.Normalize(localTargetPos);

                float rotationAngle = Mathf.Acos(Vector3.Dot(toEffectorNorm, toTargetNorm));
                if(rotationAngle > 1.0e-5f)
                {
                    Vector3 rotationAxis = Vector3.Cross(toEffectorNorm, toTargetNorm);
                    rotationAxis.Normalize();
                    joint.Transform.rotation = Quaternion.AngleAxis(rotationAngle, rotationAxis) * joint.Transform.rotation;
                }

                joint = actor.Bones[joint.Parent];
            }

            if ((localEffectorPos - localTargetPos).magnitude < errThrehold)
                return;
        }
    }

    public void FABRIKSolver(Actor actor, Vector3 targetPos, int maxItr, float errThrehold)
    {
        Vector3 rootPos = actor.Bones[0].Transform.position;      

        for(int i = 0; i < maxItr; i++)
        {
            // backward
            actor.Bones[actor.Bones.Length - 1].Transform.position = targetPos;
            for (int index = actor.Bones.Length - 2; index > -1; index--)
            {
                Actor.Bone backBone = actor.Bones[index + 1];
                Actor.Bone frontBone = actor.Bones[index];

                Vector3 backwardNorm = Vector3.Normalize(frontBone.Transform.position - backBone.Transform.position);
                actor.Bones[index].Transform.position = backBone.Transform.position + backwardNorm * backBone.Length;
            }

            // forward
            actor.Bones[0].Transform.position = rootPos;
            for(int index = 1; index < actor.Bones.Length; index++)
            {
                Actor.Bone frontBone = actor.Bones[index - 1];
                Actor.Bone backBone = actor.Bones[index];

                Vector3 forwardNorm = Vector3.Normalize(backBone.Transform.position - frontBone.Transform.position);
                actor.Bones[index].Transform.position = frontBone.Transform.position + forwardNorm * backBone.Length;
            }

            if ((actor.Bones[actor.Bones.Length - 1].Transform.position - targetPos).magnitude < errThrehold)
                return;
        }
    }
}
