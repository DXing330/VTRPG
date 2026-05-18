using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleStartManager", menuName = "ScriptableObjects/BattleLogic/BattleStartManager", order = 1)]
public class BattleStartManager : ScriptableObject
{
    public BattleState battleState;
    public BattleState roguelikeBattleState;
    public RelicBattleManager relicBattleManager;
    public CharacterList playerParty;
    public CharacterList enemyParty;
    protected bool loadedCustomBattleMap = false;
    protected List<int> customEnemyStartingLocations = new List<int>();
    protected List<int> customAllyStartingLocations = new List<int>();
    // Determines How The Battle Is Started.
    public void InitializeMap(BattleMap map, BattleManager manager, bool roguelike = false)
    {
        // Initialize Map
        map.ForceStart();
        manager.combatLog.ForceStart();
        manager.combatLog.AddNewLog();
        manager.UI.UpdateWinConString();
        map.SetWeather(battleState.GetWeather());
        manager.combatLog.UpdateNewestLog("The weather is " + battleState.GetWeather());
        map.SetTime(battleState.GetTime());
        manager.combatLog.UpdateNewestLog("The time is " + battleState.GetTime());
        // Custom Map Stuff
        customEnemyStartingLocations.Clear();
        customAllyStartingLocations.Clear();
        loadedCustomBattleMap = TryLoadCustomBattleMap(map);
        if (!loadedCustomBattleMap)
        {
            map.GetNewMapFeatures(manager.battleMapFeatures.CurrentMapFeatures());
            map.GetNewTerrainEffects(manager.battleMapFeatures.CurrentMapTerrainFeatures());
            manager.interactableMaker.GetNewInteractables(map, manager.battleMapFeatures.CurrentMapInteractables());
            map.InitializeElevations();
        }
        manager.moveManager.UpdateInfoFromBattleMap(map);
        manager.actorMaker.SetMapSize(map.mapSize);
        // Spawn The Actors
        SpawnActors(map, manager.actorMaker);
        // TODO Apply Starting Relics?
        if (roguelike)
        {
            // Get the relic list and counter from the dungeon bag.
            relicBattleManager.ApplyBattleRelicEffects(map.battlingActors, manager.partyData.dungeonBag);
        }
        if (loadedCustomBattleMap)
        {
            ApplyCustomEnemyStartingPositions(map);
        }
        else
        {
            map.RandomEnemyStartingPositions(battleState.GetEnemySpawnPattern());
        }
    }

    public void SpawnActors(BattleMap map, ActorMaker actorMaker)
    {
        int partySizeCap = map.MapMaxPartyCapacity();
        // Spawn actors in patterns based on teams.
        List<TacticActor> actors = new List<TacticActor>();
        actors = actorMaker.SpawnTeamInPattern(battleState.GetAllySpawnPattern(), 0, playerParty.characters, playerParty.stats, playerParty.characterNames, playerParty.equipment, playerParty.characterIDs);
        actorMaker.ApplyBattleModifiers(actors, playerParty.GetBattleModifiers());
        for (int i = 0; i < Mathf.Min(partySizeCap, actors.Count); i++)
        {
            // Add the assigned items to the actors from the inventory.
            actors[i].SetAssignedItems(map.battleManager.partyData.inventory.GetItemsAssignedToActorID(actors[i].GetID()));
            map.AddActorToBattle(actors[i]);
        }
        actors = new List<TacticActor>();
        actors = actorMaker.SpawnTeamInPattern(battleState.GetEnemySpawnPattern(), 1, enemyParty.characters, enemyParty.stats, enemyParty.characterNames, enemyParty.equipment, enemyParty.characterIDs);
        actorMaker.ApplyBattleModifiers(actors, enemyParty.GetBattleModifiers());
        for (int i = 0; i < Mathf.Min(partySizeCap, actors.Count); i++){ map.AddActorToBattle(actors[i]); }
    }

    // Apply relics/ascension/etc. battle modifier effects here.
    public void ApplyRelics(BattleManager manager)
    {

    }

