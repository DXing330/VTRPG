using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Active", menuName = "ScriptableObjects/BattleLogic/Active", order = 1)]
public class ActiveSkill : SkillEffect
{
    // Used to check the conditions of active cost adjusting passives.
    public PassiveSkill passiveChecker;
    public string activeSkillDelimiter = "_";
    public string skillInfo;
    public string GetSkillInfo(){return skillInfo;}
    public List<string> skillInfoList;
    public List<string> GetSkillInfoList(){ return skillInfoList; }
    public virtual void RefreshSkillInfo()
    {
        skillInfoList[0] = skillName;
        skillInfoList[1] = skillType;
        skillInfoList[2] = energyCost;
        skillInfoList[3] = actionCost;
        skillInfoList[4] = range;
        skillInfoList[5] = rangeShape;
        skillInfoList[6] = shape;
        skillInfoList[7] = span;
        skillInfoList[8] = effect;
        skillInfoList[9] = specifics;
        skillInfoList[10] = power;
        if (skillInfoList.Count < 12)
        {
            skillInfo = String.Join(activeSkillDelimiter, skillInfoList);
            return;
        }
        skillInfoList[11] = healthCost;
        if (skillInfoList.Count < 13)
        {
            skillInfo = String.Join(activeSkillDelimiter, skillInfoList);
            return;
        }
        skillInfoList[12] = scalingSpecifics; // Power Or Specifics
        skillInfo = String.Join(activeSkillDelimiter, skillInfoList);
    }
    public string skillName;
    public void SetSkillName(string newInfo)
    {
        skillName = newInfo;
        RefreshSkillInfo();
    }
    public string GetSkillName()
    {
        return skillName;
    }
    public string skillType;
    public string GetSkillType(){return skillType;}
    public virtual void LoadSkillFromString(string skillData, TacticActor actor)
    {
        skillInfo = skillData;
        skillInfoList = new List<string>(skillData.Split(activeSkillDelimiter));
        LoadSkill(skillInfoList, skillData);
        // Load Mods As Soon As You Load The Skill?
        if (actor != null)
        {
            ApplyActorMods(actor);
        }
    }
    public virtual void ResetSkillInfo()
    {
        skillName = "";
        skillType = "Support";
        energyCost = "2";
        actionCost = "2";
        range = "0";
        rangeShape = "Circle";
        shape = "None";
        span = "0";
        effect = "Passive";
        specifics = "";
        power = "1";
        healthCost = "0";
        scalingSpecifics = "Power";
    }
    public virtual void LoadSkill(List<string> skillData, string newName = "")
    {
        ResetSkillInfo();
        if (skillData.Count <= 10)
        {
            specifics = newName;
            return;
        }
        skillName = skillData[0];
        skillType = skillData[1];
        energyCost = skillData[2];
        actionCost = skillData[3];
        range = skillData[4];
        rangeShape = skillData[5];
        shape = skillData[6];
        span = skillData[7];
        effect = skillData[8];
        specifics = skillData[9];
        power = skillData[10];
        if (skillData.Count <= 11){return;}
        healthCost = skillData[11];
        if (skillData.Count <= 12){return;}
        scalingSpecifics = skillData[12];
    }
    public virtual void ApplyActorMods(TacticActor actor)
    {
        List<string> actorMods = actor.GetActiveMods();
        List<string> mods = actorMods == null ? new List<string>() : new List<string>(actorMods);
        List<string> nextMods = actor.GetNextSkillMods();
        for (int i = 0; i < nextMods.Count; i++)
        {
            mods.Add(skillName + activeSkillDelimiter + nextMods[i]);
        }
        for (int i = 0; i < mods.Count; i++)
        {
            string[] modDetails = mods[i].Split(activeSkillDelimiter);
            if (modDetails[0] != skillName){continue;}
            // Active Mods Buff Power/Energy/Action/Range/Span
            ApplySingleActorMod(actor, modDetails);
        }
    }

