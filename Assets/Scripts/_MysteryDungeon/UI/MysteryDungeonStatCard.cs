using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MysteryDungeonStatCard : MonoBehaviour
{
    public GameObject thisObject;
    public void DisableStatCard(){thisObject.SetActive(false);}
    public SpriteContainer characterSprites;
    public Image characterImage;
    public RectTransform healthBar;
    public void UpdateStatCard(TacticActor actor)
    {
        thisObject.SetActive(true);
        characterSprites.ApplyToImage(characterImage, actor.GetSpriteName());
        UpdateHealthBar(actor.GetHealth(), actor.GetBaseHealth());
    }
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        float percent = Mathf.Clamp01((float)currentHealth / maxHealth);
        Vector3 scale = healthBar.localScale;
        scale.x = percent;
        healthBar.localScale = scale;
    }
}