using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stats that are saved outside of battle + current health/mana/statuses
public class ActorInitialStats : ActorPassives
{
    public GeneralUtility utility;
    public string delimiter = "!";
    public string allStatNames;
    [ContextMenu("LoadStatNames")]
    protected void LoadStatNames()
    {
        statNames = allStatNames.Split(delimiter).ToList();
        publicStats = allPublicStats.Split(delimiter).ToList();
    }
    public List<string> statNames;
    public string allPublicStats;
    public List<string> publicStats;
    public List<string> changeFormStatNames;
    public List<string> stats;
    public List<string> GetPublicStatNames()
    {
        List<string> names = new List<string>();
        for (int i = 0; i < statNames.Count; i++)
        {
            if (publicStats[i] == "0"){continue;}
            names.Add(statNames[i]);
        }
        return names;
    }
    public List<string> GetPublicStatInfo()
    {
        List<string> pStats = new List<string>();
        for (int i = 0; i < stats.Count; i++)
        {
            if (publicStats[i] == "0"){continue;}
            pStats.Add(stats[i]);
        }
        return pStats;
    }
    public string GetInitialStats()
    {
        string allStats = "";
        for (int i = 0; i < stats.Count; i++)
        {
            stats[i] = GetInitialStat(statNames[i]);
            allStats += stats[i];
            if (i < stats.Count - 1) { allStats += delimiter; }
        }
        return allStats;
    }
    public void SetInitialStatsFromString(string newStats)
    {
        SetInitialStats(newStats.Split(delimiter).ToList());
    }
    // Make sure the ordering and naming is correct based on the statNames.
    public void SetInitialStats(List<string> newStats)
    {
        List<string> newStatNames = new List<string>(statNames);
        ClearStatuses();
        ResetPassives();
        stats = newStats;
        for (int i = 0; i < stats.Count; i++)
        {
            SetInitialStat(stats[i], newStatNames[i]);
        }
        if (currentHealth <= 0) { currentHealth = GetBaseHealth(); }
        else if (currentHealth > GetBaseHealth()) { currentHealth = GetBaseHealth(); }
    }
    // GET
    protected string GetInitialStat(string statName)
    {
        switch (statName)
        {
            case "Sprite":
                return GetSpriteName();
            case "Species":
                return GetSpecies();
            case "Elements":
                return GetElementString();
            case "Attributes":
                return GetAttributeString();
            case "Health":
                return GetBaseHealth().ToString();
            case "Attack":
                return GetBaseAttack().ToString();
            case "Range":
                return GetAttackRange().ToString();
            case "Defense":
                return GetBaseDefense().ToString();
            case "MoveSpeed":
                return GetMoveSpeed().ToString();
            case "MoveType":
                return GetMoveType();
            case "Weight":
                return GetBaseWeight().ToString();
            case "Initiative":
                return GetInitiative().ToString();
            case "Luck":
                return GetLuck().ToString();
            case "CritChance":
                return GetBaseCritChance().ToString();
            case "CritDamage":
                return GetBaseCritDamage().ToString();
            case "HitChance":
                return GetBaseHitChance().ToString();
            case "Dodge":
                return GetBaseDodge().ToString();
            case "Passives":
                return GetPassiveString();
            case "PassiveLevels":
                return GetPassiveLevelString();
            case "CustomPassives":
                return GetCustomPassiveString();
            case "Energy":
                return GetBaseEnergy().ToString();
            case "Actives":
                return GetActivesString();
            case "ActiveMods":
                return GetActiveModsString();
            case "DeathActives":
                return GetDeathActivesString();
            case "Spells":
                return GetSpellsString();
            case "SpellMods":
                return GetSpellModsString();
            case "MagicPower":
                return GetMagicPower().ToString();
            case "MagicResist":
                return GetMagicResist().ToString();
            case "ManaEfficiency":
                return GetManaEfficiency().ToString();
            case "MaxMana":
                return GetMaxMana().ToString();
            case "CustomSpells":
                return GetCustomSpellsString();
            case "ItemSlots":
                return GetItemSlots().ToString();
            // PERSISTENT STATS
            case "CurrentMana":
                return GetMana().ToString();
            case "CurrentHealth":
                return GetHealth().ToString();
            case "Curses":
                return GetCurseString();
        }
        return "";
    }
    // SET
    protected void SetInitialStat(string newStat, string statName)
    {
        switch (statName)
        {
            case "Sprite":
                SetSpriteName(newStat);
                break;
            case "Species":
                SetSpecies(newStat);
                break;
            case "Elements":
                SetElementsFromString(newStat);
                break;
            case "Attributes":
                SetAttributesFromString(newStat);
                break;
            case "Health":
                SetBaseHealth(utility.SafeParseInt(newStat));
                break;
            case "Energy":
                SetBaseEnergy(utility.SafeParseInt(newStat));
                break;
            case "Attack":
                SetBaseAttack(utility.SafeParseInt(newStat));
                break;
            case "Range":
                SetAttackRange(utility.SafeParseInt(newStat));
                break;
            case "Defense":
                SetBaseDefense(utility.SafeParseInt(newStat));
                break;
            case "MoveSpeed":
                SetMoveSpeed(utility.SafeParseInt(newStat));
                break;
            case "MoveType":
                SetMoveType(newStat);
                break;
            case "Weight":
                SetWeight(utility.SafeParseInt(newStat));
                break;
            case "Initiative":
                SetInitiative(utility.SafeParseInt(newStat));
                break;
            case "Luck":
                SetLuck(utility.SafeParseInt(newStat));
                break;
            case "CritChance":
                SetBaseCritChance(utility.SafeParseInt(newStat));
                break;
            case "CritDamage":
                SetBaseCritDamage(utility.SafeParseInt(newStat));
                break;
            case "HitChance":
                SetBaseHitChance(utility.SafeParseInt(newStat));
                break;
            case "Dodge":
                SetBaseDodge(utility.SafeParseInt(newStat));
                break;
            case "Passives":
                SetPassiveSkills(newStat.Split(passiveDelimiter).ToList());
                break;
            case "PassiveLevels":
                SetPassiveLevels(newStat.Split(passiveDelimiter).ToList());
                break;
            case "CustomPassives":
                SetCustomPassives(newStat.Split(passiveDelimiter).ToList());
                break;
            case "Actives":
                SetActiveSkills(newStat.Split(",").ToList());
                break;
            case "ActiveMods":
                SetActiveMods(newStat.Split(",").ToList());
                break;
            case "DeathActives":
                SetDeathActives(newStat.Split(",").ToList());
                break;
            case "MagicPower":
                SetMagicPower(utility.SafeParseInt(newStat));
                break;
            case "MagicResist":
                SetMagicResist(utility.SafeParseInt(newStat));
                break;
            case "ManaEfficiency":
                SetManaEfficiency(utility.SafeParseInt(newStat));
                break;
            case "MaxMana":
                SetMaxMana(utility.SafeParseInt(newStat));
                break;
            case "Spells":
                SetSpells(newStat.Split(",").ToList());
                break;
            case "SpellMods":
                SetSpellMods(newStat.Split(",").ToList());
                break;
            case "CustomSpells":
                SetCustomSpells(newStat.Split(passiveDelimiter).ToList());
                break;
            case "ItemSlots":
                SetItemSlots(utility.SafeParseInt(newStat));
                break;
            case "CurrentMana":
                SetMana(utility.SafeParseInt(newStat));
                break;
            case "CurrentHealth":
                SetCurrentHealth(utility.SafeParseInt(newStat));
                break;
            case "Curses":
                // If they were kept then they must have had infinite duration.
                List<string> curses = newStat.Split(",").ToList();
                for (int i = 0; i < curses.Count; i++)
                {
                    AddStatus(curses[i], -1);
                }
                break;
        }
    }
    //// INITIAL STATS
    public string species;
    public void SetSpecies(string newSpecies){species = newSpecies;}
    public string GetSpecies(){return species;}
    public string spriteName;
    public void SetSpriteName(string newName){spriteName = newName;}
    public string GetSpriteName(){return spriteName;}
    public List<string> elements;
    public void SetElementsFromString(string allElements, string delimiter = ",")
    {
        ResetElements();
        elements = allElements.Split(delimiter).ToList();
    }
    public string GetElementString()
    {
        if (elements.Count == 0) { return ""; }
        return String.Join(",", elements);
    }
    public void ResetElements(){elements.Clear();}
    public void AddElement(string newInfo){elements.Add(newInfo);}
    public List<string> GetElements(){return elements;}
    public bool SameElement(string newInfo)
    {
        return elements.Contains(newInfo);
    }
    public List<string> attributes;
    public void ResetAttributes(){attributes.Clear();}
    public void AddAttribute(string newAttribute)
    {
        if (newAttribute.Length < 2){return;}
        attributes.Add(newAttribute);
    }
    public void SetAttributesFromString(string newInfo, string delimiter = ",")
    {
        ResetAttributes();
        attributes = newInfo.Split(delimiter).ToList();
        utility.RemoveEmptyListItems(attributes);
    }
    public string GetAttributeString()
    {
        if (attributes.Count == 0) { return ""; }
        return String.Join(",", attributes);
    }
    public void InitializeAttributes(StatDatabase attrDB)
    {
        string randomAttr = attrDB.ReturnRandomKey();
        if (attrDB.ReturnValue(randomAttr) == "1" && attributes.Count < 3)
        {
            AddAttribute(randomAttr);
        }
        if (attributes.Count < 3)
        {
            InitializeAttributes(attrDB);
        }
    }
    public List<string> GetAttributes()
    {
        return attributes;
    }
    public int AttributeCount(string attribute)
    {
        return utility.CountStringsInList(attributes, attribute);
    }
    public int baseHealth;
    public void SetBaseHealth(int newHealth) { baseHealth = newHealth; }
    public int GetBaseHealth() { return baseHealth; }
    public void UpdateBaseHealth(int changeAmount, bool decrease = true)
    {
        if (decrease) { baseHealth -= changeAmount; }
        else { baseHealth += changeAmount; }
    }
    public int currentHealth;
    public void SetCurrentHealth(int newHealth) { currentHealth = newHealth; }
    public int GetHealth() { return currentHealth; }
    public int baseAttack;
    public void SetBaseAttack(int newAttack) { baseAttack = newAttack; }
    public int GetBaseAttack() { return baseAttack; }
    public void UpdateBaseAttack(int changeAmount) { baseAttack += changeAmount; }
    public int attackRange;
    public void SetAttackRange(int newRange) { attackRange = newRange; }
    public void SetAttackRangeMax(int newRange)
    {
        attackRange = Mathf.Max(attackRange, newRange);
    }
    public int GetAttackRange() { return attackRange + bonusAttackRange; }
    public void UpdateAttackRange(int changeAmount) { attackRange += changeAmount; }
    public int bonusAttackRange;
    public void ResetBonusAttackRange()
    {
        bonusAttackRange = 0;
    }
    public void UpdateBonusAttackRange(int changeAmount)
    {
        bonusAttackRange += changeAmount;
    }
    public int baseDefense;
    public void SetBaseDefense(int newDefense) { baseDefense = newDefense; }
    public int GetBaseDefense() { return baseDefense; }
    public void UpdateBaseDefense(int changeAmount) { baseDefense += changeAmount; }
    public int moveSpeed;
    public virtual void SetMoveSpeed(int newMoveSpeed)
    {
        moveSpeed = newMoveSpeed;
    }
    public void SetMoveSpeedMax(int newMax)
    {
        moveSpeed = Mathf.Max(moveSpeed, newMax);
    }
    public void UpdateBaseSpeed(int changeAmount) { moveSpeed += changeAmount; }
    public int GetMoveSpeed() { return moveSpeed; }
    public string moveType;
    public void SetMoveType(string newMoveType) { moveType = newMoveType; }
    public string GetMoveType() { return moveType; }
    public int weight;
    public int GetBaseWeight() { return weight; }
    public void SetWeight(int newWeight) { weight = newWeight; }
    public int initiative;
    public void SetInitiative(int newInitiative) { initiative = newInitiative; }
    public int GetInitiative() { return initiative; }
    // Luck stat for fun.
    public int luck;
    public void SetLuck(int newInfo){luck = newInfo;}
    public int GetLuck(){return luck;}
    public void ChangeLuck(int amount){luck += amount;}
    public int baseCrit;
    public int GetBaseCritChance(){return baseCrit;}
    public void SetBaseCritChance(int newInfo){baseCrit = newInfo;}
    public int baseCritDamage;
    public int GetBaseCritDamage(){return baseCritDamage;}
    public void SetBaseCritDamage(int newInfo){baseCritDamage = newInfo;}
    public int baseHitChance;
    public int GetBaseHitChance(){return baseHitChance;}
    public void SetBaseHitChance(int newInfo){baseHitChance = newInfo;}
    public int baseDodge;
    public int GetBaseDodge(){return baseDodge;}
    public void SetBaseDodge(int newInfo){baseDodge = newInfo;}
    public int baseEnergy;
    public void UpdateBaseEnergy(int changeAmount)
    {
        baseEnergy += changeAmount;
        if (baseEnergy < 0)
        {
            baseEnergy = 0;
        }
    }
    public void SetBaseEnergy(int newEnergy) { baseEnergy = newEnergy; }
    public int GetBaseEnergy() { return baseEnergy; }
    public List<string> activeSkills;
    public void SetActiveSkills(List<string> newSkills)
    {
        activeSkills = newSkills;
        if (activeSkills.Count == 0) { return; }
        for (int i = activeSkills.Count - 1; i >= 0; i--)
        {
            if (activeSkills[i].Length <= 1) { activeSkills.RemoveAt(i); }
        }
    }
    public string GetActivesString()
    {
        if (activeSkills.Count == 0) { return ""; }
        return String.Join(",", activeSkills);
    }
    protected string activeSkillDelimiter = "_";
    // TODO Implement The Active Mods And Display The Modified Actives In Armory
    // Active Mods Buff Power/Energy/Action/Range/Span
    // Each Active Can Have X(2+?) Mods But Each Mod Can Only Apply 1(2+?) Time To Each Active.
    public List<string> activeMods;
    public bool ActiveUpgraded(string skillName)
    {
        for (int i = 0; i < activeMods.Count; i++)
        {
            string[] modDetails = activeMods[i].Split("_");
            if (modDetails[0] != skillName){continue;}
            if (modDetails[1] == "Power" || modDetails[1] == "Energy" || modDetails[1] == "Action")
            {
                return true;
            }
        }
        return false;
    }
    public bool ActiveModified(string skillName)
    {
        for (int i = 0; i < activeMods.Count; i++)
        {
            string[] modDetails = activeMods[i].Split("_");
            if (modDetails[0] != skillName){continue;}
            if (modDetails[1] != "Power" && modDetails[1] != "Energy" && modDetails[1] != "Action")
            {
                return true;
            }
        }
        return false;
    }
    public List<string> GetActiveMods()
    {
        return activeMods;
    }
    public string GetActiveModsString()
    {
        if (activeMods.Count == 0) { return ""; }
        return String.Join(",", activeMods);
    }
    public void SetActiveMods(List<string> newMods)
    {
        activeMods = newMods;
        if (activeMods.Count == 0) { return; }
        for (int i = activeMods.Count - 1; i >= 0; i--)
        {
            if (activeMods[i].Length <= 1) { activeMods.RemoveAt(i); }
        }
    }
    public int magicPower;
    public void SetMagicPower(int newInfo)
    {
        magicPower = newInfo;
    }
    public int GetMagicPower(){return magicPower;}
    public void GainMagicPower(int amount)
    {
        magicPower += amount;
    }
    public int magicResist;
    public void SetMagicResist(int newInfo)
    {
        magicResist = newInfo;
    }
    public int GetMagicResist(){return magicResist;}
    public void GainMagicResist(int amount)
    {
        magicResist += amount;
    }
    // Only applies to elemental damage, which is from actives and spells.
    public int ApplyMagicResist(int damage)
    {
        if (GetMagicResist() >= 100){return 0;}
        // Magic Resist is a percentage reduction.
        damage = damage * (100 - GetMagicPower()) / 100;
        return damage;
    }
    // How much bonus mana you get from consuming 1 mana.
    // Can be negative.
    public int manaEfficiency;
    public void IncreaseManaEfficiency(int amount)
    {
        manaEfficiency += amount;
    }
    public void SetManaEfficiency(int newInfo)
    {
        manaEfficiency = newInfo;
    }
    public int GetManaEfficiency(){return manaEfficiency;}
    public int maxMana;
    public void SetMaxMana(int amount)
    {
        maxMana = amount;
    }
    public int GetMaxMana(){return maxMana;}
    public List<string> spells;
    public string GetSpellsString()
    {
        if (spells.Count == 0) { return ""; }
        return String.Join(",", spells);
    }
    public void SetSpells(List<string> newSkills)
    {
        spells = newSkills;
        if (spells.Count == 0) { return; }
        for (int i = spells.Count - 1; i >= 0; i--)
        {
            if (spells[i].Length <= 1) { spells.RemoveAt(i); }
        }
    }
    public List<string> spellMods;
    public string GetSpellModsString()
    {
        if (spellMods.Count == 0) { return ""; }
        return String.Join(",", spellMods);
    }
    public void SetSpellMods(List<string> newMods)
    {
        spellMods = newMods;
        if (spellMods.Count == 0) { return; }
        for (int i = spellMods.Count - 1; i >= 0; i--)
        {
            if (spellMods[i].Length <= 1) { spellMods.RemoveAt(i); }
        }
    }
    public List<string> customSpells;
    public void SetCustomSpells(List<string> newInfo)
    {
        customSpells = new List<string>(newInfo);
    }
    public List<string> GetCustomSpells(){return customSpells;}
    public string GetCustomSpellsString()
    {
        if (customSpells.Count == 0) { return ""; }
        return String.Join(",", customSpells);
    }
    //// PERSISTENT STATS ACROSS BATTLES
    public List<string> statuses;
    public List<int> statusDurations;
    public List<string> GetStatuses() { return statuses; }
    public virtual void AddStatus(string newCondition, int duration)
    {
        // Don't add blank statuses.
        if (newCondition.Length <= 1 || newCondition.Trim().Length <= 1) { return; }
        // Permanent statuses can stack up infinitely and are a win condition.
        if (duration < 0)
        {
            statuses.Add(newCondition);
            statusDurations.Add(duration);
            return;
        }
        int indexOf = statuses.IndexOf(newCondition);
        if (indexOf < 0)
        {
            statuses.Add(newCondition);
            statusDurations.Add(duration);
        }
        else
        {
            statusDurations[indexOf] = statusDurations[indexOf] + duration;
        }
    }
    public void ClearStatuses(string specifics = "*")
    {
        if (specifics == "*")
        {
            statusDurations.Clear();
            statuses.Clear();
            return;
        }
        RemoveStatus(specifics);
    }
    public void RemoveStatus(string statusName)
    {
        if (statusName == "All")
        {
            statusDurations.Clear();
            statuses.Clear();
            return;
        }
        for (int i = statuses.Count - 1; i >= 0; i--)
        {
            if (statuses[i] == statusName)
            {
                statuses.RemoveAt(i);
                statusDurations.RemoveAt(i);
            }
        }
    }
    public string curseStatName;
    public void AddCurse(string newInfo)
    {
        if (newInfo.Length <= 0){return;}
        AddStatus(newInfo, -1);
    }
    public void SetCurses(string newInfo)
    {
        // Set implies reseting and starting from the beginning.
        ClearStatuses();
        int index = statNames.IndexOf(curseStatName);
        stats[index] = newInfo;
        string[] blocks = newInfo.Split(",");
        for (int i = 0; i < blocks.Length; i++)
        {
            AddCurse(blocks[i]);
        }
    }
    public List<string> GetCurses()
    {
        List<string> curses = new List<string>();
        for (int i = 0; i < statuses.Count; i++)
        {
            if (statusDurations[i] < 0 && statuses[i].Length > 0) { curses.Add(statuses[i]); }
        }
        return curses;
    }
    public string GetCurseString()
    {
        List<string> curses = GetCurses();
        string curseString = "";
        if (curses.Count <= 0) { return curseString; }
        for (int i = 0; i < curses.Count; i++)
        {
            curseString += curses[i];
            if (i < curses.Count - 1) { curseString += ","; }
        }
        return curseString;
    }
    public int currentMana;
    public void SetCurrentMana(int amount)
    {
        currentMana = amount;
    }
    public void SetMana(int amount)
    {
        currentMana = amount;
    }
    public int GetMana(){return currentMana;}
    public void UseMana(int amount)
    {
        currentMana -= amount;
    }
    public void RestoreMana(int amount)
    {
        currentMana += Mathf.Max(0, amount + manaEfficiency);
    }
    public int itemSlots;
    public void SetItemSlots(int amount)
    {
        itemSlots = amount;
    }
    public int GetItemSlots(){return itemSlots;}
    public void UpdateItemSlots(int amount)
    {
        itemSlots += amount;
    }
}
