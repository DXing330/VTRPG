using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MysteryDungeonQuest
{
    protected string questDelimiter = "questDelim";
    public string questLocation;
    public int questActive = 0;
    public bool QuestActive(){return questActive != 0;}
    public bool QuestLocation(string location){return questLocation == location;}
    public int questFloor;
    public bool QuestFloor(int floor){return questFloor == floor;}
    public string questGoal; // Defeat, Escort, Rescue
    public string GetRandomGoal()
    {
        int roll = Random.Range(0, 3);
        switch (roll)
        {
            default:
            return "Defeat";
            case 1:
            return "Escort";
            case 2:
            return "Rescue";
        }
    }
    public string goalSpecifics;
    public int completed = 0;
    // Called At End Of Dungeon.
    public void CheckCompletion(PartyDataManager partyData)
    {
        if (completed == 1 || !QuestActive()){return;}
        switch (questGoal)
        {
            default:
            break;
            case "Escort":
            if (partyData.TempPartyMemberExists(goalSpecifics)){completed = 1;}
            break;
        }
    }
    // Called After Every Floor
    public void CheckCompletion(int floor, MysteryDungeonBattleMap map)
    {
        if (completed == 1 || !QuestActive()){return;}
        if (!QuestFloor(floor)){return;}
        switch (questGoal)
        {
            default:
            break;
            case "Defeat":
            if (map.DefeatedActorExists(goalSpecifics))
            {
                completed = 1;
            }
            break;
            case "Rescue":
            if (map.EscapedActorExists(goalSpecifics))
            {
                completed = 1;
            }
            break;
        }
    }
    public string reward;
    public int rewardQuantity;
    public void InitializeQuest(string newLoc, int newFloor, string newGoal, string newSpecifics, string newReward, int newQty)
    {
        questLocation = newLoc;
        questFloor = newFloor;
        questGoal = newGoal;
        goalSpecifics = newSpecifics;
        reward = newReward;
        rewardQuantity = newQty;
        completed = 0;
    }
    public string ReturnQuestAsString()
    {
        string questString = "";
        questString += questLocation + questDelimiter;
        questString += questFloor.ToString() + questDelimiter;
        questString += questGoal + questDelimiter;
        questString += goalSpecifics + questDelimiter;
        questString += reward + questDelimiter;
        questString += rewardQuantity.ToString() + questDelimiter;
        questString += completed.ToString() + questDelimiter;
        questString += questActive + questDelimiter;
        return questString;
    }
    public bool LoadQuestFromString(string newInfo)
    {
        string[] questDetails = newInfo.Split(new string[] { questDelimiter }, System.StringSplitOptions.None);
        if (questDetails.Length < 8)
        {
            Debug.Log("Invalid MysteryDungeonQuest data: " + newInfo);
            return false;
        }
        questLocation = questDetails[0];
        int.TryParse(questDetails[1], out questFloor);
        questGoal = questDetails[2];
        goalSpecifics = questDetails[3];
        reward = questDetails[4];
        int.TryParse(questDetails[5], out rewardQuantity);
        int.TryParse(questDetails[6], out completed);
        int.TryParse(questDetails[7], out questActive);
        return true;
    }
    public void StartDungeon(PartyDataManager partyData, string location)
    {
        if (completed == 1){return;}
        questActive = QuestLocation(location) ? 1 : 0;
        if (!QuestActive()){return;}
        switch (questGoal)
        {
            default:
            break;
            case "Escort":
            partyData.AddTempPartyMember(goalSpecifics);
            break;
        }
    }
    public void EnterFloor(int floor, MysteryDungeonBattleMap map)
    {
        if (completed == 1 || !QuestActive()){return;}
        if (!QuestFloor(floor)){return;}
        int spawnLocation = map.GenerateEnemyLocation();
        Debug.Log("Spawning " + goalSpecifics + " at " + spawnLocation);
        switch (questGoal)
        {
            default:
            break;
            case "Defeat":
            map.SpawnAndAddActor(spawnLocation, goalSpecifics, 1);
            map.AddStartingEnemy(goalSpecifics, spawnLocation);
            break;
            case "Rescue":
            map.AddNPC(spawnLocation, goalSpecifics);
            break;
        }
    }
}
