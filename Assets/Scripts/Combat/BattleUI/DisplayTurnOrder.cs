using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DisplayTurnOrder : MonoBehaviour
{
    public GeneralUtility utility;
    public SpriteContainer actorSprites;
    public List<GameObject> displayObjects;
    public List<TMP_Text> teamNumberTexts;
    public List<LayeredImage> actorImages;
    public List<GameObject> displayTargetObjects;
    public List<LayeredImage> actorTargets;
    public int displayLayer = 1;
    public int currentTurn;
    public int actorCount;

    [ContextMenu("Reset Actor Images")]
    public void ResetActorImages()
    {
        for (int i = 0; i < actorImages.Count; i++)
        {
            teamNumberTexts[i].text = "";
            displayObjects[i].SetActive(false);
            actorImages[i].SetSprite(null, displayLayer);
            actorImages[i].BackgroundColor(displayLayer);
            displayTargetObjects[i].SetActive(false);
            actorTargets[i].SetSprite(null, displayLayer);
            actorTargets[i].BackgroundColor(displayLayer);
        }
    }


    public void UpdateTurnOrder(List<TacticActor> actors, int turnIndex)
    {
        currentTurn = turnIndex;
        ResetActorImages();
        if (turnIndex < 0) { return; }
        int index = 0;
        for (int i = turnIndex; i < actors.Count; i++)
        {
            if (index >= actorImages.Count){break;}
            displayObjects[index].SetActive(true);
            actorImages[index].SetSprite(actorSprites.GetSprite(actors[i].GetSpriteName()), displayLayer);
            actorImages[index].DefaultColor(displayLayer);
            teamNumberTexts[index].text = (actors[i].GetTeam()).ToString();
            if (actors[i].GetTarget() == null || actors[i].GetTarget().GetHealth() <= 0)
            {
                index++;
                continue;
            }
            displayTargetObjects[index].SetActive(true);
            actorTargets[index].SetSprite(actorSprites.GetSprite(actors[i].GetTarget().GetSpriteName()), displayLayer);
            actorTargets[index].DefaultColor(displayLayer);
            index++;
        }
    }

    // Later enable scrolling up and down.
    public int currentIndex;
}
