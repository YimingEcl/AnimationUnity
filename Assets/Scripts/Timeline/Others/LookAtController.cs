using UnityEngine;

public class LookAtController : MonoBehaviour
{
    public Transform Head = null;
    public Transform LeftEye = null;
    public Transform RightEye = null;
    public float Radius = 2.0f;
    public float Speed = 1.5f;
    public float MaxHeadAngle = 60.0f;
    public float MaxEyeAngle = 10.0f;
    public float NeckWeight = 0.2f;
    public float BodyWeight = 0.1f;

    private Vector3 Position = Vector3.zero;
    private Vector3 Forward = Vector3.forward;
    private Vector3 Up = Vector3.up;
    private Vector3 Right = Vector3.right;

    private void Start()
    {
        if(Head == null)
            Head = gameObject.transform;

        Position = Head.position;
        Forward = Head.forward;
        Up = Head.up;
        Right = Head.right;

        if(LeftEye != null && RightEye != null)
            Position = (LeftEye.position + RightEye.position) / 2.0f;
    }

    private void LateUpdate()
    {
        GameObject target = DetectNearestObject();

        if (target != null)
        {
            Vector3 targetForward = Vector3.Normalize(target.transform.position - Position);
            Vector3 targetUp = Vector3.Normalize(Vector3.Cross(targetForward, Right));
            if (Vector3.Dot(targetUp, Vector3.up) < 0)
                targetUp = -1.0f * targetUp;
            float angle = Vector3.Angle(Forward, targetForward);

            float forwardAngle = Vector3.Angle(Head.forward, targetForward);
            float upAngle = Vector3.Angle(Head.up, targetUp);
            Vector3 forwardAxis = Vector3.Normalize(Vector3.Cross(Head.forward, targetForward));
            Vector3 upAxis = Vector3.Normalize(Vector3.Cross(Head.up, targetUp));

            if (Mathf.Abs(angle) <= MaxEyeAngle && LeftEye != null && RightEye != null)
            {
                RotateHeadBack();

                RotateJoint(LeftEye, forwardAngle, forwardAxis, upAngle, upAxis, 1.0f, 2.0f * Speed);
                RotateJoint(RightEye, forwardAngle, forwardAxis, upAngle, upAxis, 1.0f, 2.0f * Speed);
            }
            else if (Mathf.Abs(angle) <= MaxHeadAngle)
            {
                LeftEye.localRotation = Quaternion.Slerp(LeftEye.localRotation, Quaternion.identity, 2.0f * Time.deltaTime);
                RightEye.localRotation = Quaternion.Slerp(RightEye.localRotation, Quaternion.identity, 2.0f * Time.deltaTime);

                // head joint
                RotateJoint(Head, forwardAngle, forwardAxis, upAngle, upAxis, 1.0f - NeckWeight - BodyWeight, Speed);

                // neck joint
                Transform neck = Head.parent;
                RotateJoint(neck, forwardAngle, forwardAxis, upAngle, upAxis, NeckWeight, Speed);

                // spine joint
                Transform spine = neck.parent;
                RotateJoint(spine, forwardAngle, forwardAxis, upAngle, upAxis, BodyWeight, Speed);
            }
            else
            {
                RotateHeadBack();
                RotateEyeBack();
            }
        }
        else
        {
            RotateHeadBack();
            RotateEyeBack();
        }
    }

    private void RotateJoint(Transform joint, float forwardAngle, Vector3 forwardAxis, float upAngle, Vector3 upAxis, float weight, float speed)
    {
        float jointForwardAngle = forwardAngle * weight;
        Quaternion forwardRotation = Quaternion.AngleAxis(jointForwardAngle, Quaternion.Inverse(joint.rotation) * forwardAxis);

        float jointUpAngle = upAngle * weight;
        Quaternion upRotation = Quaternion.AngleAxis(jointUpAngle, Quaternion.Inverse(joint.rotation) * upAxis);

        Quaternion rotation = joint.localRotation * forwardRotation * upRotation;
        joint.localRotation = Quaternion.Slerp(joint.localRotation, rotation, speed * Time.deltaTime);
    }

    private void RotateJointBack(Transform joint)
    {
        float angle = Vector3.Angle(joint.forward, Forward);
        Vector3 axis = Vector3.Cross(joint.forward, Forward);
        Quaternion forwardRotation = Quaternion.AngleAxis(angle, Quaternion.Inverse(joint.rotation) * axis);

        angle = Vector3.Angle(joint.up, Up);
        axis = Vector3.Cross(joint.up, Up);
        Quaternion upRotation = Quaternion.AngleAxis(angle, Quaternion.Inverse(joint.rotation) * axis);

        Quaternion rotation = joint.localRotation * forwardRotation * upRotation;
        joint.localRotation = Quaternion.Slerp(joint.localRotation, rotation, 0.5f * Speed * Time.deltaTime);
    }

    private void RotateHeadBack()
    {
        RotateJointBack(Head);

        Transform neck = Head.parent;
        RotateJointBack(neck);

        Transform spine = neck.parent;
        RotateJointBack(spine);
    }

    private void RotateEyeBack()
    {
        LeftEye.localRotation = Quaternion.Slerp(LeftEye.localRotation, Quaternion.identity, 3.0f * Speed * Time.deltaTime);
        RightEye.localRotation = Quaternion.Slerp(RightEye.localRotation, Quaternion.identity, 3.0f * Speed * Time.deltaTime);
    }

    private GameObject DetectNearestObject()
    {
        GameObject target = null;
        float minDistance = float.MaxValue;

        Collider[] colliders = Physics.OverlapSphere(Position, Radius);
        if(colliders.Length > 0)
        {
            foreach (Collider collider in colliders)
            {
                Vector3 position = collider.gameObject.transform.position;
                float distance = Vector3.Distance(Position, position);
                float angle = Vector3.Angle(Forward, position - Position);
                if (distance < minDistance && Mathf.Abs(angle) <= MaxHeadAngle)
                {
                    target = collider.gameObject;
                    minDistance = distance;
                }
            }
        }

        return target;
    }
}
