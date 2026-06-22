using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoChessPrepUIManager : MonoBehaviour
{
    public AutoChessDataManager dataManager;
    public AutoChessFactionDisplay factionDisplay;
    public List<AutoChessBenchSlot> benchSlots;
    public List<MapTile> mapSlots;
    public AutoActorDisplay actorDisplay;
    public void ActivateSellObject()
    {
        actorDisplay.ActivateSellObject();
    }
    public void ResetActorDisplay()
    {
        actorDisplay.ResetDisplay();
    }
    public void UpdateActorDisplay(AutoActorRollUpData actor)
    {
        actorDisplay.DisplayActor(actor.GetName());
    }
    public void UpdateActorDisplayByName(string newName)
    {
        actorDisplay.DisplayActor(newName);
    }
    public TMP_Text levelText;
    public TMP_Text goldText;
    public TMP_Text castleHealthText;
    public TMP_Text deployLimitText;
    public void UpdateUI(AutoChessPrepManager prepManager)
    {
        ResetActorDisplay();
        for (int i = 0; i < benchSlots.Count; i++)
        {
            benchSlots[i].ResetDisplay();
        }
        for (int i = 0; i < prepManager.benchSlots.Count; i++)
        {
            int benchIndex = prepManager.benchSlots[i].GetLocation();
            benchSlots[benchIndex].UpdateBenchSlot(prepManager.benchSlots[i].GetName(), prepManager.benchSlots[i].GetLevel());
        }
        if (dataManager.MaxLevel())
        {
            levelText.text = "MAX";
        }
        else
        {
            levelText.text = dataManager.GetLevel().ToString() + "\n" + dataManager.GetExp() + "/" + dataManager.ExpToLevelUp();
        }
        goldText.text = dataManager.GetGold().ToString();
        castleHealthText.text = dataManager.GetHealth().ToString();
        deployLimitText.text = prepManager.fieldSlots.Count + "/" + (2 + dataManager.GetLevel()).ToString();
    }
}
