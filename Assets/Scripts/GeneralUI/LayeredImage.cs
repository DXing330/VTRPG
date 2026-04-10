using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LayeredImage : MonoBehaviour
{
    public List<Image> images;
    public Color basicColor;
    public Color highlightColor;
    public Color backgroundColor;

    public void SetSprite(Sprite newSprite, int layer = 0){images[layer].sprite = newSprite;}

    public void ResetHighlights()
    {
        for (int i = 0; i < images.Count; i++)
        {
            images[i].color = basicColor;
        }
    }

    public void Highlight(int layer = 0){images[layer].color = highlightColor;}

    public void BackgroundColor(int layer = 0){images[layer].color = backgroundColor;}

    public void DefaultColor(int layer = 0){images[layer].color = basicColor;}
}
