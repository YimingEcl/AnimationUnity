using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtController : MonoBehaviour
{
    public float Radius = 2.0f;
    public float Speed = 1.2f;
    public float MaxAngle = 60.0f;
    public float NeckWeight = 0.2f;
    public float BodyWeight = 0.05f;

    private Vector3 Position = Vector3.zero;
    private Vector3 Forward = Vector3.zero;
    private Vector3 Up = Vector3.zero;

    private void Start()
    {
        Forward = gameObject.transform.forward;
        Up = gameObject.transform.up;
        Position = gameObject.transform.position;
    }

    private void Update()
    {
        GameObject target = DetectNearestObject();

        if (target != null)
        {
            Vector3 toward = target.transform.position - Position;
            float angle = Vector3.Angle(Forward, toward);
            if(Mathf.Abs(angle) <= MaxAngle)
            {
                float headAngle = Vector3.Angle(gameObject.transform.forward, toward) * (1.0f - NeckWeight - BodyWeight);
                Vector3 axis = Vector3.Cross(gameObject.transform.forward, toward);
                gameObject.transform.RotateAround(gameObject.transform.position, axis, Speed * headAngle * Time.deltaTime);
            }
            else
                RotateBack();
        }
        else
            RotateBack();
    }

    private void RotateBack()
    {
        float rotationAngle = Vector3.Angle(gameObject.transform.forward, Forward);
        Vector3 axis = Vector3.Cross(gameObject.transform.forward, Forward);
        Quaternion forwardRotation = Quaternion.AngleAxis(rotationAngle, axis);
        rotationAngle = Vector3.Angle(gameObject.transform.up, Up);
        axis = Vector3.Cross(gameObject.transform.up, Up);
        Quaternion upRotation = Quaternion.AngleAxis(rotationAngle, axis);
        Quaternion rotation = forwardRotation * upRotation;
        rotation.ToAngleAxis(out rotationAngle, out axis);
        gameObject.transform.RotateAround(gameObject.transform.position, axis, Speed * rotationAngle * Time.deltaTime);
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
                float d = Vector3.Distance(Position, collider.gameObject.transform.position);
                if (d < minDistance)
                {
                    target = collider.gameObject;
                    minDistance = d;
                }
            }
        }

        return target;
    }
}
