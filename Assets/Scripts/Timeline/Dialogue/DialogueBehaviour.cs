using UnityEngine;
using TMPro;
using UnityEngine.Playables;

[System.Serializable]
public class DialogueBehaviour : PlayableBehaviour
{
    //public string CharacterName = string.Empty;
    public string Text = string.Empty;
    public bool hasPause = false; 

    private PlayableDirector Director = null;
    private TextMeshProUGUI Tmp = null;
    private bool isPause = false;


    public override void OnPlayableCreate(Playable playable)
    {
        Director = playable.GetGraph().GetResolver() as PlayableDirector;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        Tmp = playerData as TextMeshProUGUI;

        if(Tmp)
        {
            if(Application.isPlaying && hasPause)
            {
                isPause = true;
            }

            Tmp.SetText(Text);
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if(isPause)
        {
            Director.playableGraph.GetRootPlayable(0).SetSpeed(0d);
        }
    }
}
