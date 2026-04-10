using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// For Displaying Skillbook Rewards
public class SkillDisplay : MonoBehaviour
{
    public ActiveDescriptionViewer descriptionViewer;
    public SpellDetailViewer spellViewer;
    public PassiveDetailViewer passiveViewer;
    public int skillRarity;
    public Image borderImage;
    public ColorDictionary borderColors;
    public TMP_Text skillName;
    public TMP_Text typeName;
    public TMP_Text skillDescription;

    public void SetSkill(string newName, string type = "Skill", int rarity = 1)
    {
        skillName.text = newName;
        typeName.text = "[" + type + "]";
        skillRarity = rarity;
        borderImage.color = borderColors.GetColorByIndex(skillRarity);
        switch (type)
        {
            // Skill
            default:
            skillDescription.text = descriptionViewer.ReturnActiveDescriptionFromName(newName);
            break;
            case "Passive":
            skillDescription.text = "Increase [" + newName + "] passive level by 1.";
            skillDescription.text += "\n" + "First Level:";
            skillDescription.text += "\n" + passiveViewer.ReturnSpecificPassiveLevelEffect(newName, 1);
            break;
            case "Spell":
            skillDescription.text = spellViewer.ReturnSpellDescriptionFromName(newName);
            break;
        }
    }
}
