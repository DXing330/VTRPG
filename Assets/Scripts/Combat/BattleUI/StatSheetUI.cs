using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatSheetUI : MonoBehaviour
{
    public SpriteContainer characterSprites;
    public Image characterImage;
    public void UpdateCharacterSprite(string spriteName)
    {
        characterImage.sprite = characterSprites.SpriteByKey(spriteName);
    }
    public List<TMP_Text> characterStats;

    public void UpdateStats(List<string> newStats)
    {
        for (int i = 0; i < Mathf.Min(characterStats.Count, newStats.Count); i++)
        {
            characterStats[i].text = newStats[i];
        }
    }

    protected void UpdateHealthBar(int currentHealth, int maxHealth)
    {

    }
}
