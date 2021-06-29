using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class CameraBehaviour : PlayableBehaviour
{
    public enum Type { Translate, Rotate, Follow, LookAt, Length };
    public string[] AxisName = { "XPositive", "YPositive", "ZPositive" };
    public int TypeIndex = 0;
    public int AxisIndex = 0;
    public GameObject TargetObject = null;
    public float Speed = 10.0f;
    public float Degree = 15.0f;


    public string Name = string.Empty;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        Camera camera = playerData as Camera;
        if (Application.isPlaying && camera)
        {
            switch((Type)TypeIndex)
            {
                case Type.Translate:
                    camera.transform.position += GetAxis() * Speed / 10000.0f;
                    break;

                case Type.Rotate:
                    if(TargetObject != null)
                        camera.transform.RotateAround(TargetObject.transform.position, GetAxis(), Degree * Time.deltaTime);
                    break;

                case Type.Follow:
                    Vector3 offset = new Vector3(0.0f, 0.0f, 10.0f);
                    if (TargetObject != null)
                        camera.transform.position = TargetObject.transform.position + offset;
                    break;

                case Type.LookAt:
                    if (TargetObject != null)
                        camera.transform.LookAt(TargetObject.transform);
                    break;
            }

        }
    }

    public void Reset()
    {
        AxisIndex = 0;
        TargetObject = null;
        Speed = 10.0f;
        Degree = 15.0f;
    }

    public string[] GetNames()
    {
        string[] Names = new string[(int)Type.Length + 1];
        for (int i = 0; i < Names.Length - 1; i++)
            Names[i] = ((Type)i).ToString();

        return Names;
    }

    public Vector3 GetAxis()
    {
        Vector3 value = Vector3.zero;

        switch(AxisIndex)
        {
            case 0:
                value = Vector3.right;
                break;
            case 1:
                value = Vector3.up;
                break;
            case 2:
                value = Vector3.forward;
                break;
        }

        return value;
    }
}