    protected virtual void ApplySingleActorMod(TacticActor actor, string[] modDetails)
    {
        if (modDetails.Length < 2){return;}
        switch (modDetails[1])
        {
            default:
            break;
            case "Free":
            energyCost = "0";
            actionCost = "0";
            break;
            case "FirstFree":
            // Check if the skill has been cast already.
            if (actor.SkillUsedAlready(GetSkillName()))
            {
                break;
            }
            energyCost = "0";
            actionCost = "0";
            break;
            case "DoublePower":
            ApplyPowerMod(true);
            break;
            case "Power":
            ApplyPowerMod();
            break;
            case "Energy":
            energyCost = Mathf.Max(0, int.Parse(energyCost) - 1).ToString();
            break;
            case "EnergyUp":
            energyCost = Mathf.Max(0, int.Parse(energyCost) + 1).ToString();
            break;
            case "Action":
            case "Actions":
            actionCost = Mathf.Max(0, int.Parse(actionCost) - 1).ToString();
            break;
            case "ActionsUp":
            actionCost = Mathf.Max(0, int.Parse(actionCost) + 1).ToString();
            break;
            case "Range":
            // If it's a string then add a plus.
            if (range.Length > 2)
            {
                range = range + "+";
            }
            // Else add 1.
            else
            {
                range = (int.Parse(range) + 1).ToString();
            }
            break;
            case "RangeDown":
            // If it's a string then add a minus.
            if (range.Length > 2)
            {
                range = range + "-";
            }
            // Else subtract 1.
            else
            {
                range = (int.Parse(range) - 1).ToString();
            }
            break;
            case "Span":
            if (span.Length <= 0){break;}
            span = (int.Parse(span) + 1).ToString();
            break;
            // Requires a third string for specifics.
            case "SpanShape":
            if (modDetails.Length > 2){shape = modDetails[2];}
            break;
            case "RangeShape":
            if (modDetails.Length > 2){rangeShape = modDetails[2];}
            break;
        }
    }

    protected virtual void ApplyPowerMod(bool doubled = false)
    {
        List<string> powers = GetAllPowerStrings();
        List<string> specificsList = GetAllSpecifics();
        List<string> scalingList = GetAllScalingSpecifics();
        int effectCount = Mathf.Max(GetAllEffects().Count, Mathf.Max(powers.Count, specificsList.Count));
        HashSet<int> modifiedPowerIndexes = new HashSet<int>();
        HashSet<int> modifiedSpecificsIndexes = new HashSet<int>();
        for (int i = 0; i < effectCount; i++)
        {
            switch (GetMatchingString(scalingList, i, "Power"))
            {
                case "Power":
                int powerIndex = NormalizeIndex(powers, i);
                if (modifiedPowerIndexes.Contains(powerIndex)){break;}
                int powerInt = utility.SafeParseInt(GetMatchingString(powers, i, "1"), 0);
                if (powerInt <= 0){break;}
                powers[powerIndex] = PowerModdedInt(powerInt, doubled).ToString();
                modifiedPowerIndexes.Add(powerIndex);
                break;
                case "Specifics":
                int specificsIndex = NormalizeIndex(specificsList, i);
                if (modifiedSpecificsIndexes.Contains(specificsIndex)){break;}
                int specificsInt = utility.SafeParseInt(GetMatchingString(specificsList, i, "0"), 0);
                if (specificsInt <= 0){break;}
                specificsList[specificsIndex] = PowerModdedInt(specificsInt, doubled).ToString();
                modifiedSpecificsIndexes.Add(specificsIndex);
                break;
            }
        }
        power = String.Join(effectDelimiter, powers);
        specifics = String.Join(effectDelimiter, specificsList);
    }
    // Default is 50% boost.
    protected int PowerModdedInt(int value, bool doubled = false)
    {
        if (value == 1){return 2;}
        if (doubled)
        {
            return value * 2;
        }
        return (value * 3) / 2;
    }

