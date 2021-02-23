using UnityEngine;

public class EigenSample : MonoBehaviour
{
    void Start()
    {
        System.IntPtr ptr = Eigen.Create(2, 3);
        Debug.Log(Eigen.GetRows(ptr));
    }
}
