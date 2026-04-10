using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Based on Yugioh/Langrisser?
public class ActorStatCard : BattleUIBaseClass
{
    public GameObject cardObject;
    public GeneralUtility utility;    
    public TacticActor cardActor;
    public override void SetActor(TacticActor newActor)
    {
        cardActor = newActor;
    }
    public SpriteContainer characterSprites;
    public SpriteContainer actorStatIcons;
    // Health bar on top.
    // Later add a temp health bar.
    public GameObject mainHPBar;
    public GameObject healthBar;
    protected void ResetHealth()
    {
        mainHPBar.SetActive(false);
    }
    protected void UpdateHealth()
    {
        mainHPBar.SetActive(true);
        float hpP = (float) cardActor.GetHealth() / (float) cardActor.GetBaseHealth();
        healthBar.transform.localScale = new Vector3(hpP, 1f, 0f);
    }
    // Basic status sprites and status counts.
    public List<string> basicStatusNames;
    public List<GameObject> statusImageObjects;
    public List<Image> statusImages;
    public List<TMP_Text> statusStacks;
    protected void ResetStatuses()
    {
        utility.DisableGameObjects(statusImageObjects);
    }
    protected void UpdateStatuses()
    {
        ResetStatuses();
        int index = 0;
        for (int i = 0; i < basicStatusNames.Count; i++)
        {
            int stacks = Mathf.Min(9, cardActor.StatusStacks(basicStatusNames[i]));
            if (stacks > 0)
            {
                statusStacks[index].text = "x" + stacks;
                statusImageObjects[index].SetActive(true);
                statusImages[index].sprite = actorStatIcons.SpriteDictionary(basicStatusNames[i]);
                index++;
            }
        }
    }
    // Sprite in the middle.
    public GameObject actorSpriteObject;
    public Image actorSprite;
    protected Color defaultActorSpriteColor = Color.white;
    protected Vector3 defaultActorSpriteScale = Vector3.one;
    protected bool actorSpriteAppearanceInitialized = false;
    protected void InitializeActorSpriteAppearance()
    {
        if (actorSpriteAppearanceInitialized || actorSprite == null){return;}
        defaultActorSpriteColor = actorSprite.color;
        defaultActorSpriteScale = actorSprite.rectTransform.localScale;
        actorSpriteAppearanceInitialized = true;
    }
    public void ResetActor()
    {
        actorSpriteObject.SetActive(false);
    }
    public void UpdateActor()
    {
        InitializeActorSpriteAppearance();
        actorSpriteObject.SetActive(true);
        actorSprite.sprite = characterSprites.SpriteDictionary(cardActor.GetSpriteName());
        actorSprite.color = characterSprites.GetColor(cardActor.GetSpriteName(), defaultActorSpriteColor);
        float scale = 1f;
        if (!float.TryParse(characterSprites.GetSize(cardActor.GetSpriteName()), out scale))
        {
            scale = 1f;
        }
        actorSprite.rectTransform.localScale = defaultActorSpriteScale * scale;
    }
    // Attack / Defense / Speed on bottom. Speed Icon Changes With Movement Type.
    public Image moveTypeImage;
    public List<GameObject> statObjects;
    public List<TMP_Text> baseStats;
    protected void ResetStats()
    {
        utility.DisableGameObjects(statObjects);
    }
    public void UpdateStats()
    {
        utility.EnableGameObjects(statObjects);
        moveTypeImage.sprite = actorStatIcons.SpriteDictionary(cardActor.GetMoveType());
        baseStats[0].text = cardActor.GetAttack().ToString();
        baseStats[1].text = cardActor.GetDefense().ToString();;
        baseStats[2].text = cardActor.GetSpeed().ToString();;
    }

    // Common UI functions.
    public override void ResetUI()
    {
        ResetHealth();
        ResetStatuses();
        ResetStats();
    }

    public override void UpdateUI()
    {
        if (cardActor == null || cardActor.GetHealth() <= 0)
        {
            ResetUI();
            return;
        }
        UpdateHealth();
        UpdateStatuses();
        UpdateActor();
        UpdateStats();
    }
}
