using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using TMPro;

public class ButtonControl : MonoBehaviour
{
    public PlayableDirector Director;
    public Button MyButton;
    public TMP_InputField InputBox;

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

        InputBox.interactable = false;
        MyButton.gameObject.SetActive(false);
    }

    private void Initial()
    {
        Button button = MyButton.GetComponent<Button>();
    }
}
