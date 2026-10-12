using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls The Scene Flow.
public class MysteryDungeonManager : MonoBehaviour
{
    void Start()
    {
        state.LoadToMysteryDungeonMap(map);
        partyData.Load();
        // Load Party + Add Party To Battling Actors.
        //battleManager.Initialize() Or Something.
        map.UpdateMap();
    }
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
        generator.GenerateDungeon(testGenerateSize);
        map.InitializeMapFromGenerator(generator, state.dungeonType);
        miniMap.CreateMinimap();
    }
    public void NewFloor()
    {
        // Generate New Map.
        generator.GenerateDungeon();
        map.InitializeMapFromGenerator(generator, state.dungeonType);
        // Remove All Enemies Since They Are On A Different Floor Now.
        // Restore Energy Of Party.
        // Save Map + Party.
    }
    public void CompleteDungeon(bool win = true)
    {
        // Move To Reward Scene.
    }
}
