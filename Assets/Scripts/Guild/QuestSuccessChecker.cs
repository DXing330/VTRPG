using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestSuccessChecker : MonoBehaviour
{
    public bool StoryQuestSuccessful(PartyDataManager partyData, Dungeon dungeon)
    {
        if (!dungeon.MainStoryQuest()){return false;}
        switch (dungeon.mainStory.GetCurrentRequest())
        {
            case "Complete":
            if (dungeon.escaped){return false;}
            return true;
            case "Capture":
            string captureRequirement = dungeon.mainStory.GetRequestSpecifics().Split("*")[0];
            if (partyData.tempPartyData.PartyMemberIncluded(captureRequirement))
            {
                partyData.RemoveTempPartyMember(captureRequirement);
                return true;
            }
            return false;
            case "Deliver":
            // Check if your inventory includes the quest item.
            if (partyData.dungeonBag.ItemExists("Check"))
            {
                partyData.dungeonBag.UseItem("Check");
                return true;
            }
            return false;
        }
        return CheckGoal(partyData, dungeon, dungeon.mainStory.GetCurrentRequest(), true);
    }
    
    public bool QuestSuccessful(PartyDataManager partyData, int index, Dungeon dungeon)
    {
        string goal = partyData.guildCard.GetQuestGoals()[index];
        return CheckGoal(partyData, dungeon, goal);
    }

    protected bool CheckGoal(PartyDataManager partyData, Dungeon dungeon, string goal, bool main = false)
    {
        string questItem = dungeon.GetSearchName();
        string escortName = dungeon.GetEscortName();
        if (main)
        {
            questItem = dungeon.mainStory.GetRequestSpecifics().Split("*")[0];
            escortName = dungeon.mainStory.GetRequestSpecifics().Split("*")[0];
        }
        switch (goal)
        {
            default:
            return false;
            case "Search":
            // Check if your inventory includes the quest item.
            if (partyData.dungeonBag.ItemExists(questItem))
            {
                partyData.dungeonBag.UseItem(questItem);
                return true;
            }
            return false;
            case "Escort":
            if (dungeon.escaped){return false;}
            if (partyData.tempPartyData.PartyMemberIncluded(escortName))
            {
                partyData.RemoveTempPartyMember(escortName);
                return true;
            }
            return false;
            case "Rescue":
            if (partyData.tempPartyData.PartyMemberIncluded(escortName))
            {
                partyData.RemoveTempPartyMember(escortName);
                return true;
            }
            return false;
        }
    }
}
