using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ButtonText : MonoBehaviour
{
    public TMP_Text buttonText;
    public void UpdateText(string newText)
    {
        buttonText.text = newText;
    }
}
