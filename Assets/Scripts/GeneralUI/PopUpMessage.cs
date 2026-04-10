using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PopUpMessage : MonoBehaviour
{
    public GameObject thisObject;
    public TMP_Text message;
    public void ResetMessage()
    {
        thisObject.SetActive(false);
        message.text = "";
    }
    public void SetMessage(string newInfo)
    {
        thisObject.SetActive(true);
        message.text = newInfo;
    }
}
