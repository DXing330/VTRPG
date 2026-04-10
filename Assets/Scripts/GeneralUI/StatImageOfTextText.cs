using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatImageOfTextText : StatTextText
{
    public Color emptyColor;
    public Color defaultColor;
    public Image image;
    public SpriteContainer spriteContainer;

    public override void Reset()
    {
        text.text = "";
        image.sprite = null;
        image.color = emptyColor;
    }

    public override void SetStatText(string statName)
    {
        image.sprite = spriteContainer.SpriteDictionary(statName);
        if (image.sprite == null){Reset();}
        else {image.color = defaultColor;}
    }

    public override void SetColor(Color newColor)
    {
        text.color = newColor;
    }
}
