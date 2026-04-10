using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultiImage : MonoBehaviour
{
    public Image image;
    public List<Sprite> sprites;
    public void SetSpriteByIndex(int index)
    {
        image.sprite = sprites[index];
    }
    public void SetSpriteByName(string newName)
    {
        int index = 0;
        for (int i = 0; i < sprites.Count; i++)
        {
            if (sprites[i].name == newName)
            {
                index = i;
            }
        }
        SetSpriteByIndex(index);
    }
    public void IncrementSprite()
    {
        int currentIndex = sprites.IndexOf(image.sprite);
        currentIndex = (currentIndex + 1) % sprites.Count;
        SetSpriteByIndex(currentIndex);
    }
}
