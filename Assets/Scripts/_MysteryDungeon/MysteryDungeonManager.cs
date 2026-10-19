using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls The Scene Flow.
public class MysteryDungeonManager : MonoBehaviour
{
    void Start()
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
        generator.GenerateDungeon();
        map.InitializeMapFromGenerator(generator, state.GetDungeonType());
        state.SaveFromMysteryDungeonMap(map);
        map.UpdateVisibleTiles();
    }
    public void NewFloor()
    {
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
        battleManager.NewRound();
        map.UpdateVisibleTiles();
        map.UpdateMap();
    }
    public void CompleteDungeon(bool win = true)
    {
        // TODO Move To Reward Scene If Win.
        sceneMover.ReturnToHub();
    }
}
