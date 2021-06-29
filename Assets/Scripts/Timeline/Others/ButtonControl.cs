using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;

public class ButtonControl : MonoBehaviour
{
    public PlayableDirector Director;
    public Button MyButton;

    private void Start()
    {
        Button button = MyButton.GetComponent<Button>();
        button.onClick.AddListener(Click);
    }

    public void Click()
    {
        if(Director != null)
        {
            Director.playableGraph.GetRootPlayable(0).SetSpeed(1);
        }
    }
}
