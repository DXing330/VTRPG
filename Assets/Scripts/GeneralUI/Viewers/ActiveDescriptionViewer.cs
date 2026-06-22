using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActiveDescriptionViewer : MonoBehaviour
{
    public ActiveDetailViewerSwitch activeDetailData;
    public ActiveSkill dummyActive;
    public StatDatabase activeData;
    public SelectStatTextList activeSelect;
    public PopUpMessage popUp;
    public void SelectActive(TacticActor actor = null)
    {
        if (activeSelect.GetSelected() < 0){return;}
        popUp.SetMessage(ReturnActiveDescriptionFromName(activeSelect.GetSelectedStat(), actor));
    }
    public string ReturnSpellDescription(MagicSpell spell, TacticActor caster = null)
    {
        string fullDetails = "";
        List<string> effects = spell.GetAllEffects();
        for (int i = 0; i < effects.Count; i++)
        {
            fullDetails += AED(effects[i], spell.GetSpecificsAt(i), spell.GetPowerAt(i).ToString());
            if (i < effects.Count - 1)
            {
                fullDetails += "\n";
            }
        }
        fullDetails += "\n" + "Action Cost: " + spell.GetActionCost();
        //+"; Actions Left: " +activeSkill;
        fullDetails += "\n" + "Mana Cost: "+spell.ReturnManaCost(caster);
        return fullDetails;
    }
    public string ReturnActiveDescriptionOnly(ActiveSkill activeSkill)
    {
        return ReturnActiveEffectDescriptions(activeSkill);
    }
    public string ReturnActiveDescriptionFromName(string activeName, TacticActor actor = null)
    {
        return activeDetailData.ReturnActiveDescriptionFromName(activeName, actor);
    }
    public string ReturnActiveDescriptionFromNameWithMod(string activeName, TacticActor actor, string modName)
    {
        dummyActive.LoadSkillFromStringWithMod(activeData.ReturnValue(activeName), actor, modName);
        return ReturnActiveDescription(dummyActive, actor);
    }
    public string ReturnActiveDescription(ActiveSkill activeSkill, TacticActor actor = null, BattleMap map = null)
    {
        return activeDetailData.ReturnActiveDescription(activeSkill, actor, map);
    }
    public string ReturnActiveEffectDescriptions(ActiveSkill activeSkill)
    {
        return activeDetailData.ReturnActiveEffectDescriptions(activeSkill);
    }
    public string AED(string e, string s, string p)
    {
        return activeDetailData.AED(e,s,p);
    }
    // TODO Skill Mod Descriptions
    public string GetSkillModDescription(string skillModName)
    {
        switch (skillModName)
        {
            default:
            return "";
            case "RandomBasic":
            return "Randomly Increase Power or Decrease Energy Cost or Decrease Action Cost";
            case "Free":
            return "Free Use.";
            case "FirstFree":
            return "Single Free Use.";
            case "Instinct":
            case "DoublePower":
            return "Double power.";
            case "Power":
            return "Increase power.";
            case "Energy":
            return "Decrease energy cost.";
            case "EnergyUp":
            return "Increase energy cost.";
            case "Action":
            case "Actions":
            return "Decrease action cost.";
            case "ActionsUp":
            return "Increase action cost.";
            case "Range":
            return "Increase target range.";
            case "RangeDown":
            return "Decrease target range.";
            case "Span":
            return "Increase target span (if span > 0).";
            // ADD EFFECTS TO THE SKILL.
            case "Swift":
            return "Targeted actor(s) gain 2 temporary movement";
            case "Momentum":
            return "Skill user gains 1 base attack";
            case "Sharp":
            return "Targeted actor(s) take 3 earth damage";
            case "Androit":
            return "Targeted actor(s) gain 6 temporary health";
            // SPECIAL EFFECTS.
            case "Copy":
            return "Gain a Skillbook Copy of the Skill";
        }
    }
}
