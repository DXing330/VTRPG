using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Barracks", menuName = "ScriptableObjects/DataContainers/SavedData/Barracks", order = 1)]
public class BarracksData : PartyData
{
    public void AddFromParty(int index, PartyDataManager partyDataManager)
    {
        List<string> allStats = partyDataManager.mainPartyData.GetStatsAtIndex(index);
        AddToBarracks(allStats[0],allStats[1],allStats[2],allStats[3]);
        partyDataManager.mainPartyData.RemoveStatsAtIndex(index);
        partyDataManager.Save();
        Save();
    }
    protected void AddToBarracks(string personalName, string ID, string baseStats, string equipment)
    {
        partyNames.Add(personalName);
        partyIDs.Add(ID);
        partyStats.Add(baseStats);
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

    public void AddFromBarracks(int index, PartyDataManager partyDataManager)
    {
        partyDataManager.mainPartyData.AddAllStats(partyNames[index], partyIDs[index], partyStats[index], partyEquipment[index]);
        RemoveStatsAtIndex(index);
        partyDataManager.Save();
        Save();
    }
}
