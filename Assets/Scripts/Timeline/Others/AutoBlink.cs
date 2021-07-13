using UnityEngine;
using System.Collections;

public class AutoBlink : MonoBehaviour
{
    public bool isActive = true;                
    public SkinnedMeshRenderer ref_Face;
    public float ratio_Close = 85.0f; 
    public float ratio_HalfClose = 20.0f; 

    [HideInInspector]
    public float ratio_Open = 0.0f;
    private bool timerStarted = false;
    private bool isBlink = false;

    [SerializeField, TooltipAttribute("BlinkTime")]
    public float timeBlink = 0.4f;
    private float timeRemining = 0.0f;
    public float threshold = 0.3f;
    public float interval = 3.0f;

    private int blendShapeID = 8;
    enum Status
    {
        Close,
        HalfClose,
        Open
    }

    private Status eyeStatus;

    void Start()
    {
        ResetTimer();
        StartCoroutine("RandomChange");
    }

    void ResetTimer()
    {
        timeRemining = timeBlink;
        timerStarted = false;
    }

    void Update()
    {
        if (!timerStarted)
        {
            eyeStatus = Status.Close;
            timerStarted = true;
        }
        if (timerStarted)
        {
            timeRemining -= Time.deltaTime;
            if (timeRemining <= 0.0f)
            {
                eyeStatus = Status.Open;
                ResetTimer();
            }
            else if (timeRemining <= timeBlink * 0.3f)
            {
                eyeStatus = Status.HalfClose;
            }
        }
    }

    void LateUpdate()
    {
        if (isActive)
        {
            if (isBlink)
            {
                switch (eyeStatus)
                {
                    case Status.Close:
                        SetCloseEyes(ratio_Close);
                        break;
                    case Status.HalfClose:
                        SetCloseEyes(ratio_HalfClose);
                        break;
                    case Status.Open:
                        SetOpenEyes();
                        isBlink = false;
                        break;
                }
            }
        }
    }

    void SetCloseEyes(float ratio_Close)
    {
        ref_Face.SetBlendShapeWeight(blendShapeID, ratio_Close);
    }


    void SetOpenEyes()
    {
        ref_Face.SetBlendShapeWeight(blendShapeID, ratio_Open);
    }

    IEnumerator RandomChange()
    {
        while (true)
        {
            float _seed = Random.Range(0.0f, 1.0f);
            if (!isBlink)
            {
                if (_seed > threshold)
                {
                    isBlink = true;
                }
            }
            yield return new WaitForSeconds(interval);
        }
    }
}