    protected bool TryLoadCustomBattleMap(BattleMap map)
    {
        customEnemyStartingLocations.Clear();
        customAllyStartingLocations.Clear();
        if (!battleState.UsingCustomBattle()){return false;}
        else if (battleState.UsingCustomBattle() && battleState.savedBattles == null)
        {
            Debug.Log("TryLoadCustomBattleMap aborted. UsingCustomBattle=" + battleState.UsingCustomBattle() + " savedBattlesNull=" + (battleState.savedBattles == null));
            return false;
        }
        List<string> savedMapInfo;
        List<string> savedTerrainEffects;
        List<int> savedElevations;
        List<string> savedBorders;
        List<string> savedBuildings;
        List<string> savedEnemies;
        List<int> savedEnemyLocations;
        string savedWeather;
        string savedTime;
        List<int> savedAllyLocations;
        if (!battleState.savedBattles.TryLoadBattleData(battleState.GetCustomBattleName(), out savedMapInfo, out savedTerrainEffects, out savedElevations, out savedBorders, out savedBuildings, out savedEnemies, out savedEnemyLocations, out savedWeather, out savedTime, out savedAllyLocations))
        {
            Debug.Log("TryLoadCustomBattleMap failed to load saved data for: " + battleState.GetCustomBattleName());
            return false;
        }
        map.SetMapInfo(savedMapInfo);
        map.terrainEffectTiles = new List<string>(savedTerrainEffects);
        map.mapElevations = new List<int>(savedElevations);
        for (int i = 0; i < Mathf.Min(map.mapTiles.Count, savedBorders.Count); i++)
        {
            map.mapTiles[i].SetBorders(savedBorders[i].Split("|").ToList());
            map.UpdateTileBorderSprites(i);
        }
        for (int i = 0; i < Mathf.Min(map.mapTiles.Count, savedElevations.Count); i++)
        {
            map.ChangeTileElevation(i, savedElevations[i]);
        }
        for (int i = 0; i < savedBuildings.Count; i++)
        {
            if (savedBuildings[i].Length <= 0){continue;}
            map.AddBuilding(savedBuildings[i], i);
        }
        enemyParty.ResetLists();
        enemyParty.AddCharacters(savedEnemies);
        battleState.SetEnemyNames(savedEnemies);
        customEnemyStartingLocations = new List<int>(savedEnemyLocations);
        customAllyStartingLocations = new List<int>(savedAllyLocations);
        map.UpdateMap();
        return true;
    }
    
    protected void ApplyCustomEnemyStartingPositions(BattleMap map)
    {
        List<TacticActor> enemyTeam = map.AllTeamMembers(1);
        if (customEnemyStartingLocations.Count <= 0)
        {
            map.RandomEnemyStartingPositions(battleState.GetEnemySpawnPattern());
            return;
        }
        for (int i = 0; i < Mathf.Min(enemyTeam.Count, customEnemyStartingLocations.Count); i++)
        {
            enemyTeam[i].SetLocation(customEnemyStartingLocations[i]);
        }
        map.UpdateMap();
    }
    protected bool TileUsedByEnemy(List<int> enemyTiles, int tile)
    {
        return enemyTiles.Contains(tile);
    }
    public bool HasCustomAllyStartingLocations()
    {
        return customAllyStartingLocations.Count > 0;
    }
    public List<int> GetCustomAllyStartingTiles(BattleMap map, int allyCount)
    {
        return ExpandCustomAllyStartingLocations(map, allyCount);
    }
    public void ApplyCustomAllyStartingPositions(BattleMap map)
    {
        List<TacticActor> allyTeam = map.AllTeamMembers(0);
        if (!HasCustomAllyStartingLocations())
        {
            map.RandomAllyStartingPositions(battleState.GetAllySpawnPattern());
            return;
        }
        List<int> allyTiles = ExpandCustomAllyStartingLocations(map, allyTeam.Count);
        for (int i = 0; i < Mathf.Min(allyTeam.Count, allyTiles.Count); i++)
        {
            allyTeam[i].SetLocation(allyTiles[i]);
        }
        map.UpdateMap();
    }
    public List<int> ExpandCustomAllyStartingLocations(BattleMap map, int allyCount, List<int> currentStarting = null)
    {
        if (customAllyStartingLocations.Count >= allyCount)
        {
            return customAllyStartingLocations;
        }
        if (currentStarting == null)
        {
            currentStarting = new List<int>(customAllyStartingLocations);
        }
        // You shouldn't be allowed more than half the map tiles.
        if (currentStarting.Count >= (map.mapSize * map.mapSize) / 2)
        {
            return currentStarting;
        }
        // Get the borders of the currentStarting.
        List<int> frontier = map.mapUtility.AdjacentBorders(currentStarting, map.mapSize);
        currentStarting.AddRange(frontier);
        currentStarting = currentStarting.Distinct().ToList();
        // Remove Any Enemy Tiles.
        for (int i = currentStarting.Count - 1; i >= 0; i--)
        {
            if (TileUsedByEnemy(customEnemyStartingLocations, currentStarting[i]))
            {
                currentStarting.RemoveAt(i);
            }
        }
        if (currentStarting.Count < allyCount)
        {
            return ExpandCustomAllyStartingLocations(map, allyCount, currentStarting);
        }
        return currentStarting;
    }
}
