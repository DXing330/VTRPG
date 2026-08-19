using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentDetailViewerSwitch", menuName = "ScriptableObjects/UIData/EquipmentDetailViewerSwitch", order = 1)]
public class EquipmentDetailViewerSwitch : ScriptableObject
{
    public PassiveDetailViewerSwitch passiveDetails;
    public StatDatabase autoChessEquipmentData;
    public string ReturnAutoChessEquipmentDescription(string autoChessEquipmentName)
    {
        // TODO Add Descriptions For Custon Effects, Ie Theives Gloves, Tianshi Cauldron, Etc.
        switch (autoChessEquipmentName)
        {
            case "Commercial Packaging Plan":
            return "Gain a level 1 copy of the equipped unit whenever 8 units are sold each round.";
            case "Steam Heart":
            return "Gain the effects of all hammers on the field.";
            case "Thief's Gloves":
            return "Gain up to 2 random equipment at the start of each battle.";
        }
        string[] blocks = autoChessEquipmentData.ReturnValue(autoChessEquipmentName).Split("|");
        if (blocks.Length < 4){return "";}
        string[] effects = blocks[2].Split(",");
        string[] specifics = blocks[3].Split(",");
        string description = "";
        for (int i = 0; i < effects.Length; i++)
        {
            description += ReturnEffectSpecificsDescriptions(effects[i], specifics[i]);
            if (i < effects.Length - 1)
            {
                description += "\n";
            }
        }
        return description;
    }
    protected string ReturnEffectSpecificsDescriptions(string effect, string specifics)
    {
        switch (effect)
        {
            default:
            return "+" + specifics + " " + effect;
            case "PassiveAtLevel":
            string[] passiveAtLevel = specifics.Split("Equals");
            if (passiveAtLevel.Length < 2){return "";}
            return passiveDetails.ReturnPassiveAtLevelDetails(passiveAtLevel[0], int.Parse(passiveAtLevel[1]));
            case "Passive":
            return passiveDetails.ReturnPassiveDetailsFromName(specifics);
        }
    }
}
