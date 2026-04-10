using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorPassives : MonoBehaviour
{
    public string passiveDelimiter = "+";
    // Only obtained from magic runes in equipment.
    // Later can make custom runes in the mage tower.
    public List<string> runePassives;
    public void AddRunePassive(string newRune)
    {
        if (runePassives.Contains(newRune)){return;}
        if (newRune.Length <= 1){return;}
        runePassives.Add(newRune);
    }
    public List<string> GetRunePassives(){return runePassives;}
    // Only obtained through custom training.
    public List<string> customPassives;
    public int CustomPassiveCount()
    {
        return customPassives.Count;
    }
    public List<string> GetCustomPassives()
    {
        return customPassives;
    }
    public string GetCustomPassiveString()
    {
        if (customPassives.Count == 0) { return ""; }
        return String.Join(passiveDelimiter, customPassives);
    }
    public void SetCustomPassives(List<string> newPassives)
    {
        customPassives = new List<string>(newPassives);
        if (customPassives.Count == 0) { return; }
        for (int i = customPassives.Count - 1; i >= 0; i--)
        {
            if (customPassives[i].Length <= 1) { customPassives.RemoveAt(i); }
        }
    }
    public void AddCustomPassive(string newInfo)
    {
        if (newInfo.Length < 6){return;}
        customPassives.Add(newInfo);
    }
    public bool CustomPassiveExists(string newInfo)
    {
        if (newInfo.Length < 6){return false;}
        return customPassives.Contains(newInfo);
    }
    public void ResetPassives()
    {
        customPassives.Clear();
        runePassives.Clear();
        passiveSkills.Clear();
        passiveLevels.Clear();
    }
    public List<string> passiveSkills;
    public bool AnyPassiveExists(List<string> pNames)
    {
        for (int i = 0; i < pNames.Count; i++)
        {
            if (PassiveExists(pNames[i])){return true;}
        }
        return false;
    }
    public bool PassiveExists(string pName)
    {
        return passiveSkills.Contains(pName);
    }
    // Usually only get new passives based on equipment at the start of battle, before you start organizing passives.
    // Can also use to this learn new permanent passive skills.
    public void AddPassiveSkill(string skillName, string skillLevel = "1")
    {
        if (skillName == "")
        {
            return;
        }
        int indexOf = passiveSkills.IndexOf(skillName);
        if (indexOf < 0)
        {
            passiveSkills.Add(skillName);
            passiveLevels.Add(skillLevel);
        }
        else
        {
            int newLevel = int.Parse(passiveLevels[indexOf]) + int.Parse(skillLevel);
            passiveLevels[indexOf] = newLevel.ToString();
        }
    }
    public void SetPassiveSkills(List<string> newSkills)
    {
        passiveSkills = new List<string>(newSkills);
        if (passiveSkills.Count == 0) { return; }
        for (int i = passiveSkills.Count - 1; i >= 0; i--)
        {
            if (passiveSkills[i].Length <= 1) { passiveSkills.RemoveAt(i); }
        }
    }
    public List<string> GetPassiveSkills()
    {
        return passiveSkills;
    }
    public List<string> GetPassiveSkillsAndLevels()
    {
        List<string> passivesAndLevels = new List<string>();
        for (int i = 0; i < passiveSkills.Count; i++)
        {
            passivesAndLevels.Add(passiveSkills[i] + " - " + passiveLevels[i]);
        }
        return passivesAndLevels;
    }
    public string GetPassiveString()
    {
        if (passiveSkills.Count == 0) { return ""; }
        return String.Join(passiveDelimiter, passiveSkills);
    }
    public string GetPassiveAtIndex(int index)
    {
        if (index < 0 || index >= passiveSkills.Count){ return ""; }
        return passiveSkills[index];
    }
    public List<string> passiveLevels;
    public void SetPassiveLevels(List<string> newLevels)
    {
        passiveLevels = new List<string>(newLevels);
        if (passiveLevels.Count == 0) { return; }
        for (int i = passiveLevels.Count - 1; i >= 0; i--)
        {
            if (passiveLevels[i].Length < 1) { passiveLevels.RemoveAt(i); }
        }
    }
    public List<string> GetPassiveLevels()
    {
        return passiveLevels;
    }
    public string GetPassiveLevelString()
    {
        if (passiveLevels.Count == 0) { return ""; }
        return String.Join(passiveDelimiter, passiveLevels);
    }
    public int GetLevelFromPassive(string passiveName)
    {
        int indexOf = passiveSkills.IndexOf(passiveName);
        if (indexOf == -1) { return 0; }
        return int.Parse(passiveLevels[indexOf]);
    }
    public void SetLevelOfPassive(string passiveName, int newLevel)
    {
        int indexOf = passiveSkills.IndexOf(passiveName);
        if (indexOf == -1) { return; }
        passiveLevels[indexOf] = newLevel.ToString();
    }
    public int GetTotalPassiveLevels()
    {
        int total = 0;
        for (int i = 0; i < passiveLevels.Count; i++)
        {
            total += int.Parse(passiveLevels[i]);
        }
        total += customPassives.Count;
        return total;
    }
    public int GetTotalPassiveLevelsOfPassiveGroup(List<string> passiveGroup)
    {
        int total = 0;
        for (int i = 0; i < passiveGroup.Count; i++)
        {
            total += GetLevelFromPassive(passiveGroup[i]);
        }
        return total;
    }
    public void RemoveMaxLevelPassives(List<string> passiveNames, List<string> passiveMaxLevels)
    {
        for (int i = passiveNames.Count - 1; i >= 0; i--)
        {
            if (GetLevelFromPassive(passiveNames[i]) >= int.Parse(passiveMaxLevels[i]))
            {
                passiveNames.RemoveAt(i);
                passiveMaxLevels.RemoveAt(i);
            }
        }
    }
    // Temporary Passives Are Always Level 1.
    public List<string> tempPassives;
    public List<int> tempPassiveDurations;
    // Bool to know if you need to add it to the passive list or just extend the duraction.
    public bool AddTempPassive(string passive, int duration)
    {
        int indexOf = tempPassives.IndexOf(passive);
        if (indexOf == -1)
        {
            tempPassives.Add(passive);
            tempPassiveDurations.Add(duration);
            return true;
        }
        else
        {
            tempPassiveDurations[indexOf] += duration;
            return false;
        }
    }
    // If any temp passives expire then reorganize the passives.
    public List<string> DecreaseTempPassiveDurations()
    {
        List<string> removed = new List<string>();
        for (int i = tempPassiveDurations.Count - 1; i >= 0; i--)
        {
            tempPassiveDurations[i] -= 1;
            if (tempPassiveDurations[i] == 0)
            {
                removed.Add(tempPassives[i]);
                tempPassiveDurations.RemoveAt(i);
                tempPassives.RemoveAt(i);
            }
        }
        return removed;
    }
    public void AddSortedPassive(string passiveName, string type)
    {
        switch (type)
        {
            case "Moving":
                movingPassives.Add(passiveName);
                break;
            case "Start":
                startTurnPassives.Add(passiveName);
                break;
            case "End":
                endTurnPassives.Add(passiveName);
                break;
            case "Attack":
                attackingPassives.Add(passiveName);
                break;
            case "Defend":
                defendingPassives.Add(passiveName);
                break;
            case "BS":
                startBattlePassives.Add(passiveName);
                break;
            case "TakeDamage":
                takeDamagePassives.Add(passiveName);
                break;
            case "AfterAttack":
                afterAttackPassives.Add(passiveName);
                break;
            case "AfterDefend":
                afterDefendPassives.Add(passiveName);
                break;
            case "OOC":
                outOfCombatPassives.Add(passiveName);
                break;
            case "AdjustActives":
                adjustActivesPassives.Add(passiveName);
                break;
            case "AdjustSpells":
                adjustSpellsPassives.Add(passiveName);
                break;
            case "AfterSkill":
                afterSkillPassives.Add(passiveName);
                break;
            case "AfterSpell":
                afterSpellPassives.Add(passiveName);
                break;
        }
    }
    public void RemoveSortedPassive(string passiveName, string type)
    {
        switch (type)
        {
            case "Moving":
                movingPassives.Remove(passiveName);
                break;
            case "Start":
                startTurnPassives.Remove(passiveName);
                break;
            case "End":
                endTurnPassives.Remove(passiveName);
                break;
            case "Attack":
                attackingPassives.Remove(passiveName);
                break;
            case "Defend":
                defendingPassives.Remove(passiveName);
                break;
            case "BS":
                startBattlePassives.Remove(passiveName);
                break;
            case "TakeDamage":
                takeDamagePassives.Remove(passiveName);
                break;
            case "AfterAttack":
                afterAttackPassives.Remove(passiveName);
                break;
            case "AfterDefend":
                afterDefendPassives.Remove(passiveName);
                break;
            case "OOC":
                outOfCombatPassives.Remove(passiveName);
                break;
            case "AdjustActives":
                adjustActivesPassives.Remove(passiveName);
                break;
            case "AdjustSpells":
                adjustSpellsPassives.Remove(passiveName);
                break;
            case "AfterSkill":
                afterSkillPassives.Remove(passiveName);
                break;
            case "AfterSpell":
                afterSpellPassives.Remove(passiveName);
                break;
        }
    }
    public List<string> startBattlePassives;
    public List<string> GetStartBattlePassives() { return startBattlePassives; }
    public void AddStartBattlePassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1) { continue; }
            startBattlePassives.Add(newSkills[i]);
        }
    }
    public void SetStartBattlePassives(List<string> passives)
    {
        startBattlePassives = new List<string>(passives);
    }
    public List<string> startTurnPassives;
    public List<string> GetStartTurnPassives()
    {
        return new List<string>(startTurnPassives);
    }
    public void AddStartTurnPassive(string passiveName) { startTurnPassives.Add(passiveName); }
    public void AddStartTurnPassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1) { continue; }
            AddStartTurnPassive(newSkills[i]);
        }
    }
    public void SetStartTurnPassives(List<string> passives) { startTurnPassives = new List<string>(passives); }
    public List<string> endTurnPassives;
    public List<string> GetEndTurnPassives()
    {
        return new List<string>(endTurnPassives);
    }
    public void AddEndTurnPassive(string passiveName) { endTurnPassives.Add(passiveName); }
    public void AddEndTurnPassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1) { continue; }
            AddEndTurnPassive(newSkills[i]);
        }
    }
    public void SetEndTurnPassives(List<string> passives) { endTurnPassives = new List<string>(passives); }
    public List<string> attackingPassives;
    public virtual List<string> GetAttackingPassives() { return attackingPassives; }
    public void AddAttackingPassive(string passiveName) { attackingPassives.Add(passiveName); }
    public void AddAttackingPassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1) { continue; }
            AddAttackingPassive(newSkills[i]);
        }
    }
    public void SetAttackingPassives(List<string> passives) { attackingPassives = new List<string>(passives); }
    public List<string> defendingPassives;
    public virtual List<string> GetDefendingPassives() { return defendingPassives; }
    public void AddDefendingPassive(string passiveName) { defendingPassives.Add(passiveName); }
    public void AddDefendingPassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1) { continue; }
            AddDefendingPassive(newSkills[i]);
        }
    }
    public void SetDefendingPassives(List<string> passives) { defendingPassives = new List<string>(passives); }
    public List<string> takeDamagePassives;
    public List<string> GetTakeDamagePassives() { return takeDamagePassives; }
    public void AddTakeDamagePassive(string passiveName) { takeDamagePassives.Add(passiveName); }
    public void AddTakeDamagePassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1) { continue; }
            AddTakeDamagePassive(newSkills[i]);
        }
    }
    public void SetTakeDamagePassives(List<string> passives) { takeDamagePassives = new List<string>(passives); }
    public List<string> movingPassives;
    public List<string> GetMovingPassives() { return movingPassives; }
    public void AddMovingPassive(string passiveName) { movingPassives.Add(passiveName); }
    public void AddMovingPassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1) { continue; }
            AddMovingPassive(newSkills[i]);
        }
    }
    public void SetMovingPassives(List<string> passives) { movingPassives = new List<string>(passives); }
    public List<string> deathActives;
    public bool deathActivesActive = true;
    public void DisableDeathActives(){deathActivesActive = false;}
    public List<string> GetDeathActives()
    {
        if (!deathActivesActive){return new List<string>();}
        return deathActives;
    }
    public string GetDeathActivesString()
    {
        return String.Join(",", deathActives);
    }
    public void SetDeathActives(List<string> newInfo)
    {
        deathActives = new List<string>(newInfo);
        for (int i = deathActives.Count - 1; i >= 0; i--)
        {
            if (deathActives[i].Length < 3)
            {
                deathActives.RemoveAt(i);
            }
        }
    }
    public List<string> afterAttackPassives;
    public List<string> GetAfterAttackPassives()
    {
        return afterAttackPassives;
    }
    public void AddAfterAttackPassive(string passiveName)
    {
        afterAttackPassives.Add(passiveName);
    }
    public void AddAfterAttackPassives(List<string> newPassives)
    {
        for (int i = 0; i < newPassives.Count; i++)
        {
            if (newPassives[i].Length <= 1) { continue; }
            AddAfterAttackPassive(newPassives[i]);
        }
    }
    public void SetAfterAttackPassives(List<string> passives)
    {
        afterAttackPassives = new List<string>(passives);
    }
    public List<string> afterDefendPassives;
    public List<string> GetAfterDefendPassives()
    {
        return afterDefendPassives;
    }
    public void AddAfterDefendPassive(string passiveName)
    {
        afterDefendPassives.Add(passiveName);
    }
    public void AddAfterDefendPassives(List<string> newPassives)
    {
        for (int i = 0; i < newPassives.Count; i++)
        {
            if (newPassives[i].Length <= 1) { continue; }
            AddAfterDefendPassive(newPassives[i]);
        }
    }
    public void SetAfterDefendPassives(List<string> passives)
    {
        afterDefendPassives = new List<string>(passives);
    }
    public List<string> outOfCombatPassives;
    public List<string> GetOOCPassives(){return outOfCombatPassives;}
    public void AddOOCPassive(string passiveName){outOfCombatPassives.Add(passiveName);}
    public void AddOOCPassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1){continue;}
            AddOOCPassive(newSkills[i]);
        }
    }
    public void SetOOCPassives(List<string> passives){outOfCombatPassives = new List<string>(passives);}
    public List<string> adjustActivesPassives;
    public List<string> GetAdjustActivesPassives() { return adjustActivesPassives; }
    public void AddAdjustActivesPassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1) { continue; }
            adjustActivesPassives.Add(newSkills[i]);
        }
    }
    public void SetAdjustActivesPassives(List<string> passives)
    {
        adjustActivesPassives = new List<string>(passives);
    }
    public List<string> adjustSpellsPassives;
    public List<string> GetAdjustSpellsPassives() { return adjustSpellsPassives; }
    public void AddAdjustSpellsPassives(List<string> newSkills)
    {
        for (int i = 0; i < newSkills.Count; i++)
        {
            if (newSkills[i].Length <= 1) { continue; }
            adjustSpellsPassives.Add(newSkills[i]);
        }
    }
    public void SetAdjustSpellsPassives(List<string> passives)
    {
        adjustSpellsPassives = new List<string>(passives);
    }
    public List<string> afterSkillPassives;
    public List<string> GetAfterSkillPassives()
    {
        return afterSkillPassives;
    }
    public void AddAfterSkillPassives(List<string> newPassives)
    {
        for (int i = 0; i < newPassives.Count; i++)
        {
            if (newPassives[i].Length <= 1) { continue; }
            afterSkillPassives.Add(newPassives[i]);
        }
    }
    public void SetAfterSkillPassives(List<string> passives)
    {
        afterSkillPassives = new List<string>(passives);
    }
    public List<string> afterSpellPassives;
    public List<string> GetAfterSpellPassives()
    {
        return afterSpellPassives;
    }
    public void AddAfterSpellPassives(List<string> newPassives)
    {
        for (int i = 0; i < newPassives.Count; i++)
        {
            if (newPassives[i].Length <= 1) { continue; }
            afterSpellPassives.Add(newPassives[i]);
        }
    }
    public void SetAfterSpellPassives(List<string> passives)
    {
        afterSpellPassives = new List<string>(passives);
    }
}
