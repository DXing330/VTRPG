using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MagicSpell", menuName = "ScriptableObjects/BattleLogic/MagicSpell", order = 1)]
public class MagicSpell : ActiveSkill
{
    public StatDatabase spellAttributes;
    public override void LoadSkillFromString(string skillData, TacticActor actor = null)
    {
        skillInfo = standardSpells.ReturnValue(skillData);
        if (skillInfo == "")
        {
            skillInfo = skillData;
        }
        skillInfoList = new List<string>(skillInfo.Split(activeSkillDelimiter));
        LoadSkill(skillInfoList);
    }
    public override bool Activatable(TacticActor actor, BattleMap map)
    {
        if (actor.GetSilenced()){return false;}
        return (actor.GetActions() >= GetActionCost() && actor.GetMana() >= ReturnManaCost(actor));
    }
    public int ReturnManaCost(TacticActor actor = null, BattleMap map = null)
    {
        int cost = GetEnergyCost();
        if (actor != null)
        {
            string attribute = spellAttributes.ReturnValue(GetSkillName());
            int attributeCount = actor.AttributeCount(attribute);
            int nilCount = actor.AttributeCount("Nil");
            return AdjustCostByAttributes(cost, attributeCount, nilCount);
        }
        return cost;
    }
    protected int AdjustCostByAttributes(int baseCost, int attributeCount, int nilCount)
    {
        switch (attributeCount)
        {
            default:
            return Mathf.Max(1, baseCost) * (nilCount + 1);
            case 1:
            return Mathf.Max(1, baseCost / 2) * (nilCount + 1);
            case 2:
            return Mathf.Max(1, baseCost / 3) * (nilCount + 1);
            case 3:
            return Mathf.Max(1, baseCost / 4) * (nilCount + 1);
        }
    }
    public void AddEffects(string newInfo)
    {
        effect += effectDelimiter + newInfo;
        RefreshSkillInfo();
    }
    public void AddSpecifics(string newInfo)
    {
        specifics += effectDelimiter + newInfo;
        RefreshSkillInfo();
    }
    public void AddPowers(string newInfo)
    {
        power += effectDelimiter + newInfo;
        RefreshSkillInfo();
    }
    public string GetAllPowersString()
    {
        return String.Join(effectDelimiter, GetAllPowers());
    }
}