    protected int NormalizeIndex(List<string> entries, int index)
    {
        if (entries.Count <= 1){return 0;}
        return Mathf.Min(index, entries.Count - 1);
    }
    public string GetStat(string statName)
    {
        switch (statName)
        {
            case "Range":
                return range.ToString();
            case "RangeShape":
                return rangeShape.ToString();
            case "EffectShape":
                return shape.ToString();
            case "EffectSpan":
                return span.ToString();
        }
        return "";
    }
    public string energyCost;
    public void SetEnergyCost(string newInfo)
    {
        energyCost = newInfo;
        RefreshSkillInfo();
    }
    public int GetEnergyCost(TacticActor actor = null, BattleMap map = null)
    {
        int cost = utility.SafeParseInt(energyCost);
        if (actor != null && map != null)
        {
            (int aCost, int eCost) = GetAdjustedCost(actor, map);
            cost = eCost;
        }
        return cost;
    }
    public void AddEnergyCost(int newInfo)
    {
        energyCost = (newInfo + GetEnergyCost()).ToString();
        RefreshSkillInfo();
    }
    public string actionCost;
    public int GetActionCost(TacticActor actor = null, BattleMap map = null)
    {
        int cost = utility.SafeParseInt(actionCost);
        if (actor != null && map != null)
        {
            (int aCost, int eCost) = GetAdjustedCost(actor, map);
            cost = aCost;
        }
        return cost;
    }
    public string healthCost;
    public int GetHealthCost()
    {
        int cost = utility.SafeParseInt(healthCost);
        return cost;
    }
    public string scalingSpecifics;
    public string GetScalingSpecifics(){return scalingSpecifics;}
    protected int flatActionAdjust = 0;
    protected int percentActionAdjust = 0;
    protected int flatEnergyAdjust = 0;
    protected int percentEnergyAdjust = 0;
    protected int overrideActionValue = -1;
    protected int overrideEnergyValue = -1;
    public void AdjustFlatActionCost(int amount)
    {
        flatActionAdjust += amount;
    }
    public void AdjustPercentActionCost(int percent)
    {
        percentActionAdjust += percent;
    }
    public void AdjustFlatEnergyCost(int amount)
    {
        flatEnergyAdjust += amount;
    }
    public void AdjustPercentEnergyCost(int percent)
    {
        percentEnergyAdjust += percent;
    }
    public void SetActionCostOverride(int value)
    {
        if (overrideActionValue > -1)
        {
            overrideActionValue = Mathf.Min(overrideActionValue, value);
            return;
        }
        overrideActionValue = value;
    }
    public void SetEnergyCostOverride(int value)
    {
        if (overrideEnergyValue > -1)
        {
            overrideEnergyValue = Mathf.Min(overrideEnergyValue, value);
            return;
        }
        overrideEnergyValue = value;
    }
    protected virtual (int aCost, int eCost) GetAdjustedCost(TacticActor actor, BattleMap map)
    {
        int newACost = int.Parse(actionCost);
        int newECost = int.Parse(energyCost);
        flatActionAdjust = 0;
        percentActionAdjust = 0;
        flatEnergyAdjust = 0;
        percentEnergyAdjust = 0;
        overrideActionValue = -1;
        overrideEnergyValue = -1;
        // Iterate through active cost adjustment passives.
        List<string> adjustPassives = actor.GetAdjustActivesPassives();
        for (int i = 0; i < adjustPassives.Count; i++)
        {
            passiveChecker.ApplyAdjustCostPassive(actor, this, map, adjustPassives[i]);
        }
        // Apply flat changes then percentage changes.
        newACost += flatActionAdjust;
        newACost += newACost * (percentActionAdjust) / 100;
        newECost += flatEnergyAdjust;
        newECost += newECost * (percentEnergyAdjust) / 100;
        // Clamp the costs.
        newACost = Mathf.Max(0, newACost);
        newECost = Mathf.Max(0, newECost);
        // Apply the override if applicable.
        if (overrideEnergyValue > -1)
        {
            newECost = overrideEnergyValue;
        }
        if (overrideActionValue > - 1)
        {
            newACost = overrideActionValue;
        }
        return (newACost, newECost);
    }
    public virtual bool Activatable(TacticActor actor, BattleMap map)
    {
        if (actor.GetSilenced()){return false;}
        (int actionCost, int energyCost) = GetAdjustedCost(actor, map);
        return (actor.GetActions() >= actionCost && actor.GetEnergy() >= energyCost);
    }
    public string range;
    public void SetRange(string newInfo)
    {
        range = newInfo;
        RefreshSkillInfo();
    }
    public string GetRangeString(TacticActor actor = null, BattleMap map = null)
    {
        if (range.Length > 2){return range;}
        return GetRange(actor, map).ToString();
    }
    public int GetRange(TacticActor skillUser = null, BattleMap map = null)
    {
        if (range == "") { return 0; }
        int plusCount = 0;
        string rangeString = range;
        for (int i = range.Length - 1; i >= 0; i--)
        {
            if (range[i] == '+')
            {
                plusCount++;
                rangeString = rangeString.Remove(i, 1);
            }
            else if (range[i] == '-')
            {
                plusCount--;
                rangeString = rangeString.Remove(i, 1);
            }
        }
        switch (rangeString)
        {
            case "Move":
                if (skillUser == null) { return 1; }
                return Mathf.Max(0, skillUser.GetSpeed() + plusCount);
            case "AttackRange":
                if (skillUser == null) { return 1; }
                return Mathf.Max(0, skillUser.GetAttackRange() + plusCount);
        }
        return Mathf.Max(0, int.Parse(range));
    }
    public string rangeShape;
    public void SetRangeShape(string newInfo)
    {
        rangeShape = newInfo;
        RefreshSkillInfo();
    }
    public string GetRangeShape() { return rangeShape; }
    public int targetTile;
    public string shape;
    public void SetShape(string newInfo)
    {
        shape = newInfo;
        RefreshSkillInfo();
    }
    public string GetShape() { return shape; }
    public string span;
    public void SetSpan(string newInfo)
    {
        span = newInfo;
        RefreshSkillInfo();
    }
    public int GetSpan(TacticActor actor = null, BattleMap map = null)
    {
        if (span.Length <= 0)
        {
            return 0;
        }
        return int.Parse(span);
    }
    public int selectedTile;
    public void ResetSelectedTile(){ selectedTile = -1; }
    public void SetSelectedTile(int newInfo) { selectedTile = newInfo; }
    public int GetSelectedTile(){ return selectedTile; }
    List<int> targetedTiles;
    public void SetTargetedTiles(List<int> newTiles){targetedTiles = newTiles;}
    public List<int> GetTargetedTiles(){ return targetedTiles; }
    // Return a list of actors on those tiles.
    List<TacticActor> targetedActors;
    public void SetTargetedActors(List<TacticActor> newTargets){targetedActors = newTargets;}
    public string effect;
    public string GetEffect(){return effect;}
    public string specifics;
    public string GetSpecifics(){return specifics;}
    public string power;
    public string GetPowerString()
    {
        return power;
    }
    public int GetPower()
    {
        return utility.SafeParseInt(power);
    }
    public string effectDelimiter = "?";
    public List<string> GetAllEffects()
    {
        return SplitEffectField(GetEffect());
    }
    public List<string> GetAllSpecifics()
    {
        return SplitEffectField(GetSpecifics());
    }
    public List<string> GetAllPowerStrings()
    {
        return SplitEffectField(GetPowerString());
    }
    public List<int> GetAllPowers()
    {
        List<string> temp = GetAllPowerStrings();
        List<int> powers = new List<int>();
        for (int i = 0; i < temp.Count; i++)
        {
            powers.Add(utility.SafeParseInt(temp[i], 1));
        }
        return powers;
    }
    public List<string> GetAllScalingSpecifics()
    {
        return SplitEffectField(GetScalingSpecifics());
    }
    public string GetEffectAt(int index)
    {
        return GetMatchingString(GetAllEffects(), index, "");
    }
    public string GetSpecificsAt(int index)
    {
        return GetMatchingString(GetAllSpecifics(), index, "");
    }
    public int GetPowerAt(int index)
    {
        return utility.SafeParseInt(GetMatchingString(GetAllPowerStrings(), index, "1"), 1);
    }
    public string GetScalingSpecificsAt(int index)
    {
        return GetMatchingString(GetAllScalingSpecifics(), index, "Power");
    }
    protected List<string> SplitEffectField(string field)
    {
        return field.Split(effectDelimiter).ToList();
    }
    protected string GetMatchingString(List<string> entries, int index, string defaultValue = "")
    {
        if (entries == null || entries.Count <= 0){return defaultValue;}
        if (entries.Count == 1){return entries[0];}
        if (index < entries.Count){return entries[index];}
        return defaultValue;
    }
    public bool CanTrainPower()
    {
        switch (scalingSpecifics)
        {
            case "Power":
                return GetPower() > 0;
            case "Specifics":
                return utility.SafeParseInt(specifics) > 0;
            default:
                return false;
        }
    }
    public void AffectActors(List<TacticActor> actors, string effect, string specifics, int power)
    {
        for (int i = 0; i < actors.Count; i++)
        {
            AffectActor(actors[i], effect, specifics, power);
        }
    }
}
