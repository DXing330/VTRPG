using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurnLogMini : BattleUIBaseClass
{
    public GeneralUtility utility;
    public SpriteContainer actorSprites;
    public List<GameObject> displayObjects;
    public List<TMP_Text> turnNumberTexts;
    public List<TMP_Text> teamNumberTexts;
    public List<LayeredImage> actorImages;
    public List<GameObject> displayTargetObjects;
    public List<LayeredImage> actorTargets;
    public int displayLayer = 1;
    public int currentTurn;
    public int actorCount;

    public override void ResetUI()
    {
        for (int i = 0; i < actorImages.Count; i++)
        {
            turnNumberTexts[i].text = "";
            teamNumberTexts[i].text = "";
            displayObjects[i].SetActive(false);
            actorImages[i].SetSprite(null, displayLayer);
            actorImages[i].BackgroundColor(displayLayer);
            displayTargetObjects[i].SetActive(false);
            actorTargets[i].SetSprite(null, displayLayer);
            actorTargets[i].BackgroundColor(displayLayer);
        }
    }
    public override void UpdateUI()
    {
        ResetUI();
    }
}
