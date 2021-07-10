using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InputController : MonoBehaviour
{
    private void OnEnable()
    {
        TMP_InputField inputField = GetComponent<TMP_InputField>();
        inputField.text = string.Empty;
        inputField.interactable = true;
        transform.Find("Button").gameObject.SetActive(true);
    }
}
