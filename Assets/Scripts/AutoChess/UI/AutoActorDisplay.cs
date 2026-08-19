using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoActorDisplay : MonoBehaviour
{
    public GameObject thisObject;
    public void DisableDisplay()
    {
        thisObject.SetActive(false);
    }
    public GeneralUtility utility;
    public StatDatabase actorData;
    public StatDatabase actorRarity;
    public ActiveDetailViewerSwitch activeViewer;
    public List<GameObject> displayPages;
    public List<TMP_Text> displayPageStrings;
    public int currentPage;
    public void SetPage(int newPage)
    {
        currentPage = newPage;
        utility.DisableGameObjects(displayPages);
        displayPages[currentPage].SetActive(true);
        for (int i = 0; i < displayPageStrings.Count; i++)
        {
            displayPageStrings[i].fontStyle = FontStyles.Normal;
            if (i == currentPage)
            {
                displayPageStrings[i].fontStyle = FontStyles.Underline;
            }
        }
    }
    public SpriteContainer masterSprites;
    public TMP_Text actorName;
    public TMP_Text HPText;
    public TMP_Text ATKText;
    public TMP_Text DEFText;
    public TMP_Text traitText;
    public TMP_Text skillText;
    public List<GameObject> factionIconObjects;
    public List<Image> factionIcons;
    // Later upgrade this to icons?
    public List<GameObject> currentEquipImageObjects;
    public List<Image> currentEquipImages;
    public void ResetDisplay()
    {
        SetPage(0);
        utility.DisableGameObjects(factionIconObjects);
        actorName.text = "";
        traitText.text = "";
        skillText.text = "";
        HPText.text = "";
        ATKText.text = "";
        DEFText.text = "";
        utility.DisableGameObjects(currentEquipImageObjects);
    }
    public void DisplayActor(string newName)
    {
        ResetDisplay();
        actorName.text = newName;
        string[] blocks = actorData.ReturnValue(newName).Split("|");
        string[] allFactions = blocks[0].Split(",");
        for (int i = 0; i < allFactions.Length; i++)
        {
            factionIconObjects[i].SetActive(true);
            factionIcons[i].sprite = masterSprites.SpriteDictionary(allFactions[i]);
        }
        traitText.text = ReturnTraitDescription(blocks[1], blocks[2], blocks[3]);
        skillText.text = activeViewer.ReturnActiveDescriptionOnlyFromName(blocks[4]) + "\n" + "CD: " + blocks[5];
        HPText.text = blocks[7];
        ATKText.text = blocks[8];
        DEFText.text = blocks[9];
    }
    public void DisplayActor(AutoActorRollUpData actor)
    {
        DisplayActor(actor.GetName());
        List<string> equipNames = actor.GetEquipmentNames();
        for (int i = 0; i < Mathf.Min(currentEquipImages.Count, equipNames.Count); i++)
        {
            currentEquipImageObjects[i].SetActive(true);
            masterSprites.ApplyToImage(currentEquipImages[i], equipNames[i]);
        }
        HPText.text = actor.GetHealth().ToString();
        ATKText.text = actor.GetAttack().ToString();
        DEFText.text = actor.GetDefense().ToString();
    }
    // TRAIT DETAILS, Maybe Move This To A Utility Later.
    public string ReturnTraitDescription(string timing, string effect, string specifics)
    {
        string description = "";
        description += ReturnTraitTiming(timing);
        description += ReturnTraitEffect(effect, specifics);
        return description;
    }
    public string ReturnTraitTiming(string timing)
    {
        switch (timing)
        {
            default:
            return "";
            case "DuringBattle":
            return "During battle,";
            case "StartBattle":
            return "At the start of each battle,";
            case "OnPurchase":
            return "When obtained,";
            case "OnAttack":
            return "When attacking,";
            case "FirstSkill":
            return "When using a skill for the first time each battle,";
            case "OnSkill":
            return "When using a skill,";
            case "OnKill":
            return "When defeating an enemy,";
            case "OnDeath":
            return "When defeated,";
            case "OnSold":
            return "When sold,";
            case "OnForwardSkill":
            return "When the ally in front uses a skill,";
            case "OnForwardAttack":
            return "When the ally in front attacks,";
        }
    }
    public string ReturnTraitEffect(string effect, string specifics)
    {
        string amount = ReturnTraitSpecifics(specifics);
        if (specifics.Contains("Scaling"))
        {
            return amount;
        }
        switch (effect)
        {
            default:
            return "";
            case "Self":
            return " increase own faction stacks by " + amount + ".";
            case "SelfActive":
            return " increase own active faction stacks by " + amount + ".";
            case "Gold":
            return " gain " + amount + " gold.";
            case "NextRoundGold":
            return " gain " + amount + " gold, next round.";
            case "RandomActive":
            return " increase a random active faction stacks by " + amount + ".";
            case "HighestActive":
            return " increase highest active faction stacks by " + amount + ".";
            case "Unit":
            case "Equipment":
            return " gain " + amount + " equipment.";
            case "AegirUnit":
            return " gain " + amount + " Skadi/Specter/Andreana.";
            case "HighestActiveUnit":
            return " gain " + amount + " unit from from the active faction with the highest stacks.";
            case "SelfAndBackActive":
            return " increase active faction stacks by " + amount + " for this unit and the ally behind.";
            case "SelfAndFrontActive":
            return " increase active faction stacks by " + amount + " for this unit and the ally in front.";
            case "FrontActive":
            return " increase active faction stacks by " + amount + " for the ally in front.";
            case "CopyFront":
            return " copy the [" + amount + "] trait of the ally in front, if possible.";
            case "CopyBack":
            return " copy the [" + amount + "] trait of the ally behind, if possible.";
            case "SelfAndFrontLineActive":
            return " increase active faction stacks by " + amount + " for this unit and all allies in front.";
            case "AllActiveBench":
            return " increase active faction stacks by " + amount + " all units on the bench that are apart of active factions.";
        }
    }
    public string ReturnTraitSpecifics(string specifics)
    {
        if (specifics.Contains("MultiBy"))
        {
            string[] blocks = specifics.Split("MultiBy");
            string details = "";
            switch (blocks[0])
            {
                default:
                details = " times " + blocks[0];
                break;
                case "UnitsBought":
                details = " times the number of units bought this round";
                break;
                case "GoldSpent":
                details = " times the amount of gold spent this round";
                break;
                case "BenchSize":
                details = " times the number of units on the bench";
                break;
                case "SelfActiveUnits":
                details = " times the number of units on the field that are part of the same faction";
                break;
            }
            return blocks[1] + details;
        }
        if (specifics.Contains("Scaling"))
        {
            string[] blocks = specifics.Split("Scaling");
            return $" increase {blocks[0]} by 1% for every {blocks[1]} faction stacks.";
        }
        return specifics;
    }
}
