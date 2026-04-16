using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// For Displaying Skillbook Rewards
public class SkillDisplay : MonoBehaviour
{
    // --- Data ---
    public StatDatabase skillBookData;
    public StatDatabase skillBookRarity;
    public StatDatabase colorlessSkillBookData;
    public StatDatabase colorlessSkillBookRarity;

    // --- Description Viewers ---
    public ActiveDescriptionViewer descriptionViewer;
    public SpellDetailViewer spellViewer;
    public PassiveDetailViewer passiveViewer;

    // --- Visuals ---
    public int skillRarity;
    public Image borderImage;
    public ColorDictionary skillBookColors;
    public Image leftPageImage;
    public Image rightPageImage;
    public GameObject highlightObject;

    // --- Text ---
    public TMP_Text skillName;
    public TMP_Text typeName;
    public TMP_Text skillDescription;

    public void SetSkill(string newName, string type = "Skill", int rarity = 1)
    {
        skillName.text = newName;
        typeName.text = "[" + type + "]";
        skillRarity = rarity;
        leftPageImage.color = skillBookColors.GetColorByIndex(skillRarity);
        rightPageImage.color = skillBookColors.GetColorByIndex(skillRarity);
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
        UpdatePageColors();
    }

    // Skillbook-first API for store/reward UIs that only know the book name.
    public void SetSkillBook(string skillBookName, bool colorless = false)
    {
        string skillBookValue = ReturnSkillBookValue(skillBookName, colorless);
        if (skillBookValue == "")
        {
            ResetDisplay();
            return;
        }
        string[] blocks = skillBookValue.Split("_");
        string learnedType = blocks.Length > 0 ? blocks[0] : "";
        string learnedName = blocks.Length > 1 ? blocks[1] : skillBookName;
        int rarity = 1;
        if (skillBookRarity != null)
        {
            int.TryParse(skillBookRarity.ReturnValue(skillBookName), out rarity);
            if (rarity <= 0){rarity = 1;}
        }
        SetSkill(learnedName, learnedType, rarity);
        skillName.text = skillBookName;
        UpdateFrameColor(colorless);
    }

    protected string ReturnSkillBookValue(string skillBookName, bool colorless = false)
    {
        if (colorless && colorlessSkillBookData != null)
        {
            return colorlessSkillBookData.ReturnValue(skillBookName);
        }
        if (skillBookData == null)
        {
            return "";
        }
        return skillBookData.ReturnValue(skillBookName);
    }

    protected void ResetDisplay()
    {
        skillName.text = "";
        typeName.text = "";
        skillDescription.text = "";
        skillRarity = 1;
        if (leftPageImage != null && skillBookColors != null)
        {
            leftPageImage.color = skillBookColors.GetDefaultColor();
        }
        if (rightPageImage != null && skillBookColors != null)
        {
            rightPageImage.color = skillBookColors.GetDefaultColor();
        }
    }

    protected void UpdatePageColors()
    {
        Color pageColor = skillBookColors.GetColorByIndex(skillRarity);
        if (leftPageImage != null)
        {
            leftPageImage.color = pageColor;
        }
        if (rightPageImage != null)
        {
            rightPageImage.color = pageColor;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(highlighted);
        }
    }

    protected void UpdateFrameColor(bool colorless = false)
    {
        // TODO different frame colors for different classes of skills.
    }
}
