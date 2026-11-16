using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls The Scene Flow + PartyData.
public class MysteryDungeonManager : MonoBehaviour
{
    void Start()
    {
        InitializeMysteryDungeon();
    }
    public void InitializeMysteryDungeon()
    {
        // Load Party.
        partyData.Load();
        // Load Map, If Floor One Then You Need To Generate.
        if (!state.LoadToMysteryDungeonMap(map))
        {
            GenerateDungeon();
        }
        // Load Battle (Loads Persistent Stats + Sets Location), Uses Both Map + Party Data.
        battleManager.Initialize();
        map.UpdateMap();
    }
    public SceneMover sceneMover;
    public LoadingScreen loadingScreen;
    public MysteryDungeonState state;
    public MysteryDungeonGenerator generator;
    public MysteryDungeonBattleMap map;
    public MysteryDungeonBattleManager battleManager;
    public MysteryDungeonUIManager UIManager;
    public void EnterStore()
    {
        UIManager.shopUI.ShowMDStoreUI();
    }
    public PartyDataManager partyData;
    public int testGenerateSize;
    public MysteryDungeonMinimap miniMap;
    [ContextMenu("Test Generate")]
    public void TestGenerator()
    {
        GenerateDungeon();
        map.UpdateVisibleTiles();
        miniMap.CreateMinimap();
        map.UpdateMap();
    }
    protected void GenerateDungeon()
    {
        generator.GenerateDungeon(state.GetDungeonSize(), state.GetFloor());
        map.InitializeMapFromGenerator(generator, state.GetDungeonType(), state.GetDungeonEnemies());
        partyData.guildCard.QuestStartFloor(state.GetFloor(), map);
        state.SaveFromMysteryDungeonMap(map);
        map.UpdateVisibleTiles();
    }
    public void AddNPCToParty(string NPC)
    {
        partyData.AddTempPartyMember(NPC);
    }
    public bool PickUpItem(string item, int floor = 0)
    {
        if (item == "Gold")
        {
            int goldAmount = Random.Range(0, 6) + Mathf.RoundToInt(Mathf.Sqrt(floor) * Random.Range(5, 20));
            partyData.inventory.GainGold(goldAmount);
            return true;
        }
        if (partyData.dungeonBag.BagFull()){return false;}
        partyData.dungeonBag.GainItem(item);
        return true;
    }
    public void TriggerTrap(TacticActor actor, string trapName)
    {
        battleManager.itemEffect.TriggerTrap(actor, map, partyData, trapName);
        
    }
    public void NewFloor()
    {
        // Remove Defeat Members From The Party.
        for (int i = 0; i < map.defeatedActors.Count; i++)
        {
            partyData.RemovePartyMemberByID(map.defeatedActors[i].GetID());
        }
        // Check Quests At Current Map State.
        partyData.guildCard.CheckQuestCompletion(state.GetFloor(), map);
        // Generate New Map.
        if (!state.NewFloor())
        {
            CompleteDungeon();
            return;
        }
        loadingScreen.EnableLoading();
        loadingScreen.FinishLoadingScreen();
        GenerateDungeon();
        // Remove All Enemies Since They Are On A Different Floor Now.
        map.RemoveNonTeamMembers();
        // Restore Energy Of Party.
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            map.battlingActors[i].RestoreEnergy();
        }
        // Save Map + Party.
        partyData.dungeonState.NewFloor(map.battlingActors);
        partyData.Save();
        // Don't Need To Spawn The Party, Just Reset The Round So That The Player Can Act Again.
        battleManager.InitializePartyLocations(map.battlingActors, map.battlingActors.Count);
        map.SpawnStartingEnemies();
        battleManager.NewRound();
        map.UpdateVisibleTiles();
        map.UpdateMap();
    }
    public void CompleteDungeon(bool win = true)
    {
        // Update The Held Items As Well.
        partyData.dungeonState.NewFloor(map.battlingActors);
        if (win)
        {
            partyData.guildCard.CheckQuestCompletion(partyData);
        }
        partyData.Save();
        // TODO Move To Reward Scene If Win.
        sceneMover.ReturnToHub();
    }
}
