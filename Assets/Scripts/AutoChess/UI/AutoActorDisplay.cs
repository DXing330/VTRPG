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
    public GeneralUtility utility;
    public StatDatabase actorData;
    public StatDatabase actorRarity;
    public ActiveDetailViewerSwitch activeViewer;
    public SpriteContainer factionSprites;
    public TMP_Text actorName;
    public TMP_Text traitText;
    public TMP_Text skillText;
    public List<GameObject> rarityObjects;
    public List<GameObject> factionIconObjects;
    public List<Image> factionIcons;
    public TMP_Text healthText;
    public TMP_Text attackText;
    public TMP_Text defenseText;
    public TMP_Text energyText;
    // TODO Add A Display For Attack Range + Shape
    public Image attackShapeImage;
    public TMP_Text attackRangeText;
    // TODO Add A Display For Respawn Timing.
    public TMP_Text respawnText;
    public GameObject sellObject;
    public void ActivateSellObject()
    {
        sellObject.SetActive(true);
    }
    public void ResetDisplay()
    {
        utility.DisableGameObjects(factionIconObjects);
        utility.DisableGameObjects(rarityObjects);
        actorName.text = "";
        traitText.text = "";
        skillText.text = "";
        healthText.text = "";
        attackText.text = "";
        defenseText.text = "";
        energyText.text = "";
        attackRangeText.text = "";
        respawnText.text = "";
        sellObject.SetActive(false);
    }
    public void DisplayActor(string newName)
    {
        ResetDisplay();
        actorName.text = newName;
        int rarity = utility.SafeParseInt(actorRarity.ReturnValue(newName), 1);
        for (int i = 0; i < rarity; i++)
        {
            rarityObjects[i].SetActive(true);
        }
        string[] blocks = actorData.ReturnValue(newName).Split("|");
        string[] allFactions = blocks[0].Split(",");
        for (int i = 0; i < allFactions.Length; i++)
        {
            factionIconObjects[i].SetActive(true);
            factionIcons[i].sprite = factionSprites.SpriteDictionary(allFactions[i]);
        }
        // TODO Display Trait Based On 1,2,3
        traitText.text = ReturnTraitDescription(blocks[1], blocks[2], blocks[3]);
        // TODO Display Skill Based On 4
        skillText.text = activeViewer.ReturnActiveDescriptionOnlyFromName(blocks[4]);
        energyText.text = blocks[5] + "(" + blocks[6] + ")";
        healthText.text = blocks[7];
        attackText.text = blocks[8];
        defenseText.text = blocks[9];
        attackRangeText.text = blocks[10];
        // TODO Update Attack Range Image Based On Attack Range Type.
        respawnText.text = blocks[13];
    }
    public void DisplayActor(AutoActorRollUpData actor)
    {
        DisplayActor(actor.GetName());
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
            case "StartBattle":
            return "At the start of each battle,";
            case "OnPurchase":
            return "When bought,";
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
            case "HighestActive":
            return " increase highest active faction stacks by " + amount + ".";
            case "Unit":
            case "Equipment":
            return " gain " + amount + ".";
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
        return specifics;
    }
}
