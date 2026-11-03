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
    public GameObject burnTrackerObject;
    public GameObject bleedTrackerObject;
    public GameObject poisonTrackerObject;
    public RectTransform healthBar;
    public RectTransform energyBar;
    public RectTransform hungerBar;
    public void UpdateStatCard(TacticActor actor)
    {
        thisObject.SetActive(true);
        characterSprites.ApplyToImage(characterImage, actor.GetSpriteName());
        UpdateHealthBar(actor.GetHealth(), actor.GetBaseHealth());
        UpdateEnergyBar(actor.GetEnergy(), actor.GetBaseEnergy());
        UpdateHungerBar(actor.GetFullness(), actor.GetMaxFullness());
        burnTrackerObject.SetActive(actor.StatusExists("Burn"));
        bleedTrackerObject.SetActive(actor.StatusExists("Bleed"));
        poisonTrackerObject.SetActive(actor.StatusExists("Poison"));
    }
    protected void UpdateBar(RectTransform bar, int current, int max)
    {
        // Avoid Divide By 0.
        float percent = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
        Vector3 scale = bar.localScale;
        scale.x = percent;
        bar.localScale = scale;
    }
    public void UpdateHealthBar(int current, int max)
    {
        UpdateBar(healthBar, current, max);
    }
    public void UpdateEnergyBar(int current, int max)
    {
        UpdateBar(energyBar, current, max);
    }
    public void UpdateHungerBar(int current, int max)
    {
        UpdateBar(hungerBar, current, max);
    }
}