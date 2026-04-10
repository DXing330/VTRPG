using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatImageText : MonoBehaviour
{
    public TMP_Text text;
    public Image image;

    public void SetImage(Sprite newSprite){image.sprite = newSprite;}

    public void SetText(string newText){text.text = newText;}

    public void Reset()
    {
        text.text = "";
        image.sprite = null;
    }
}
