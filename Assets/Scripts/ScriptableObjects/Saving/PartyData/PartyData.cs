using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PartyData", menuName = "ScriptableObjects/DataContainers/SavedData/PartyData", order = 1)]
public class PartyData : SavedData
{
    public int restHealth;
    public string exhaustStatus;
    public int exhaustDamage;
    public string hungerStatus;
    public TacticActor dummyActor;
    public string delimiterTwo;
    public List<string> partyNames;
    public int PartyCount() { return partyNames.Count; }
    public TacticActor ReturnActorAtIndex(int index)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        dummyActor.SetPersonalName(partyNames[index]);
        return dummyActor;
    }
    public void ChangeName(string newName, int index)
    {
        partyNames[index] = newName;
        Save();
    }
    public List<string> GetNames(){return partyNames;}
    public string GetNameAtIndex(int index)
    {
        return partyNames[index];
    }
    public List<string> partyIDs;
    public TacticActor ReturnActorFromID(int ID)
    {
        int indexOf = partyIDs.IndexOf(ID.ToString());
        if (indexOf < 0){return null;}
        return ReturnActorAtIndex(indexOf);
    }
    public int GetIDAtIndex(int index)
    {
        return int.Parse(partyIDs[index]);
    }
    public List<string> GetPartyIDs(){return partyIDs;}
    public void SetPartyIDs(List<string> newIDs){partyIDs = newIDs;}
    public int GetMatchingIDIndex(string ID)
    {
        return partyIDs.IndexOf(ID);
    }
    public List<string> GetSpriteNames()
    {
        List<string> partySpriteNames = new List<string>();
        for (int i = 0; i < partyStats.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(partyStats[i]);
            partySpriteNames.Add(dummyActor.GetSpriteName());
        } 
        return partySpriteNames;
    }
    public string GetSpriteNameAtIndex(int index)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        return dummyActor.GetSpriteName();
    }
    public List<string> partyStats;
    public List<string> GetBaseStats() { return partyStats; }
    public string GetMemberStatsAtIndex(int index)
    {
        return partyStats[index];
    }
    public int GetCurrentHealthAtIndex(int index)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        return dummyActor.GetHealth();
    }
    public void ChangeBaseStats(string newStats, int index)
    {
        partyStats[index] = newStats;
    }
    // Equipment goes here?
    public List<string> partyEquipment;
    public string GetEquipmentAtIndex(int index)
    {
        if (index < 0 || index >= partyEquipment.Count){return "";}
        return partyEquipment[index];
    }
    public void SetEquipmentAtIndex(string equip, int index)
    {
        if (index < 0 || index >= partyEquipment.Count){return;}
        partyEquipment[index] = equip;
    }
    // This is not needed, we can store everything in the stats.
    //public List<string> partyCurrentStats;
    public void SetCurrentStats(string newStats, int index)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        string[] newCurrentStats = newStats.Split("|");
        dummyActor.SetCurrentHealth(utility.SafeParseInt(newCurrentStats[0]));
        dummyActor.SetCurses(newCurrentStats[1]);
        dummyActor.SetCurrentMana(utility.SafeParseInt(newCurrentStats[2]));
        partyStats[index] = dummyActor.GetInitialStats();
        UpdateDefeatedMemberTracker(index);
    }
    public void ClearAllStats()
    {
        partyNames.Clear();
        partyStats.Clear();
        partyEquipment.Clear();
    }
    public List<string> GetStatsAtIndex(int index)
    {
        List<string> allStats = new List<string>();
        allStats.Add(partyNames[index]);
        allStats.Add(partyIDs[index]);
        allStats.Add(partyStats[index]);
        allStats.Add(partyEquipment[index]);
        return allStats;
    }
    public void RemoveStatsAtIndex(int index)
    {
        partyNames.RemoveAt(index);
        partyIDs.RemoveAt(index);
        partyStats.RemoveAt(index);
        partyEquipment.RemoveAt(index);
    }
    // Acts as a full restore.
    public void ClearCurrentStats()
    {
        for (int i = 0; i < partyStats.Count; i++)
        {
            // Load the actor.
            dummyActor.SetInitialStatsFromString(partyStats[i]);
            // Remove any statuses.
            // Reset current health.
            dummyActor.FullRestore();
            // Save back the actor.
            partyStats[i] = dummyActor.GetInitialStats();
        }
    }
    public void HalfRestore()
    {
        for (int i = 0; i < partyStats.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(partyStats[i]);
            dummyActor.HalfRestore();
            partyStats[i] = dummyActor.GetInitialStats();
        }
    }
    public void ReviveDefeatedMembers(bool all = false)
    {
        for (int i = 0; i < partyStats.Count; i++)
        {
            if (all)
            {
                dummyActor.SetInitialStatsFromString(partyStats[i]);
                // Set health to 1.
                dummyActor.NearDeath();
                // Save back the actor.
                partyStats[i] = dummyActor.GetInitialStats();
            }
            // Don't keep track of empty members.
            else if (defeatedMemberTracker[i])
            {
                // Load the actor.
                dummyActor.SetInitialStatsFromString(partyStats[i]);
                // Set health to 1.
                dummyActor.NearDeath();
                // Save back the actor.
                partyStats[i] = dummyActor.GetInitialStats();
            }
        }
    }
    public List<bool> defeatedMemberTracker;
    public void ResetDefeatedMemberTracker()
    {
        defeatedMemberTracker = new List<bool>();
        for (int i = 0; i < partyStats.Count; i++)
        {
            defeatedMemberTracker.Add(true);
        }
    }
    public void UpdateDefeatedMemberTracker(int index)
    {
        defeatedMemberTracker[index] = false;
    }
    public StatDatabase allInjuries;
    // Try to add a new injury three times, if all fail then die.
    public bool AddInjury(int index, int maxRolls = 3)
    {
        // Generate a random injury.
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        string randomInjury = allInjuries.ReturnRandomKey();
        int maxInjuryLevel = int.Parse(allInjuries.ReturnValue(randomInjury));
        // Check if the actor already has the max level of that injury.
        if (dummyActor.GetLevelFromPassive(randomInjury) >= maxInjuryLevel)
        {
            // Else return with one less try.
            if (maxRolls > 0)
            {
                return AddInjury(index, maxRolls - 1);
            }
            // If no more tries then die.
            else
            {
                return false;
            }
        }
        // If not then add it and return true.
        else
        {
            dummyActor.AddPassiveSkill(randomInjury, "1");
            partyStats[index] = dummyActor.GetInitialStats();
            return true;
        }
    }
    public void RemoveDefeatedMembers()
    {
        if (partyStats.Count <= 0){ return; }
        for (int i = partyStats.Count - 1; i >= 0; i--)
        {
            if (defeatedMemberTracker[i])
            {
                if (!AddInjury(i))
                {
                    RemoveStatsAtIndex(i);
                }
                else
                {
                    dummyActor.SetInitialStatsFromString(partyStats[i]);
                    dummyActor.NearDeath();
                    partyStats[i] = dummyActor.GetInitialStats();
                }
            }
        }
    }
    public void RemoveDeadMembers()
    {
        for (int i = partyStats.Count - 1; i >= 0; i--)
        {
            dummyActor.SetInitialStatsFromString(partyStats[i]);
            if (dummyActor.GetHealth() <= 0)
            {
                if (!AddInjury(i))
                {
                    RemoveStatsAtIndex(i);
                }
                else
                {
                    dummyActor.NearDeath();
                    partyStats[i] = dummyActor.GetInitialStats();
                }
            }
        }
    }
    public void ResetCurrentStats(bool defeated = false)
    {
        // Heal to full and remove status effects.
        if (!defeated)
        {
            ClearCurrentStats();
        }
        if (defeated)
        {
            // Set HP to 1.
            ReviveDefeatedMembers(true);
        }
    }
    public void ResetData()
    {
        partyNames.Clear();
        partyIDs.Clear();
        partyStats.Clear();
        partyEquipment.Clear();
    }
    public override void Save()
    {
        partyNames = utility.RemoveEmptyListItems(partyNames);
        partyIDs = utility.RemoveEmptyListItems(partyIDs);
        partyStats = utility.RemoveEmptyListItems(partyStats);
        dataPath = Application.persistentDataPath + "/" + filename;
        allData = "";
        allData += "Names=" + String.Join(delimiterTwo, partyNames) + delimiter;
        allData += "ID=" + String.Join(delimiterTwo, partyIDs) + delimiter;
        allData += "Stats=" + String.Join(delimiterTwo, partyStats) + delimiter;
        allData += "Equip=" + String.Join(delimiterTwo, partyEquipment) + delimiter;
        File.WriteAllText(dataPath, allData);
    }
    public override void NewGame()
    {
        ResetData();
        Save();
        allData = newGameData;
        if (allData.Contains(delimiter)) { dataList = allData.Split(delimiter).ToList(); }
        else { return; }
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
        Save();
    }
    public override void Load()
    {
        dataPath = Application.persistentDataPath + "/" + filename;
        if (File.Exists(dataPath)) { allData = File.ReadAllText(dataPath); }
        else { allData = newGameData; }
        if (allData.Contains(delimiter)) { dataList = allData.Split(delimiter).ToList(); }
        else { return; }
        for (int i = 0; i < dataList.Count; i++)
        {
            LoadStat(dataList[i]);
        }
        partyNames = utility.RemoveEmptyListItems(partyNames);
        partyIDs = utility.RemoveEmptyListItems(partyIDs);
        partyStats = utility.RemoveEmptyListItems(partyStats);
    }
    protected void LoadStat(string stat)
    {
        string[] blocks = stat.Split("=");
        if (blocks.Length < 2){return;}
        string data = blocks[1];
        switch (blocks[0])
        {
            default:
            break;
            case "Names":
            partyNames = data.Split(delimiterTwo).ToList();
            break;
            case "ID":
            SetPartyIDs(data.Split(delimiterTwo).ToList());
            break;
            case "Stats":
            partyStats = data.Split(delimiterTwo).ToList();
            break;
            case "Equip":
            partyEquipment = data.Split(delimiterTwo).ToList();
            break;
        }
    }
    public List<string> GetStats(string joiner = "|")
    {
        return partyStats;
    }
    public List<string> GetEquipmentStats()
    {
        return partyEquipment;
    }
    // Give back the old equipment.
    public string EquipToMember(string equip, int memberIndex, Equipment dummyEquip)
    {
        List<string> currentEquipment = partyEquipment[memberIndex].Split("@").ToList();
        dummyEquip.SetAllStats(equip);
        string newSlot = dummyEquip.GetSlot();
        string oldEquip = "";
        for (int i = 0; i < currentEquipment.Count; i++)
        {
            if (currentEquipment[i].Length < 6) { continue; }
            dummyEquip.SetAllStats(currentEquipment[i]);
            string oldSlot = dummyEquip.GetSlot();
            if (oldSlot == newSlot)
            {
                oldEquip = currentEquipment[i];
                currentEquipment.RemoveAt(i);
                break;
            }
        }
        partyEquipment[memberIndex] = equip + "@";
        for (int i = 0; i < currentEquipment.Count; i++)
        {
            partyEquipment[memberIndex] += currentEquipment[i];
            if (i < currentEquipment.Count - 1)
            {
                partyEquipment[memberIndex] += "@";
            }
        }
        return oldEquip;
    }
    public string UnequipFromMember(int memberIndex, string slot, Equipment dummyEquip)
    {
        string oldEquip = "";
        List<string> currentEquipment = partyEquipment[memberIndex].Split("@").ToList();
        for (int i = 0; i < currentEquipment.Count; i++)
        {
            if (currentEquipment[i].Length < 6) { continue; }
            dummyEquip.SetAllStats(currentEquipment[i]);
            if (slot == dummyEquip.GetSlot())
            {
                oldEquip = currentEquipment[i];
                currentEquipment.RemoveAt(i);
                break;
            }
        }
        partyEquipment[memberIndex] = "";
        for (int i = 0; i < currentEquipment.Count; i++)
        {
            partyEquipment[memberIndex] += currentEquipment[i];
            if (i < currentEquipment.Count - 1)
            {
                partyEquipment[memberIndex] += "@";
            }
        }
        return oldEquip;
    }
    public void MemberLearnsSpell(string newSpell, int index)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        dummyActor.LearnSpell(newSpell);
        partyStats[index] = dummyActor.GetInitialStats();
    }
    public void SetMemberStats(TacticActor newDummy, int index)
    {
        partyStats[index] = newDummy.GetInitialStats();
    }
    public bool PartyMemberIncluded(string memberName)
    {
        return (partyNames.Contains(memberName));
    }
    public int PartyMemberIndex(string memberName)
    {
        int indexOf = partyNames.IndexOf(memberName);
        return indexOf;
    }
    public void AddMember(string stats, string personalName, string ID)
    {
        partyStats.Add(stats);
        partyIDs.Add(ID);
        partyNames.Add(personalName);
        partyEquipment.Add("");
    }
    public bool MemberExists(string spriteName)
    {
        for (int i = 0; i < partyStats.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(partyStats[i]);
            if (dummyActor.GetSpriteName() == spriteName){return true;}
        }
        return false;
    }
    public void RemoveMember(string spriteName)
    {
        for (int i = 0; i < partyStats.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(partyStats[i]);
            if (dummyActor.GetSpriteName() == spriteName)
            {
                RemoveStatsAtIndex(i);
                break;
            }
        }
    }
    public void AddAllStats(string personalName, string ID, string baseStats, string equipment)
    {
        partyStats.Add(baseStats);
        partyIDs.Add(ID);
        partyNames.Add(personalName);
        // If equipment is empty this could cause an issue.
        if (partyEquipment.Count >= partyNames.Count)
        {
            partyEquipment[partyNames.Count - 1] = equipment;
        }
        else
        {
            partyEquipment.Add(equipment);
        }
    }
    public void RemoveExhaustion(int index)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        dummyActor.ClearStatuses(exhaustStatus);
        partyStats[index] = dummyActor.GetInitialStats();
    }
    public void Rest(int index, bool eat = true)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        if (eat)
        {
            dummyActor.UpdateHealth(restHealth, false);
            dummyActor.ClearStatuses(hungerStatus);
        }
        dummyActor.ClearStatuses(exhaustStatus);
        partyStats[index] = dummyActor.GetInitialStats();
    }
    public int Hunger(int index)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        dummyActor.AddStatus(hungerStatus, -1);
        partyStats[index] = dummyActor.GetInitialStats();
        int count = 0;
        for (int i = 0; i < dummyActor.statuses.Count; i++)
        {
            if (dummyActor.statuses[i] == hungerStatus) { count++; }
        }
        return count;
    }
    public void NaturalRegeneration(List<string> regenPassives)
    {
        for (int i = 0; i < partyStats.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(partyStats[i]);
            if (dummyActor.AnyPassiveExists(regenPassives))
            {
                dummyActor.UpdateHealth(1, false);
                partyStats[i] = dummyActor.GetInitialStats();
            }
        }
    }
    // By hunger/etc.
    public bool HungerChipDamage(int index, bool death = false)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        dummyActor.UpdateHealth(1);
        if (dummyActor.GetHealth() <= 0)
        {
            if (death)
            {
                RemoveStatsAtIndex(index);
                return false;
            }
            else
            {
                return true;
            }
        }
        partyStats[index] = dummyActor.GetInitialStats();
        return false;
    }
    public bool StatusChipDamage(List<string> statuses, bool death = false)
    {
        for (int i = 0; i < partyStats.Count; i++)
        {
            dummyActor.SetInitialStatsFromString(partyStats[i]);
            if (dummyActor.AnyStatusExists(statuses))
            {
                dummyActor.UpdateHealth(1);
                if (dummyActor.GetHealth() <= 0)
                {
                    if (death)
                    {
                        RemoveStatsAtIndex(i);
                        return false;
                    }
                    else
                    {
                        // If you can't die then just quit the dungeon losing all gold and items.
                        return true;
                    }
                }
            }
            partyStats[i] = dummyActor.GetInitialStats();
        }
        return false;
    }
    public void Exhaust(int index, bool death = false)
    {
        dummyActor.SetInitialStatsFromString(partyStats[index]);
        if (dummyActor.statuses.Contains(exhaustStatus))
        {
            dummyActor.UpdateHealth(exhaustDamage, true);
            // Exhaust won't kill you?
            if (dummyActor.GetHealth() <= 0)
            {
                if (!death)
                {
                    dummyActor.SetCurrentHealth(1);
                }
                else
                {
                    RemoveStatsAtIndex(index);
                    return;
                }
            }
        }
        else
        {
            dummyActor.AddStatus(exhaustStatus, -1);
        }
        partyStats[index] = dummyActor.GetInitialStats();
    }
}
