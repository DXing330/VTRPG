using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MysteryDungeonItemTester : MonoBehaviour
{
    public MysteryDungeonItemEffect itemEffect;
    public MysteryDungeonItemDetailSwitch itemDescriptions;
    [ContextMenu("Test All Item Descriptions")]
    public void TestAllItemDescriptions()
    {
        List<string> allItems = allItemData.GetAllKeys();
        string allDes = "";
        for (int i = 0; i < allItems.Count; i++)
        {
            allDes += allItems[i] + ":" + itemDescriptions.ReturnItemDescriptionFromName(allItems[i]);
            if (i < allItems.Count - 1){allDes += "\n";}
        }
        Debug.Log(allDes);
    }
    public MysteryDungeonManager generalManager;
    public MysteryDungeonBattleMap map;
    public PartyDataManager partyData;
    public StatDatabase allItemData;
    public MysteryDungeonUIManager UI;
    // Should Be Reusable And Later Moved To Another Class.
    [ContextMenu("Initialize MD")]
    public void InitializeMysteryDungeon()
    {
        generalManager.InitializeMysteryDungeon();
    }
    public string testItemName;
    [ContextMenu("Test MD Item")]
    public void TestMysteryDungeonItem()
    {
        map.UpdateMap();
        TacticActor leader = map.GetPartyLeader();
        itemEffect.UseItem(leader, map, testItemName, partyData);
        map.RefreshPlayerLocation();
        map.UpdateMap();
        UI.ChangeState("None");
    }
    public string testTrapName;
    [ContextMenu("Test MD Trap")]
    public void TestMysteryDungeonTrap()
    {
        map.UpdateMap();
        TacticActor leader = map.GetPartyLeader();
        itemEffect.TriggerTrap(leader, map, partyData, testTrapName);
        map.RefreshPlayerLocation();
        map.UpdateMap();
        UI.ChangeState("None");
    }
}
