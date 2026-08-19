using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Add a battle manager so the map doesn't get too bloated, the map is just the visualization of the battle map and anything that will result in a change of that visualization, a lot of logic will be handled somewhere else.
public class BattleMap : MapManager
{
    [ContextMenu("ForceStart")]
    public void ForceStart()
    {
        InitializeEmptyList();
        terrainEffectTiles = new List<string>(emptyList);
        interactables.Clear();
        // Reset Everything.
        InitializeBorders();
        ResetBuildings();
        ResetAuras();
        ResetActors();
        if (combatLog != null)
        {
            combatLog.ForceStart();
        }
    }
    protected override void Start()
    {
        InitializeEmptyList();
        // Don't start again if you already force started.
        if (terrainEffectTiles.Count < emptyList.Count)
        {
            terrainEffectTiles = new List<string>(emptyList);
            interactables.Clear();
            InitializeBorders();
            ResetBuildings();
            ResetActors();
        }
    }
    // MAP CONFIG CONSTANTS
    public int buildingLayer = 1;
    public int actorLayer = 2;
    public int tEffectLayer = 3;
    public int highlightLayer = 4;
    protected bool DisplayLayerExists(int layer)
    {
        return layer >= 0 && layer < mapDisplayers.Count && mapDisplayers[layer] != null;
    }
    // UTILITIES
    public MoveCostManager moveManager;
    public void UpdateMoveCostManager()
    {
        moveManager.UpdateInfoFromBattleMap(this);
    }
    public BattleMapUtility battleMapUtility;
    // For attacking/skill usage, since some auto trigger when moving or checking map conditions.
    public ActiveManager activeManager;
    public MapPatternLocations mapPatterns;
    public StatusDetailViewer detailViewer;
    public void EnsureUniqueActorToTile(TacticActor actor)
    {
        List<TacticActor> overlappingActors = new List<TacticActor>();
        int location = actor.GetLocation();
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i] == actor || battlingActors[i].GetLocation() != location){continue;}
            overlappingActors.Add(battlingActors[i]);
        }
        for (int i = 0; i < overlappingActors.Count; i++)
        {
            int newLocation = GetClosestEmptyTile(overlappingActors[i]);
            MoveActorToTile(overlappingActors[i], newLocation, true);
        }
    }
    public void ActorStartTurn(TacticActor actor)
    {
        ApplyWeatherStartEffect(actor);
        ApplyTileStartEffect(actor);
        ApplyBuildingStartEffect(actor);
        ApplyTerrainStartEffect(actor);
        ApplyAuraEffects(actor, "Start");
        EnsureUniqueActorToTile(actor);
    }
    public void ActorEndsTurn(TacticActor actor)
    {
        ApplyWeatherEndEffect(actor);
        ApplyTileEndEffect(actor);
        ApplyBuildingEndEffect(actor);
        EndTurnOnInteractable(actor);
        ApplyAuraEffects(actor, "End");
    }
    public string weather;
    public TerrainPassivesList weatherPassives;
    public void ApplyWeatherStartEffect(TacticActor actor)
    {
        List<string> weatherP = weatherPassives.ReturnStartPassive(weather);
        weatherP.AddRange(weatherPassives.ReturnStartPassive(time));
        ApplyPassiveEffects(weatherP, actor);
    }
    public void ApplyWeatherEndEffect(TacticActor actor)
    {
        List<string> weatherP = weatherPassives.ReturnEndPassive(weather);
        weatherP.AddRange(weatherPassives.ReturnEndPassive(time));
        ApplyPassiveEffects(weatherP, actor);
    }
    public WeatherFilter weatherFilter;
    public void SetWeather(string newInfo)
    {
        weather = newInfo;
        if (weatherFilter == null){return;}
        weatherFilter.UpdateFilter(weather);
    }
    public int battleRound;
    public void SetRound(int newInfo)
    {
        battleRound = newInfo;
        if (combatLog != null)
        {
            combatLog.SetRound(battleRound);
        }
        // Make Sure No Alive Actors Have Location = -1;
        FixActorLocations();
    }
    // TODO Later Place It Next To A Random Alive Ally.
    public void FixActorLocations()
    {
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetLocation() < 0 && battlingActors[i].GetHealth() > 0)
            {
                // Throw them all on a tile, it'll deal with itself somehow.
                battlingActors[i].SetLocation(0);
                UpdateCombatLog(battlingActors[i].GetPersonalName() + " was out of bounds.");
                Debug.Log(battlingActors[i].GetPersonalName() + " was out of bounds.");
            }
        }
    }
    public int GetRound()
    {
        return battleRound;
    }
    public string GetWeather() { return weather; }
    public string time;
    public DayNightFilter timeFilter;
    public WeatherFilter timeDisplay;
    public void SetTime(string newInfo)
    { 
        time = newInfo;
        if (timeFilter != null){timeFilter.SetTime(time);}
        if (timeDisplay != null){timeDisplay.UpdateFilter(time);}
    }
    public string GetTime(){ return time; }
    public CombatLog combatLog;
    public void AddNewCombatLog()
    {
        if (combatLog == null){return;}
        combatLog.AddNewLog();
    }
    public void InitializeCombatLog()
    {
        if (combatLog == null){return;}
        combatLog.ForceStart();
    }
    public void UpdateCombatLog(string newLog, bool detailed = false)
    {
        if (combatLog == null){return;}
        if (detailed)
        {
            combatLog.AddDetailedLogs(newLog);
        }
        else
        {
            combatLog.UpdateNewestLog(newLog);
        }
    }
    public BattleStatsTracker damageTracker;
    public void InitializeDamageTracker()
    {
        if (damageTracker == null){return;}
        damageTracker.InitializeDamageTracker(battlingActors);
    }
    public void UpdateDamageStat(TacticActor dealer, TacticActor taker, int damage)
    {
        if (damageTracker == null){return;}
        damageTracker.UpdateDamageStat(dealer, taker, damage);
    }
    public TerrainPassivesList terrainPassives;
    public List<string> ReturnTerrainStartPassive(TacticActor actor)
    {
        string terrainType = mapInfo[actor.GetLocation()];
        return terrainPassives.ReturnStartPassive(terrainType);
    }
    public List<string> ReturnTerrainEndPassive(TacticActor actor)
    {
        string terrainType = mapInfo[actor.GetLocation()];
        return terrainPassives.ReturnEndPassive(terrainType);
    }
    public void ApplyTileStartEffect(TacticActor actor)
    {
        if (actor.GetLocation() < 0){return;}
        string terrainType = mapInfo[actor.GetLocation()];
        List<string> tilePassives = terrainPassives.ReturnStartPassive(terrainType);
        ApplyPassiveEffects(tilePassives, actor);
    }
    public void ApplyTileEndEffect(TacticActor actor)
    {
        string terrainType = mapInfo[actor.GetLocation()];
        List<string> tilePassives = terrainPassives.ReturnEndPassive(terrainType);
        ApplyPassiveEffects(tilePassives, actor);
    }
    public List<string> ReturnTileMovingPassive(TacticActor actor)
    {
        string terrainType = mapInfo[actor.GetLocation()];
        return terrainPassives.ReturnMovingPassive(terrainType);
    }
    public TerrainPassivesList buildingEffectData;
    public StatDatabase buildingStats;
    public List<string> buildings;
    public bool BuildingExists(string buildingName)
    {
        return buildings.Contains(buildingName);
    }
    public List<int> buildingLocations;
    public void ResetBuildings()
    {
        buildings.Clear();
        buildingLocations.Clear();
        buildingHealths.Clear();
        buildingDefenses.Clear();
        UpdateMoveCostManager();
    }
    public int GetBuildingIndexFromLocation(int tileNumber)
    {
        return buildingLocations.IndexOf(tileNumber);
    }
    public string GetBuildingOnTile(int tileNumber)
    {
        int indexOf = buildingLocations.IndexOf(tileNumber);
        if (indexOf < 0){return "";}
        return buildings[indexOf];
    }
    public List<int> buildingHealths;
    public int GetBuildingHealthOnLocation(int tileNumber)
    {
        int indexOf = buildingLocations.IndexOf(tileNumber);
        if (indexOf < 0){return -1;}
        return buildingHealths[indexOf];
    }
    public List<int> buildingDefenses;
    public int GetBuildingDefenseOnLocation(int tileNumber)
    {
        int indexOf = buildingLocations.IndexOf(tileNumber);
        if (indexOf < 0){return -1;}
        return buildingDefenses[indexOf];
    }
    public void SetBuildingHealthAndDefense(int tileNumber, int newHealth, int newDefense)
    {
        int index = GetBuildingIndexFromLocation(tileNumber);
        if (index < 0){return;}
        buildingHealths[index] = newHealth;
        buildingDefenses[index] = newDefense;
    }
    public void DamageTileBuilding(int tileNumber, TacticActor attacker, int damage)
    {
        int index = GetBuildingIndexFromLocation(tileNumber);
        if (index < 0){return;}
        damage = Mathf.Max(0, damage - buildingDefenses[index]);
        buildingHealths[index] -= damage;
        if (buildingHealths[index] <= 0)
        {
            UpdateCombatLog(attacker.GetPersonalName() + " destroys the " + buildings[index] + " while attacking.");
            RemoveBuildingAtIndex(index);
        }
    }
    protected void RemoveBuildingAtIndex(int index)
    {
        buildings.RemoveAt(index);
        buildingLocations.RemoveAt(index);
        buildingHealths.RemoveAt(index);
        buildingDefenses.RemoveAt(index);
    }
    public void RemoveBuildingAtLocation(int tileNumber)
    {
        int indexOf = buildingLocations.IndexOf(tileNumber);
        if (indexOf >= 0)
        {
            RemoveBuildingAtIndex(indexOf);
        }
    }
    public void AddBuilding(string buildingName, int buildingLocation)
    {
        if (buildingLocations.Contains(buildingLocation)){return;}
        string buStats = buildingStats.ReturnValue(buildingName);
        if (buStats.Length < 3){return;}
        string[] buHPDef = buStats.Split("|");
        buildings.Add(buildingName);
        buildingLocations.Add(buildingLocation);
        buildingHealths.Add(int.Parse(buHPDef[0]));
        buildingDefenses.Add(int.Parse(buHPDef[1]));
        UpdateMoveCostManager();
        UpdateMap();
    }
    public void ApplyBuildingStartEffect(TacticActor actor)
    {
        string building = GetBuildingOnTile(actor.GetLocation());
        if (building.Length < 1) { return; }
        List<string> buildingP = buildingEffectData.ReturnStartPassive(building);
        ApplyPassiveEffects(buildingP, actor);
    }
    public void ApplyBuildingEndEffect(TacticActor actor)
    {
        string building = GetBuildingOnTile(actor.GetLocation());
        if (building.Length < 1) { return; }
        List<string> buildingP = buildingEffectData.ReturnEndPassive(building);
        ApplyPassiveEffects(buildingP, actor);
    }
    public TerrainPassivesList borderEffectData;
    protected void ApplyBorderMovingEffect(TacticActor actor, int tileNumber)
    {
        // Get The Border In The New Tile, Based On The Actor's Incoming Direction.
        int incomingDirection = (actor.GetDirection() + 3) % 6;
        string border = mapTiles[tileNumber].GetBorderInDirection(incomingDirection);
        if (border.Length < 1) { return; }
        // Apply The Effect.
        List<string> borderP = borderEffectData.ReturnMovingPassive(border);
        ApplyMovingPassiveEffects(borderP, actor, tileNumber);
    }
    public TerrainPassivesList terrainEffectData;
    public void ApplyTerrainStartEffect(TacticActor actor)
    {
        if (actor.GetLocation() < 0){return;}
        string terrainEffect = terrainEffectTiles[actor.GetLocation()];
        if (terrainEffect.Length < 1) { return; }
        List<string> tEffectP = terrainEffectData.ReturnStartPassive(terrainEffect);
        ApplyPassiveEffects(tEffectP, actor);
    }
    public void ApplyEndTerrainEffect(TacticActor actor)
    {
        string terrainEffect = terrainEffectTiles[actor.GetLocation()];
        if (terrainEffect.Length < 1) { return; }
        List<string> tEffectP = terrainEffectData.ReturnEndPassive(terrainEffect);
        ApplyPassiveEffects(tEffectP, actor);
    }
    public StatDatabase terrainWeatherInteractions;
    public StatDatabase terrainTileInteractions;
    public StatDatabase tileWeatherInteractions;
    public StatDatabase elementalInterations;
    public bool CheckTileConditions(int tile, string condition, string specifics)
    {
        switch (condition)
        {
            case "Tile":
                return mapInfo[tile].Contains(specifics);
            case "TerrainEffect":
                return terrainEffectTiles[tile].Contains(specifics);
        }
        return true;
    }
    public void ElementalAttackOnTile(string element, int tile)
    {
        List<string> elementalEffects = elementalInterations.ReturnAllValues(element);
        for (int i = 0; i < elementalEffects.Count; i++)
        {
            string[] eBlocks = elementalEffects[i].Split("|");
            if (CheckTileConditions(tile, eBlocks[0], eBlocks[1]))
            {
                ChangeTile(tile, eBlocks[3], eBlocks[4]);
            }
        }
    }
    public PassiveSkill passiveEffect;
    public void ApplyMovingPassiveEffects(List<string> passives, TacticActor actor, int tileNumber)
    {
        for (int i = 0; i < passives.Count; i++)
        {
            string[] data = passives[i].Split("|");
            if (data.Length < 6){return;}
            if (passiveEffect.CheckMovingCondition(actor, data[1], data[2], tileNumber, this))
            {
                passiveEffect.AffectActor(actor, data[4], data[5]);
            }
        }
    }
    public void ApplyPassiveEffects(List<string> passives, TacticActor actor)
    {
        for (int i = 0; i < passives.Count; i++)
        {
            string[] data = passives[i].Split("|");
            if (data.Length < 6){return;}
            if (passiveEffect.CheckStartEndConditions(actor, data[1], data[2], this))
            {
                passiveEffect.AffectActor(actor, data[4], data[5]);
            }
        }
    }
    public List<TacticActor> ReturnEndOfBattleActors()
    {
        List<TacticActor> endOfB = new List<TacticActor>();
        endOfB.AddRange(escapedActors);
        endOfB.AddRange(capturedActors);
        endOfB.AddRange(battlingActors);
        return endOfB;
    }
    public ActorMaker actorMaker;
    public List<TacticActor> battlingActors;
    // All Summoning Should Go Through Here?
    public void SpawnAndAddActor(int location, string actorName, int team = 0, TacticActor summoner = null)
    {
        TacticActor newActor = actorMaker.SummonActor(location, actorName, team);
        // TODO Add Battle Modifiers?
        AddActorToBattle(newActor);
        passiveEffect.ApplyStartBattlePassives(newActor, this);
        if (summoner != null)
        {
            passiveEffect.AffectActor(newActor, "SingleTemporarySkill", "Break Summon Link");
            summoner.AddSummonedActor(newActor);
        }
    }
    public void ResetActors()
    {
        battlingActors.Clear();
        defeatedActors.Clear();
    }
    public void RemoveActorWithID(int ID)
    {
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetID() == ID)
            {
                battlingActors.RemoveAt(i);
                return;
            }
        }
    }
    public void AddActorToBattle(TacticActor actor)
    {
        battlingActors.Add(actor);
        UpdateMap();
    }
    public bool AllyExists(string spriteName, int team = 0)
    {
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetTeam() != team){continue;}
            if (battlingActors[i].GetSpriteName() == spriteName){return true;}
        }
        return false;
    }
    public bool EnemyExists(string spriteName, int team = 0)
    {
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetTeam() == team){continue;}
            if (battlingActors[i].GetSpriteName() == spriteName){return true;}
        }
        return false;
    }
    public int AverageActorHealth()
    {
        return battleMapUtility.AverageActorHealth(this);
    }
    public List<TacticActor> defeatedActors;
    public List<TacticActor> GetDefeatedActors()
    {
        return defeatedActors;
    }
    public bool RemoveDefeatedActor(TacticActor actor)
    {
        if (!defeatedActors.Contains(actor)){return false;}
        defeatedActors.Remove(actor);
        return true;
    }
    public void ReviveActor(TacticActor actor)
    {
        if (RemoveDefeatedActor(actor))
        {
            AddActorToBattle(actor);
        }
    }
    public int ReviveDefeatedActorsBySprite(string spriteName)
    {
        int revivedCount = 0;
        for (int i = defeatedActors.Count - 1; i >= 0; i--)
        {
            if (defeatedActors[i].GetSpriteName() != spriteName){continue;}
            if (ReviveDefeatedActorAtOpenTile(defeatedActors[i]))
            {
                defeatedActors.RemoveAt(i);
                revivedCount++;
            }
        }
        return revivedCount;
    }
    public TacticActor ReturnFirstDeadEnemy(TacticActor actor)
    {
        for (int i = 0; i < defeatedActors.Count; i++)
        {
            if (defeatedActors[i].GetTeam() == actor.GetTeam()){continue;}
            TacticActor firstDead = defeatedActors[i];
            return firstDead;
        }
        return null;
    }
    public bool ReviveRandomAlly(TacticActor actor)
    {
        List<TacticActor> defeatedAllies = new List<TacticActor>();
        for (int i = defeatedActors.Count - 1; i >= 0; i--)
        {
            if (defeatedActors[i].GetTeam() != actor.GetTeam()){continue;}
            defeatedAllies.Add(defeatedActors[i]);
        }
        if (defeatedAllies.Count <= 0){return false;}
        int randomIndex = UnityEngine.Random.Range(0, defeatedAllies.Count);
        if (!ReviveDefeatedActorAtOpenTile(defeatedAllies[randomIndex])){return false;}
        defeatedActors.Remove(defeatedAllies[randomIndex]);
        return true;
    }
    protected bool ReviveDefeatedActorAtOpenTile(TacticActor actor)
    {
        int reviveTile = GetReviveTile(actor.GetLocation());
        if (reviveTile < 0){return false;}
        actor.SetLocation(reviveTile);
        actor.SetCurrentHealth(Mathf.Max(1, actor.GetBaseHealth() / 2));
        AddActorToBattle(actor);
        UpdateCombatLog(actor.GetPersonalName() + " is revived.");
        return true;
    }
    protected int GetReviveTile(int defeatedTile)
    {
        if (defeatedTile >= 0 && defeatedTile < mapSize * mapSize && GetActorOnTile(defeatedTile) == null)
        {
            return defeatedTile;
        }
        List<int> emptyAdjacentTiles = ReturnEmptyAdjacentTiles(defeatedTile);
        if (emptyAdjacentTiles.Count <= 0){return -1;}
        return emptyAdjacentTiles[UnityEngine.Random.Range(0, emptyAdjacentTiles.Count)];
    }
    protected List<int> ReturnEmptyAdjacentTiles(int tileNumber)
    {
        List<int> emptyAdjacentTiles = new List<int>();
        if (tileNumber < 0 || tileNumber >= mapSize * mapSize){return emptyAdjacentTiles;}
        List<int> adjacentTiles = mapUtility.AdjacentTiles(tileNumber, mapSize);
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            if (GetActorOnTile(adjacentTiles[i]) == null)
            {
                emptyAdjacentTiles.Add(adjacentTiles[i]);
            }
        }
        return emptyAdjacentTiles;
    }
    public List<TacticActor> capturedActors;
    public void CaptureActor(TacticActor actor)
    {
        int indexOf = battlingActors.IndexOf(actor);
        if (indexOf >= 0)
        {
            capturedActors.Add(actor);
            battlingActors.RemoveAt(indexOf);
        }
    }
    public bool CapturedActor(string spriteName, string actorName = "")
    {
        for (int i = 0; i < capturedActors.Count; i++)
        {
            if (capturedActors[i].GetSpriteName() == spriteName)
            {
                if (actorName == ""){return true;}
                else if (capturedActors[i].GetPersonalName() == actorName)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public List<TacticActor> escapedActors;
    public bool ActorCanEscape(TacticActor actor)
    {
        if (!mapUtility.BorderTile(actor.GetLocation(), mapSize)){return false;}
        if (GetAdjacentEnemies(actor).Count > 0){return false;}
        return true;
    }
    public void ActorEscapesBattle(TacticActor actor)
    {
        int indexOf = battlingActors.IndexOf(actor);
        if (indexOf >= 0)
        {
            // Deal with escaped actors differently at the end of battle.
            escapedActors.Add(actor);
            // Just play dead and leave the battle?
        }
    }
    public bool ActorEscaped(string spriteName, string actorName = "")
    {
        for (int i = 0; i < escapedActors.Count; i++)
        {
            if (escapedActors[i].GetSpriteName() == spriteName)
            {
                if (actorName == ""){return true;}
                else if (escapedActors[i].GetPersonalName() == actorName)
                {
                    return true;
                }
            }
        }
        return false;
    }
    public TacticActor ReturnLatestActor()
    {
        return battlingActors[battlingActors.Count - 1];
    }
    public void MoveActorPassive(TacticActor actor, string effect, string specifics)
    {
        int direction = actor.GetDirection();
        switch (effect)
        {
            default:
            break;
            case "MoveForwardRandom":
            // Get the direction, either + (5, 0, 1) from your current direction;
            direction = (direction + UnityEngine.Random.Range(-1, 2) + 6) % 6;
            break;
            case "MoveBackwardRandom":
            // Get the direction, either + (2, 3, 4) from your current direction;
            direction = (direction + UnityEngine.Random.Range(2, 5)) % 6;
            break;
            case "AKRaidMove":
            int akRaidTile = battleMapUtility.AKRaidMove(actor, this);
            if (akRaidTile >= 0)
            {
                MoveActorToTile(actor, akRaidTile, true);
            }
            // Else return, don't try to move if there's no valid target.
            return;
        }
        int rTile = mapUtility.PointInDirection(actor.GetLocation(), direction, mapSize);
        if (GetActorOnTile(rTile) == null && rTile >= 0)
        {
            MoveActorToTile(actor, rTile, true);
        }
    }
    public void SwitchActorLocations(TacticActor actor1, TacticActor actor2)
    {
        int temp = actor1.GetLocation();
        actor1.SetLocation(actor2.GetLocation());
        actor2.SetLocation(temp);
        UpdateMap();
    }
    public int GetClosestTeamMemberInRange(int start, int range, int team, List<int> except)
    {
        // Get all adjacent tiles in range.
        List<int> tilesInRange = mapUtility.GetTilesInCircleShape(start, range, mapSize);
        for (int i = 0; i < tilesInRange.Count; i++)
        {
            TacticActor tempActor = GetActorOnTile(tilesInRange[i]);
            if (tempActor != null && tempActor.GetTeam() == team && !except.Contains(tilesInRange[i]))
            {
                return tilesInRange[i];
            }
        }
        return -1;
    }
    public List<TacticActor> ChainLightningTargets(int start, int bounceRange = 2, int bounceCount = 3)
    {
        List<TacticActor> actors = new List<TacticActor>();
        List<int> chainTiles = new List<int>();
        TacticActor latestActor = GetActorOnTile(start);
        if (latestActor == null){return actors;}
        int team = latestActor.GetTeam();
        int lightningTile = latestActor.GetLocation();
        int nextLightningTile = -1;
        chainTiles.Add(lightningTile);
        for (int i = 0; i < bounceCount; i++)
        {
            nextLightningTile = GetClosestTeamMemberInRange(lightningTile, bounceRange, team, chainTiles);
            if (nextLightningTile < 0){break;}
            chainTiles.Add(nextLightningTile);
            lightningTile = nextLightningTile;
        }
        for (int i = 0; i < chainTiles.Count; i++)
        {
            actors.Add(GetActorOnTile(chainTiles[i]));
        }
        return actors;
    }
    // Should highlight all valid starting tiles to make this clear.
    public bool ValidStartingTile(string pattern, int tileNumber, List<int> overrideStartingPositions = null)
    {
        int startTileCount = mapSize * mapSize / 2 - (mapSize * mapSize / 2) % mapSize;
        List<int> startingTiles = mapPatterns.ReturnTilesOfPattern(pattern, startTileCount, mapSize);
        if (overrideStartingPositions != null && overrideStartingPositions.Count > 1)
        {
            startingTiles = overrideStartingPositions;
        }
        return startingTiles.Contains(tileNumber);
    }
    public List<string> excludedStartingTiles;
    public void RandomAllyStartingPositions(string pattern)
    {
        List<TacticActor> team = AllTeamMembers(0);
        for (int i = 0; i < team.Count; i++)
        {
            team[i].SetLocation(RandomStartingTile(pattern));
        }
        UpdateMap();
    }
    public void RandomEnemyStartingPositions(string pattern)
    {
        List<TacticActor> enemyTeam = AllTeamMembers(1);
        for (int i = 0; i < enemyTeam.Count; i++)
        {
            enemyTeam[i].SetLocation(RandomStartingTile(pattern));
        }
        UpdateMap();
    }
    protected int RandomStartingTile(string pattern)
    {
        int tile = UnityEngine.Random.Range(0, mapSize * mapSize);
        if (ValidRandomStartingTile(pattern, tile))
        {
            return tile;
        }
        return RandomStartingTile(pattern);
    }
    // 0 - North, 1 - East, 2 = South, 3- West
    public int EmptyStartingTileByEdge(int edge = 0)
    {
        int column = 0;
        int row = 0;
        int tile = 0;
        switch (edge)
        {
            case 1:
            column = mapSize - 1;
            break;
            case 2:
            row = mapSize - 1;
            break;
            default:
            break;
        }
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            tile = mapUtility.ReturnTileNumberFromRowCol(row, column, mapSize);
            if (GetActorOnTile(tile) == null){return tile;}
            switch (edge)
            {
                case 0:
                column++;
                if (column >= mapSize)
                {
                    row++;
                    column = 0;
                }
                break;
                case 1:
                row++;
                if (row >= mapSize)
                {
                    row = 0;
                    column--;
                }
                break;
                case 2:
                column++;
                if (column >= mapSize)
                {
                    row--;
                    column = 0;
                }
                break;
                case 3:
                default:
                row++;
                if (row >= mapSize)
                {
                    row = 0;
                    column++;
                }
                break;
            }
        }
        return -1;
    }
    // Needs to be updated based on the spawning pattern.
    protected bool ValidRandomStartingTile(string pattern, int tileNumber)
    {
        int startTileCount = mapSize * mapSize / 2 - (mapSize * mapSize / 2) % mapSize;
        List<int> startingTiles = mapPatterns.ReturnTilesOfPattern(pattern, startTileCount, mapSize);
        if (startingTiles.Contains(tileNumber) && !excludedStartingTiles.Contains(mapInfo[tileNumber]) && !TileNotEmpty(tileNumber))
        {
            return true;
        }
        return false;
    }
    public void ChangeActorsLocation(int startingTile, int newTile)
    {
        TacticActor actor = GetActorOnTile(startingTile);
        TacticActor actor2 = GetActorOnTile(newTile);
        if (actor == null){return;}
        actor.SetLocation(newTile);
        if (actor2 == null){}
        else
        {
            actor2.SetLocation(startingTile);
        }
        UpdateMap();
    }
    public void ResurrectActor(TacticActor actor)
    {
        actor.FullRestore();
        ReviveActor(actor);
    }
    public List<TacticActor> RemoveActorsFromBattle()
    {
        List<TacticActor> deadActors = new List<TacticActor>();
        for (int i = battlingActors.Count - 1; i >= 0; i--)
        {
            if (ActorEscaped(battlingActors[i].GetSpriteName()))
            {
                UpdateCombatLog(battlingActors[i].GetPersonalName() + " escaped from the battle.");
                battlingActors.RemoveAt(i);
                continue;
            }
            if (battlingActors[i].GetHealth() <= 0)
            {
                if (battlingActors[i].GetImmortal())
                {
                    battlingActors[i].SetCurrentHealth(1);
                    continue;
                }
                UpdateCombatLog(battlingActors[i].GetPersonalName() + " is defeated.");
                // Don't activate death passives, don't allow revival.
                if (battlingActors[i].WasSacrificed())
                {
                    battlingActors.RemoveAt(i);
                    continue;
                }
                // Apply the death passives here.
                deadActors.Add(battlingActors[i]);
                defeatedActors.Add(battlingActors[i]);
                battlingActors.RemoveAt(i);
            }
        }
        return deadActors;
    }
    public List<TacticActor> AllTeamMembers(int team)
    {
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetTeam() == team)
            {
                actors.Add(battlingActors[i]);
            }
        }
        return actors;
    }
    public List<TacticActor> AllAllies(TacticActor actor)
    {
        return AllTeamMembers(actor.GetTeam());
    }
    public List<TacticActor> AllEnemies(TacticActor actor)
    {
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetTeam() != actor.GetTeam())
            {
                actors.Add(battlingActors[i]);
            }
        }
        return actors;
    }
    public TacticActor FindActorByTargetSpecifics(TacticActor finder, string target, string specifics)
    {
        List<TacticActor> targets = new List<TacticActor>();
        if (target == "Ally")
        {
            targets = AllAllies(finder);
            targets.Remove(finder);
        }
        else if (target == "Enemy")
        {
            targets = AllEnemies(finder);
        }
        else if (target == "ALL")
        {
            targets = battlingActors;
            targets.Remove(finder);
        }
        if (targets.Count <= 0){return null;}
        if (specifics == "Furthest")
        {
            return GetFurthestActor(finder, targets);
        }
        else if (specifics == "Closest")
        {
            return GetClosestActor(finder, targets);
        }
        else if (specifics.StartsWith("Highest"))
        {
            string highestStat = specifics.Replace("Highest", "");
            return FindActorByStat(targets, highestStat, true);
        }
        else if (specifics.StartsWith("Lowest"))
        {
            string lowestStat = specifics.Replace("Lowest", "");
            return FindActorByStat(targets, lowestStat, false);
        }
        return null;
    }
    public TacticActor FindActorByStat(List<TacticActor> actors, string stat = "Health", bool highest = true)
    {
        if (actors.Count <= 0){return null;}
        int index = 0;
        int statAmount = -1;
        if (!highest)
        {
            statAmount = int.MaxValue;
        }
        for (int i = 0; i < actors.Count; i++)
        {
            int actorStat = 0;
            switch (stat)
            {
                default:
                actorStat = actors[i].GetHealth();
                break;
                case "Attack":
                actorStat = actors[i].GetAttack();
                break;
                case "Defense":
                actorStat = actors[i].GetDefense();
                break;
                case "Energy":
                actorStat = actors[i].GetEnergy();
                break;
            }
            if (actorStat > statAmount && highest)
            {
                index = i;
                statAmount = actorStat;
            }
            else if (actorStat < statAmount && !highest)
            {
                index = i;
                statAmount = actorStat;
            }
        }
        return actors[index];
    }
    public TacticActor FindEnemyByStat(TacticActor actor, string stat = "Health", bool highest = true)
    {
        List<TacticActor> actors = AllEnemies(actor);
        return FindActorByStat(actors, stat, highest);
    }
    public List<TacticActor> AllActorsBySpecies(string speciesName)
    {
        return battleMapUtility.AllActorsBySpecies(speciesName, this);
    }
    public List<TacticActor> AllActorsBySprite(string spriteName)
    {
        return battleMapUtility.AllActorsBySprite(spriteName, this);
    }
    public bool AllyAdjacentToActor(TacticActor actor)
    {
        List<TacticActor> adjacentActors = GetAdjacentActors(actor.GetLocation());
        for (int i = 0; i < adjacentActors.Count; i++)
        {
            if (adjacentActors[i].GetTeam() == actor.GetTeam())
            {
                return true;
            }
        }
        return false;
    }
    public bool AllyAdjacentWithSpriteName(TacticActor actor, string specificSprite)
    {
        List<TacticActor> adjacentActors = GetAdjacentActors(actor.GetLocation());
        int team = actor.GetTeam();
        for (int i = 0; i < adjacentActors.Count; i++)
        {
            if (adjacentActors[i].GetTeam() == team && adjacentActors[i].GetSpriteName().Contains(specificSprite))
            {
                return true;
            }
        }
        return false;
    }
    // List of actor names on tiles.
    public List<string> actorTiles;
    public bool TileNotEmpty(int tileNumber)
    {
        // If someone's name is on the tile then it's not empty.
        return (actorTiles[tileNumber].Length > 1);
    }
    public bool FacingEmptyTile(TacticActor actor, bool forward = true)
    {
        int dir = actor.GetDirection();
        if (!forward)
        {
            dir = (dir + 3) % 6;
        }
        return !TileNotEmpty(mapUtility.PointInDirection(actor.GetLocation(), dir, mapSize));
    }
    public bool TargetFacingActor(TacticActor actor)
    {
        TacticActor target = actor.GetTarget();
        if (target == null){return false;}
        int direction = target.GetDirection();
        int directionBetween = mapUtility.DirectionBetweenLocations(target.GetLocation(), actor.GetLocation(), mapSize);
        if (direction == directionBetween || (direction + 1) % 6 == directionBetween || (direction + 5) % 6 == directionBetween)
        {
            return true;
        }
        return false;
    }
    public bool FacingActor(TacticActor actor)
    {
        int startingPoint = actor.GetLocation();
        int direction = actor.GetDirection();
        for (int i = 0; i < actor.GetAttackRange(); i++)
        {
            if (TileNotEmpty(mapUtility.PointInDirection(startingPoint, direction, mapSize)))
            {
                return true;
            }
            startingPoint = mapUtility.PointInDirection(startingPoint, direction, mapSize);
        }
        return false;
    }
    public TacticActor ReturnClosestFacingActor(TacticActor actor)
    {
        int startingPoint = actor.GetLocation();
        int direction = actor.GetDirection();
        for (int i = 0; i < actor.GetAttackRange(); i++)
        {
            if (TileNotEmpty(mapUtility.PointInDirection(startingPoint, direction, mapSize)))
            {
                return GetActorOnTile(mapUtility.PointInDirection(startingPoint, direction, mapSize));
            }
            startingPoint = mapUtility.PointInDirection(startingPoint, direction, mapSize);
        }
        return null;
    }
    public List<int> ReturnEmptyTiles(List<int> newTiles)
    {
        for (int i = newTiles.Count - 1; i >= 0; i--)
        {
            if (TileNotEmpty(newTiles[i]))
            {
                newTiles.RemoveAt(i);
            }
        }
        return newTiles;
    }
    // List of actor directions on tiles.
    public List<string> actorDirections;
    public StatDatabase tileTileInteractions;
    public void ChangeBorder(int tileNumber, int direction, string effect)
    {
        mapTiles[tileNumber].ChangeBorder(effect, direction);
        UpdateTileBorderSprites(tileNumber);
        UpdateBorders();
        UpdateMoveCostManager();
    }
    public void ChangeAllBorders(int tileNumber, string effect)
    {
        mapTiles[tileNumber].ChangeAllBorders(effect);
        UpdateTileBorderSprites(tileNumber);
        UpdateBorders();
        UpdateMoveCostManager();
    }
    // Called during some battle passives.
    public void ChangeTile(int tileNumber, string effect, string specifics, bool force = false)
    {
        switch (effect)
        {
            case "Tile":
                ChangeTerrain(tileNumber, specifics, force);
                break;
            case "Elevation":
                ChangeTileElevation(tileNumber, int.Parse(specifics));
                break;
            case "TerrainEffect":
                ChangeTEffect(tileNumber, specifics, force);
                break;
            case "Spread":
                SpreadTerrainEffect(tileNumber, specifics);
                break;
            case "RSpread":
                RandomlySpreadTerrainEffect(tileNumber, specifics);
                break;
            case "ChainSpread":
                ChainSpreadTerrainEffect(tileNumber, specifics);
                break;
            case "ChainReplaceTEffect":
                ChainReplaceTEffects(tileNumber, specifics);
                break;
            case "SwitchBetween":
                string[] between = specifics.Split(">>");
                if (between.Length < 2){return;}
                if (mapInfo[tileNumber] == between[0])
                {
                    ChangeTerrain(tileNumber, between[1], true);
                }
                else if (mapInfo[tileNumber] == between[1])
                {
                    ChangeTerrain(tileNumber, between[0], true);
                }
                break;
            case "RandomTileSwap":
                // Switch tiles with a random adjacent tile.
                List<int> adjacent = mapUtility.AdjacentTiles(tileNumber, mapSize);
                if (adjacent.Count <= 1){return;}
                int random = adjacent[UnityEngine.Random.Range(0, adjacent.Count)];
                SwitchTile(tileNumber, random);
                break;
            case "Borders":
                mapTiles[tileNumber].SetBorders(specifics.Split("|").ToList());
                UpdateTileBorderSprites(tileNumber);
                break;
            // Direction Key?
            case "Border":
                break;
        }
        // Update the move cost manager, since any change might change tile move costs.
        UpdateMoveCostManager();
        UpdateMap();
    }
    public void ChangeTileElevation(int tileNumber, int newElevation)
    {
        mapElevations[tileNumber] = newElevation;
        mapTiles[tileNumber].SetElevation(mapElevations[tileNumber]);
        mapTiles[tileNumber].UpdateElevationSprite(elevationSprites.SpriteDictionary("E"+mapTiles[tileNumber].GetElevation().ToString()));
        UpdateMoveCostManager();
        UpdateMap();
    }
    protected void NewRandomTileElevation(int tileNumber)
    {
        if (tileNumber < 0 || tileNumber >= mapElevations.Count){return;}
        ChangeTileElevation(tileNumber, RandomElevation(mapInfo[tileNumber]));
    }
    // TESTED IN BATTLEMAPTESTER
    public void ChainReplaceTEffects(int tileNumber, string change)
    {
        List<int> connected = AllConnectedTerrain(tileNumber);
        for (int i = 0; i < connected.Count; i++)
        {
            ChangeTEffect(connected[i], change, true);
        }
    }
    public void ChangeTerrain(int tileNumber, string change, bool force = false)
    {
        if (mapInfo[tileNumber] == change){return;}
        if (force)
        {
            mapInfo[tileNumber] = change;
            NewRandomTileElevation(tileNumber);
            return;
        }
        string t_t = mapInfo[tileNumber] + "-" + change;
        // TESTED IN BATTLEMAPTESTER.
        string newInfo = tileTileInteractions.ReturnValue(t_t);
        if (newInfo == "")
        {
            mapInfo[tileNumber] = change;
        }
        else
        {
            mapInfo[tileNumber] = newInfo;
        }
        // Update the elevation.
        NewRandomTileElevation(tileNumber);
    }
    public int GetTileElevation(int tileNumber)
    {
        if (tileNumber < 0 || tileNumber >= mapTiles.Count){return 0;}
        return mapTiles[tileNumber].GetElevation();
    }
    public StatDatabase terrainTerrainInteractions;
    public List<string> terrainEffectTiles;
    public void SetTerrainEffectTiles(List<string> newInfo)
    {
        terrainEffectTiles = new List<string>(newInfo);
    }
    public string GetTerrainEffectOnTile(int tileNumber)
    {
        if (tileNumber < 0 || tileNumber >= terrainEffectTiles.Count){return "";}
        return terrainEffectTiles[tileNumber];
    }
    public void RemoveTerrainEffectOnTile(int tileNumber)
    {
        if (tileNumber < 0 || tileNumber >= terrainEffectTiles.Count){return;}
        terrainEffectTiles[tileNumber] = "";
        UpdateMap();
    }
    public virtual void GetNewTerrainEffects(List<string> featuresAndPatterns)
    {
        terrainEffectTiles = new List<string>(emptyList);
        for (int i = 0; i < featuresAndPatterns.Count; i++)
        {
            string[] fPSplit = featuresAndPatterns[i].Split(">>");
            if (fPSplit.Length < 2){continue;}
            terrainEffectTiles = mapMaker.AddFeature(terrainEffectTiles, fPSplit[0], fPSplit[1]);
        }
        UpdateMap();
    }
    public void ChangeTEffect(int tileNumber, string newEffect, bool force = false)
    {
        if (terrainEffectTiles[tileNumber] == newEffect){return;}
        if (force)
        {
            terrainEffectTiles[tileNumber] = newEffect;
            return;
        }
        if (terrainEffectTiles[tileNumber] == "" || newEffect == "")
        {
            terrainEffectTiles[tileNumber] = newEffect;
        }
        else
        {
            string t_t = terrainEffectTiles[tileNumber] + "-" + newEffect;
            string newValue = terrainTerrainInteractions.ReturnValue(t_t);
            if (newValue == "")
            {
                terrainEffectTiles[tileNumber] = newEffect;
            }
            else if (newValue.Contains("ChainReplace"))
            {
                string[] cRS = newValue.Split(">>");
                if (cRS.Length < 2)
                {
                    ChainReplaceTEffects(tileNumber, newEffect);
                }
                else
                {
                    ChainReplaceTEffects(tileNumber, cRS[1]);
                }
                return;
            }
            else
            {
                terrainEffectTiles[tileNumber] = newValue;
            }
        }
        UpdateMap();
    }
    public void SwitchTerrainEffect(int tile1, int tile2)
    {
        string temp = terrainEffectTiles[tile1];
        terrainEffectTiles[tile1] = terrainEffectTiles[tile2];
        terrainEffectTiles[tile2] = temp;
        UpdateMap();
    }
    public void SpreadTerrainEffect(int tileNumber, string sTEffect = "")
    {
        string tEffect = terrainEffectTiles[tileNumber];
        if (tEffect == ""){return;}
        if (sTEffect != "" && tEffect != sTEffect){return;}
        List<int> adjacent = mapUtility.AdjacentTiles(tileNumber, mapSize);
        for (int i = 0; i < adjacent.Count; i++)
        {
            ChangeTEffect(adjacent[i], tEffect);
        }
    }
    public void RandomlySpreadTerrainEffect(int tileNumber, string sTEffect = "")
    {
        string tEffect = terrainEffectTiles[tileNumber];
        if (tEffect == ""){return;}
        if (sTEffect != "" && tEffect != sTEffect){return;}
        List<int> adjacent = mapUtility.AdjacentTiles(tileNumber, mapSize);
        ChangeTEffect(adjacent[UnityEngine.Random.Range(0, adjacent.Count)], tEffect);
    }
    public List<int> AllConnectedTiles(int tileNumber)
    {
        List<int> connected = new List<int>();
        string tile = mapInfo[tileNumber];
        if (tile == "")
        {
            return connected;
        }
        List<int> viewedTiles = new List<int>();
        List<int> queuedTiles = new List<int>();
        viewedTiles.Add(tileNumber);
        connected.Add(tileNumber);
        queuedTiles.Add(tileNumber);
        while (queuedTiles.Count > 0)
        {
            int currentTile = queuedTiles[0];
            List<int> adjacentTiles = mapUtility.AdjacentTiles(currentTile, mapSize);
            for (int i = 0; i < adjacentTiles.Count; i++)
            {
                // Only look at new tiles.
                if (viewedTiles.Contains(adjacentTiles[i])){continue;}
                viewedTiles.Add(adjacentTiles[i]);
                if (mapInfo[adjacentTiles[i]] == tile)
                {
                    connected.Add(adjacentTiles[i]);
                    queuedTiles.Add(adjacentTiles[i]);
                }
            }
            queuedTiles.RemoveAt(0);
        }
        return connected;
    }
    public List<int> AllConnectedTerrain(int tileNumber)
    {
        List<int> connected = new List<int>();
        string tEffect = terrainEffectTiles[tileNumber];
        if (tEffect == "")
        {
            return connected;
        }
        List<int> viewedTiles = new List<int>();
        List<int> queuedTiles = new List<int>();
        viewedTiles.Add(tileNumber);
        connected.Add(tileNumber);
        queuedTiles.Add(tileNumber);
        while (queuedTiles.Count > 0)
        {
            int currentTile = queuedTiles[0];
            List<int> adjacentTiles = mapUtility.AdjacentTiles(currentTile, mapSize);
            for (int i = 0; i < adjacentTiles.Count; i++)
            {
                // Only look at new tiles.
                if (viewedTiles.Contains(adjacentTiles[i])){continue;}
                viewedTiles.Add(adjacentTiles[i]);
                if (terrainEffectTiles[adjacentTiles[i]] == tEffect)
                {
                    connected.Add(adjacentTiles[i]);
                    queuedTiles.Add(adjacentTiles[i]);
                }
            }
            queuedTiles.RemoveAt(0);
        }
        return connected;
    }
    public void ChainSpreadTerrainEffect(int tileNumber, string sTEffect = "")
    {
        string tEffect = terrainEffectTiles[tileNumber];
        if (tEffect != sTEffect){return;}
        // Get all connected tiles of the same teffect.
        List<int> connected = AllConnectedTerrain(tileNumber);
        if (connected.Count <= 0){return;}
        // Get all borders to the connected tiles.
        List<int> borders = mapUtility.AdjacentBorders(connected, mapSize);
        // Try to spread to all the border tiles.
        for (int i = 0; i < borders.Count; i++)
        {
            ChangeTEffect(borders[i], sTEffect);
        }
    }
    protected void UpdateTerrain()
    {
        if (!DisplayLayerExists(tEffectLayer)){return;}
        mapDisplayers[tEffectLayer].DisplayCurrentTiles(mapTiles, terrainEffectTiles, currentTiles);
    }
    public List<Interactable> interactables;
    public void RemoveInteractable(Interactable iAct)
    {
        interactables.Remove(iAct);
    }
    public void AddInteractable(Interactable iAct)
    {
        interactables.Add(iAct);
    }
    public Interactable GetInteractableOnTile(int tileNumber)
    {
        if (GetActorOnTile(tileNumber) != null){return null;}
        for (int i = 0; i < interactables.Count; i++)
        {
            if (interactables[i].GetLocation() == tileNumber)
            {
                return interactables[i];
            }
        }
        return null;
    }
    public List<Interactable> GetInteractablesOnTile(int tileNumber)
    {
        List<Interactable> inters = new List<Interactable>();
        for (int i = 0; i < interactables.Count; i++)
        {
            if (interactables[i].GetLocation() == tileNumber)
            {
                inters.Add(interactables[i]);
            }
        }
        return inters;
    }
    public void AttackInteractable(int tileNumber, TacticActor attacker)
    {
        List<Interactable> interactables = GetInteractablesOnTile(tileNumber);
        if (interactables.Count <= 0){return;}
        for (int i = 0; i < interactables.Count; i++)
        {
            interactables[i].AttackTrigger(this, attacker);
        }
    }
    public void EndTurnOnInteractable(TacticActor actor)
    {
        List<Interactable> interactables = GetInteractablesOnTile(actor.GetLocation());
        if (interactables.Count <= 0){return;}
        for (int i = 0; i < interactables.Count; i++)
        {
            interactables[i].EndTrigger(this, actor);
        }
    }
    public bool InteractableOnTile(int tileNumber)
    {
        for (int i = 0; i < interactables.Count; i++)
        {
            if (interactables[i].GetLocation() == tileNumber)
            {
                return true;
            }
        }
        return false;
    }
    public StatDatabase auraData;
    public List<AuraEffect> auras;
    public void ResetAuras()
    {
        auras.Clear();
    }
    public List<AuraEffect> ReturnActorAuras(TacticActor actor)
    {
        List<AuraEffect> actorAuras = new List<AuraEffect>();
        for (int i = 0; i < auras.Count; i++)
        {
            if (auras[i].AuraOwner(actor))
            {
                actorAuras.Add(auras[i]);
            }
        }
        return actorAuras;
    }
    public void RemoveAura(AuraEffect aura)
    {
        // TODO Trigger Delayed Auras Here.
        auraManager.TriggerRemoveAuraEffect(aura);
        auras.Remove(aura);
        aura = null;
    }
    public void UpdateAuraLocations()
    {
        for (int i = auras.Count - 1; i >= 0; i--)
        {
            auras[i].UpdateAuraLocation();
        }
    }
    // Later we can deal with adding custom auras.
    public void AddAura(TacticActor auraUser, int targetTile, string auraName, int duration)
    {
        // Check if the auraUser has this aura already, if so then increase the duration but don't remake it.
        List<AuraEffect> actorAuras = ReturnActorAuras(auraUser);
        for (int i = 0; i < actorAuras.Count; i++)
        {
            actorAuras[i].UpdateAuraLocation();
            if (actorAuras[i].GetAuraName() == auraName && actorAuras[i].GetLocation() == targetTile)
            {
                actorAuras[i].GainDuration(duration);
                return;
            }
        }
        AuraEffect newAura = new AuraEffect();
        newAura.InitializeAura(auraUser, targetTile, duration, auraData.ReturnValue(auraName));
        auras.Add(newAura);
    }
    public void AuraNextRound()
    {
        for (int i = auras.Count - 1; i >= 0; i--)
        {
            auras[i].NextRound(this);
        }
    }
    public void AuraActorEndsTurn(TacticActor actor)
    {
        for (int i = auras.Count - 1; i >= 0; i--)
        {
            auras[i].ActorEndsTurn(this, actor);
        }
    }
    public AuraManager auraManager;
    // This needs to differeniate between start/end/move/attack/skill.
    public void ApplyAuraEffects(TacticActor triggeringActor, string triggerType = "Move")
    {
        UpdateAuraLocations();
        auraManager.TriggerAllAuraEffects(auras, triggeringActor, triggerType);
    }
    public void HighlightAura(AuraEffect aura)
    {
        UpdateHighlights(aura.GetAuraTiles(this));
    }
    public void HighlightActorAuras(TacticActor actor)
    {
        List<int> auraTiles = new List<int>();
        // Highlight all auras tiles of the actor.
        for (int i = 0; i < auras.Count; i++)
        {
            // Only do auras owned by the actor.
            if (!auras[i].AuraOwner(actor)){continue;}
            auraTiles.AddRange(auras[i].GetAuraTiles(this));
        }
        // Display.
        UpdateHighlights(auraTiles);
    }
    public List<string> highlightedTiles;
    public ColorDictionary colorDictionary;
    protected virtual void GetActorTiles()
    {
        if (emptyList == null || emptyList.Count < mapSize * mapSize) { InitializeEmptyList(); }
        actorTiles = new List<string>(emptyList);
        actorDirections = new List<string>(emptyList);
        for (int i = 0; i < interactables.Count; i++)
        {
            actorTiles[interactables[i].GetLocation()] = interactables[i].GetSpriteName();
            actorDirections[interactables[i].GetLocation()] = "";
        }
        for (int i = 0; i < battlingActors.Count; i++)
        {
            // Just for fun make the actor not appear on the map.
            // They can still use movement to determine the actors location so it doesn't really do anything if you're paying the slightest amount of attention.
            if (battlingActors[i].invisible || battlingActors[i].GetLocation() < 0){continue;}
            actorTiles[battlingActors[i].GetLocation()] = battlingActors[i].GetSpriteName();
            actorDirections[battlingActors[i].location] = battlingActors[i].GetDirection().ToString();
        }
    }

    [ContextMenu("Clear Actors")]
    public void ClearActors()
    {
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i] == null) { continue; }
        }
        battlingActors.Clear();
    }

    public override void UpdateMap()
    {
        base.UpdateMap();
        UpdateBuildings();
        UpdateActors();
        UpdateTerrain();
    }

    public void UpdateActors()
    {
        GetActorTiles();
        if (!DisplayLayerExists(actorLayer)){return;}
        mapDisplayers[actorLayer].DisplayCurrentStyledTiles(mapTiles, actorTiles, currentTiles, true, actorDirections);
    }

    public List<string> buildingTiles;
    protected void GetBuildingTiles()
    {
        if (emptyList == null || emptyList.Count < mapSize * mapSize) { InitializeEmptyList(); }
        buildingTiles = new List<string>(emptyList);
        for (int i = 0; i < buildingLocations.Count; i++)
        {
            buildingTiles[buildingLocations[i]] = buildings[i];
        }
    }
    public void UpdateBuildings()
    {
        GetBuildingTiles();
        if (!DisplayLayerExists(buildingLayer)){return;}
        mapDisplayers[buildingLayer].DisplayCurrentTiles(mapTiles, buildingTiles, currentTiles);
    }
    public void UpdateMovingHighlights(TacticActor selectedActor, bool current = true)
    {
        if (emptyList.Count < mapSize * mapSize)
        {
            InitializeEmptyList();
        }
        highlightedTiles = new List<string>(emptyList);
        int maxActions = selectedActor.GetBaseActions();
        if (current) { maxActions = selectedActor.GetActions(); }
        for (int i = Mathf.Min(maxActions, colorDictionary.keys.Count - 2); i >= 0; i--)
        {
            UpdateHighlightsWithoutReseting(moveManager.GetReachableTilesBasedOnActions(selectedActor, battlingActors, i), colorDictionary.keys[i + 1]);
        }
    }

    public void UpdateMovingPath(TacticActor actor, int selectedTile)
    {
        List<string> originalHighlightedTiles = new List<string>(highlightedTiles);
        List<int> path = moveManager.GetPrecomputedPath(actor.GetLocation(), selectedTile);
        UpdateHighlightsWithoutReseting(path, "Red");
        highlightedTiles = new List<string>(originalHighlightedTiles);
    }

    protected void UpdateHighlightsWithoutReseting(List<int> newTiles, string colorKey = "MoveClose", int layer = 4)
    {
        if (!DisplayLayerExists(layer)){return;}
        string colorName = colorDictionary.GetColorNameByKey(colorKey);
        for (int i = 0; i < newTiles.Count; i++)
        {
            highlightedTiles[newTiles[i]] = colorName;
        }
        mapDisplayers[layer].HighlightCurrentTiles(mapTiles, highlightedTiles, currentTiles);
    }

    protected void HighlightTileWithoutReseting(int newTile, string colorKey, int layer = 4)
    {
        if (!DisplayLayerExists(layer)){return;}
        string colorName = colorDictionary.GetColorNameByKey(colorKey);
        highlightedTiles[newTile] = colorName;
        mapDisplayers[layer].HighlightCurrentTiles(mapTiles,highlightedTiles, currentTiles);
    }

    public void UpdateStartingPositionTiles(string pattern, int selectedTile = -1, List<int> overrideStartingPositions = null)
    {
        int startTileCount = mapSize * mapSize / 2 - (mapSize * mapSize / 2) % mapSize;
        List<int> startingTiles = mapPatterns.ReturnTilesOfPattern(pattern, startTileCount, mapSize);
        if (overrideStartingPositions != null && overrideStartingPositions.Count > 1)
        {
            startingTiles = overrideStartingPositions;
        }
        UpdateHighlights(startingTiles, "Green", 4);
        if (selectedTile >= 0)
        {
            HighlightTileWithoutReseting(selectedTile, "Blue", 4);
        }
    }

    public void UpdateHighlights(List<int> newTiles, string colorKey = "MoveClose", int layer = 4)
    {
        if (!DisplayLayerExists(layer)){return;}
        string colorName = colorDictionary.GetColorNameByKey(colorKey);
        if (emptyList.Count < mapSize * mapSize) { InitializeEmptyList(); }
        highlightedTiles = new List<string>(emptyList);
        for (int i = 0; i < newTiles.Count; i++)
        {
            if (newTiles[i] < 0){continue;}
            highlightedTiles[newTiles[i]] = colorName;
        }
        mapDisplayers[layer].HighlightCurrentTiles(mapTiles, highlightedTiles, currentTiles);
    }

    [ContextMenu("Reset Highlights")]
    public void ResetHighlights()
    {
        if (emptyList.Count < mapSize * mapSize) { InitializeEmptyList(); }
        if (!DisplayLayerExists(highlightLayer)){return;}
        highlightedTiles = new List<string>(emptyList);
        mapDisplayers[highlightLayer].HighlightCurrentTiles(mapTiles, highlightedTiles, currentTiles);
        mapDisplayers[highlightLayer + 1].HighlightCurrentTiles(mapTiles, highlightedTiles, currentTiles);
    }
    public TacticActor GetActorOnTile(int tileNumber, bool includeInvisible = true)
    {
        return battleMapUtility.GetActorOnTile(this, tileNumber, includeInvisible);
    }
    public TacticActor GetActorByIndex(int index)
    {
        if (index < 0 || index >= battlingActors.Count) { return null; }
        return battlingActors[index];
    }
    public string GetTileInfoOfActor(TacticActor actor)
    {
        return mapInfo[actor.GetLocation()];
    }
    public bool TileTypeExists(string tileType)
    {
        return mapInfo.Contains(tileType);
    }
    public int ReturnClosestTileOfType(TacticActor actor, string tileType)
    {
        return battleMapUtility.ReturnClosestTileOfType(this, actor, tileType);
    }
    // Override this to ignore tiles that have actors on them and then give the next closest tile.
    public override int ReturnClosestTileWithinElevationDifference(int start, int end, int maxElvDiff, List<int> moveCosts)
    {
        List<int> adjacentTiles = mapUtility.AdjacentTiles(end, mapSize);
        int target = end;
        int dist = mapSize * mapSize;
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            // Ignore those with elevation difference too large.
            if (ReturnElevationDifference(adjacentTiles[i], end) > maxElvDiff || GetActorOnTile(adjacentTiles[i]) != null)
            {
                continue;
            }
            if (moveCosts[adjacentTiles[i]] < dist)
            {
                dist = moveCosts[adjacentTiles[i]];
                target = adjacentTiles[i];
            }
        }
        return target;
    }
    public int ReturnClosestTileWithLineOfSight(int start, int end, int attackRange, List<int> moveCosts)
    {
        int target = end;
        int dist = mapSize * mapSize;
        for (int i = 0; i < mapInfo.Count; i++)
        {
            // Ignore those without line of sight.
            if (!LineOfSightBetweenTiles(i, end, attackRange))
            {
                continue;
            }
            if (moveCosts[i] < dist)
            {
                dist = moveCosts[i];
                target = i;
            }
        }
        return target;
    }
    public List<string> excludedTileTypesForNonFlying;
    public bool TileExcluded(TacticActor actor, string tile)
    {
        if (actor.GetMoveType() == "Flying"){return false;}
        return excludedTileTypesForNonFlying.Contains(tile);
    }
    public bool TileSandwiched(TacticActor actor, string tileType)
    {
        return battleMapUtility.TileSandwiched(this, actor, tileType);
    }
    public bool TileSandwichable(TacticActor actor, string tileType)
    {
        return ReturnClosestTileSandwiched(actor, tileType) >= 0;
    }
    public int ReturnClosestTileSandwiched(TacticActor actor, string tileType)
    {
        return battleMapUtility.ReturnClosestTileSandwiched(this, actor, tileType);
    }
    public bool SandwichedByTarget(TacticActor actor, string tileType)
    {
        if (actor.GetTarget() == null || actor.GetTarget().GetHealth() <= 0 || actor.GetTarget().invisible){return false;}
        int location = actor.GetLocation();
        int targetLoc = actor.GetTarget().GetLocation();
        if (!mapUtility.TilesAdjacent(location, targetLoc, mapSize)){return false;}
        int direction = mapUtility.DirectionBetweenLocations(targetLoc, location, mapSize);
        int sandwichingPoint = mapUtility.PointInDirection(location, direction, mapSize);
        if (sandwichingPoint < 0){return false;}
        return mapInfo[sandwichingPoint].Contains(tileType);
    }
    public bool TargetSandwiched(TacticActor actor, string tileType)
    {
        if (actor.GetTarget() == null || actor.GetTarget().GetHealth() <= 0 || actor.GetTarget().invisible){return false;}
        int location = actor.GetLocation();
        int targetLoc = actor.GetTarget().GetLocation();
        if (!mapUtility.TilesAdjacent(location, targetLoc, mapSize)){return false;}
        int direction = mapUtility.DirectionBetweenLocations(location, targetLoc, mapSize);
        int sandwichingPoint = mapUtility.PointInDirection(targetLoc, direction, mapSize);
        if (sandwichingPoint < 0){return false;}
        return mapInfo[sandwichingPoint].Contains(tileType);
    }
    public bool TargetSandwichable(TacticActor actor, string tileType)
    {
        return ReturnClosestSandwichTargetBetweenTileOfType(actor, tileType) >= 0;
    }
    public int ReturnClosestSandwichTargetBetweenTileOfType(TacticActor actor, string tileType)
    {
        List<int> tiles = new List<int>();
        if (actor.GetTarget() == null || actor.GetTarget().GetHealth() <= 0 || actor.GetTarget().invisible){return -1;}
        int targetLocation = actor.GetTarget().GetLocation();
        List<int> adjacentTiles = mapUtility.AdjacentTiles(targetLocation, mapSize);
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            // Check if the target is adjacent to any of the requested tile types.
            if (mapInfo[adjacentTiles[i]].Contains(tileType))
            {
                // Check the opposite tile and make sure it's empty and valid.
                int sandwichingPoint = mapUtility.PointInOppositeDirection(targetLocation, adjacentTiles[i], mapSize);
                // Can't be out of bounds, excluded or have an actor on it.
                if (sandwichingPoint < 0 || TileExcluded(actor, mapInfo[sandwichingPoint]) || GetActorOnTile(sandwichingPoint) != null)
                {
                    continue;
                }
                tiles.Add(sandwichingPoint);
            }
        }
        return battleMapUtility.ReturnLowestMoveCostTile(this, actor, tiles);
    }
    public string GetTileEffectOfActor(TacticActor actor)
    {
        return terrainEffectTiles[actor.GetLocation()];
    }
    public List<TacticActor> GetActorsOnTiles(List<int> tiles)
    {
        return battleMapUtility.GetActorsOnTiles(this, tiles);
    }
    public List<TacticActor> GetAdjacentActors(int tileNumber)
    {
        return GetActorsOnTiles(mapUtility.AdjacentTiles(tileNumber, mapSize));
    }
    public bool TileInAttackRange(TacticActor actor, int tileIndex)
    {
        List<int> attackable = GetAttackableTiles(actor);
        return attackable.Contains(tileIndex);
    }
    // Melees have to deal with elevation differences, ranged needs to deal with line of sight. Obviously ranged is still superior since you can always attack all adjacent tiles at least.
    public List<int> GetAttackableTiles(TacticActor actor, bool current = true, int startTile = -1)
    {
        int range = actor.GetAttackRange();
        if (current)
        {
            startTile = actor.GetLocation();
        }
        List<int> attackable = mapUtility.GetTilesInCircleShape(startTile, range, mapSize);
        attackable.Remove(startTile);
        // Melee attacks can't attack if the height difference is too large.
        if (range <= 1)
        {
            for (int i = attackable.Count - 1; i >= 0; i--)
            {
                // Elevation difference of 3 or more means melee basic attacks are ineffective.
                if (ReturnElevationDifference(startTile, attackable[i]) > actor.GetWeaponReach())
                {
                    attackable.RemoveAt(i);
                }
            }
        }
        // Ranged Attacks.
        else
        {
            for (int i = attackable.Count - 1; i >= 0; i--)
            {
                if (!LineOfSightBetweenTiles(startTile, attackable[i], range))
                {
                    attackable.RemoveAt(i);
                }
            }
        }
        return attackable;
    }
    public bool LineOfSightBetweenTiles(int start, int target, int range = 2)
    {
        // If the tiles are adjacent then return true.
        if (mapUtility.DistanceBetweenTiles(start, target, mapSize) <= 1){return true;}
        // If tile is out of range return false.
        if (mapUtility.DistanceBetweenTiles(start, target, mapSize) > range){return false;}
        int startElevation = GetTileElevation(start);
        // Get all the tiles inbetween the start and the target.
        List<int> tilesBetween = mapUtility.ShortestLineBetweenPoints(start, target, mapSize);
        for (int i = 0; i < tilesBetween.Count; i++)
        {
            // If there is a tile, elevation, teffect, border or building that blocks LoS then return false.
            if (TileBlocksLineOfSight(tilesBetween[i], startElevation)){return false;}
        }
        return true;
    }
    public bool TileBlocksLineOfSight(int tile, int startElevation)
    {
        if (GetTileElevation(tile) > startElevation){return true;}
        // Some teffects also block line of sight?
        string tEffect = GetTerrainEffectOnTile(tile);
        if (tEffect == "Fog" || tEffect == "Toxic Mist"){return true;}
        return false;
    }
    public void UpdateSelectedAttackTile(TacticActor actor, int selectedTile)
    {
        List<int> attackable = GetAttackableTiles(actor);
        UpdateHighlights(attackable, "Attack");
        if (!attackable.Contains(selectedTile)){return;}
        List<int> selected = new List<int>();
        selected.Add(selectedTile);
        UpdateHighlightsWithoutReseting(selected, "Green");
    }
    public List<TacticActor> GetAttackableEnemies(TacticActor actor)
    {
        List<TacticActor> attackableEnemies = GetActorsOnTiles(GetAttackableTiles(actor));
        for (int i = attackableEnemies.Count - 1; i >= 0; i--)
        {
            if (attackableEnemies[i].GetTeam() == actor.GetTeam())
            {
                attackableEnemies.RemoveAt(i);
            }
        }
        return attackableEnemies;
    }
    public List<int> GetShootableTiles(TacticActor actor, int shootingRange = 2)
    {
        List<int> attackable = mapUtility.GetTilesInCircleShape(actor.GetLocation(), shootingRange, mapSize);
        attackable.Remove(actor.GetLocation());
        return attackable;
    }
    public bool ShootableEnemies(TacticActor actor, int shootingRange = 2)
    {
        List<TacticActor> attackableEnemies = GetActorsOnTiles(GetShootableTiles(actor, shootingRange));
        for (int i = attackableEnemies.Count - 1; i >= 0; i--)
        {
            if (attackableEnemies[i].GetTeam() == actor.GetTeam())
            {
                attackableEnemies.RemoveAt(i);
            }
        }
        return attackableEnemies.Count > 0;
    }
    public int GetRandomEnemyLocation(TacticActor actor, List<int> targetedTiles)
    {
        return battleMapUtility.GetRandomEnemyLocation(actor, targetedTiles, this);
    }
    public int GetRandomAllyLocation(TacticActor actor, List<int> targetedTiles)
    {
        return battleMapUtility.GetRandomAllyLocation(actor, targetedTiles, this);
    }
    public int DirectionBetweenActors(TacticActor actor1, TacticActor actor2)
    {
        return mapUtility.DirectionBetweenLocations(actor1.GetLocation(), actor2.GetLocation(), mapSize);
    }
    public int DirectionBetweenActorAndLocation(TacticActor actor, int location)
    {
        return mapUtility.DirectionBetweenLocations(actor.GetLocation(), location, mapSize);
    }
    public int DistanceBetweenActors(TacticActor actor1, TacticActor actor2)
    {
        if (actor1 == null || actor2 == null)
        {
            return mapSize * mapSize + 1;
        }
        return mapUtility.DistanceBetweenTiles(actor1.GetLocation(), actor2.GetLocation(), mapSize);
    }
    public bool StraightLineBetweenActors(TacticActor actor1, TacticActor actor2)
    {
        if (actor1 == null || actor2 == null)
        {
            return false;
        }
        return mapUtility.StraightLineBetweenPoints(actor1.GetLocation(), actor2.GetLocation(), mapSize);
    }
    public TacticActor GetFurthestActor(TacticActor actor, List<TacticActor> otherActors)
    {
        if (otherActors.Count <= 0){return null;}
        int index = 0;
        int distance = 0;
        for (int i = 0; i < otherActors.Count; i++)
        {
            if (otherActors[i] == actor){continue;}
            if (DistanceBetweenActors(actor, otherActors[i]) > distance)
            {
                distance = DistanceBetweenActors(actor, otherActors[i]);
                index = i;
            }
        }
        return otherActors[index];
    }
    public TacticActor GetClosestActor(TacticActor actor, List<TacticActor> otherActors)
    {
        if (otherActors.Count <= 0){return null;}
        int index = 0;
        int distance = mapSize * 2;
        for (int i = 0; i < otherActors.Count; i++)
        {
            if (otherActors[i] == actor){continue;}
            if (DistanceBetweenActors(actor, otherActors[i]) < distance)
            {
                distance = DistanceBetweenActors(actor, otherActors[i]);
                index = i;
            }
        }
        return otherActors[index];
    }
    public TacticActor GetClosestEnemy(TacticActor actor)
    {
        List<TacticActor> enemies = AllEnemies(actor);
        return GetClosestActor(actor, enemies);
    }
    public int GetClosestEmptyTile(TacticActor actor)
    {
        int location = actor.GetLocation();
        if (actor.GetHealth() < 0){return location;}
        List<int> adjacentEmpty = GetAdjacentEmptyTiles(location);
        if (adjacentEmpty.Count > 0)
        {
            return adjacentEmpty[UnityEngine.Random.Range(0, adjacentEmpty.Count)];
        }
        int distance = mapSize;
        int tile = -1;
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            if (!TileNotEmpty(i))
            {
                int newDist = mapUtility.DistanceBetweenTiles(i, location, mapSize);
                if (newDist < distance)
                {
                    distance = newDist;
                    tile = i;
                }
            }
        }
        return tile;
    }
    public List<int> GetAdjacentEmptyTiles(int tileNumber)
    {
        List<int> allAdjacent = mapUtility.AdjacentTiles(tileNumber, mapSize);
        for (int i = allAdjacent.Count - 1; i >= 0; i--)
        {
            if (TileNotEmpty(allAdjacent[i]))
            {
                allAdjacent.RemoveAt(i);
            }
        }
        return allAdjacent;
    }
    public int ReturnRandomAdjacentEmptyTile(int tileNumber)
    {
        List<int> emptyAdjacent = GetAdjacentEmptyTiles(tileNumber);
        if (emptyAdjacent.Count == 0)
        {
            return -1;
        }
        int tile = emptyAdjacent[UnityEngine.Random.Range(0, emptyAdjacent.Count)];
        if (TileNotEmpty(tile))
        {
            return -1;
        }
        return tile;
    }
    public int ReturnTileInRelativeDirection(TacticActor actor, int relativeDirection)
    {
        int direction = (actor.GetDirection() + relativeDirection) % 6;
        return mapUtility.PointInDirection(actor.GetLocation(), direction, mapSize);
    }
    public bool AlliesInTiles(TacticActor actor, List<int> tiles)
    {
        int team = actor.GetTeam();
        for (int i = 0; i < tiles.Count; i++)
        {
            TacticActor tileActor = GetActorOnTile(tiles[i]);
            if (tileActor == null)
            {
                continue;
            }
            if (tileActor.GetTeam() == team)
            {
                return true;
            }
        }
        return false;
    }
    // Used By AutoBattle Healers
    public List<TacticActor> ReturnHurtAlliesInTiles(TacticActor actor, List<int> tiles)
    {
        List<TacticActor> actors = ReturnAlliesInTiles(actor, tiles);
        for (int i = actors.Count - 1; i >= 0; i--)
        {
            if (actors[i].GetHealth() >= actors[i].GetBaseHealth())
            {
                actors.RemoveAt(i);
            }
        }
        return actors;
    }
    public List<TacticActor> ReturnAlliesInTiles(TacticActor actor, List<int> tiles)
    {
        int team = actor.GetTeam();
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < tiles.Count; i++)
        {
            TacticActor tileActor = GetActorOnTile(tiles[i]);
            if (tileActor == null)
            {
                continue;
            }
            if (tileActor.GetTeam() == team)
            {
                actors.Add(tileActor);
            }
        }
        return actors;
    }
    public bool EnemiesInTiles(TacticActor actor, List<int> tiles)
    {
        int team = actor.GetTeam();
        for (int i = 0; i < tiles.Count; i++)
        {
            TacticActor tileActor = GetActorOnTile(tiles[i]);
            if (tileActor == null)
            {
                continue;
            }
            if (tileActor.GetTeam() != team)
            {
                return true;
            }
        }
        return false;
    }
    // Normally Used For Attack Targeting, So Don't Hit Invisible.
    public List<TacticActor> ReturnEnemiesInTiles(TacticActor actor, List<int> tiles, bool includeInvisible = false)
    {
        int team = actor.GetTeam();
        List<TacticActor> actors = new List<TacticActor>();
        for (int i = 0; i < tiles.Count; i++)
        {
            TacticActor tileActor = GetActorOnTile(tiles[i], includeInvisible);
            if (tileActor == null)
            {
                continue;
            }
            if (tileActor.GetTeam() != team)
            {
                actors.Add(tileActor);
            }
        }
        return actors;
    }
    public List<TacticActor> GetAdjacentAllies(TacticActor actor)
    {
        List<TacticActor> all = new List<TacticActor>();
        if (actor == null){return all;}
        all = GetAdjacentActors(actor.GetLocation());
        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (all[i].GetTeam() != actor.GetTeam()) { all.RemoveAt(i); }
        }
        return all;
    }
    // Currently Only Used By Aegir Consumption.
    // Due To Flipping, It Is Possible For Actors To Overlap During This Step.
    public TacticActor GetActorInFrontActor(TacticActor actor, bool aegir = true)
    {
        int location = actor.GetLocation();
        int direction = actor.GetDirection();
        int target = mapUtility.PointInDirection(location, direction, mapSize);
        if (target < 0){return null;}
        if (!aegir)
        {
            return GetActorOnTile(target);
        }
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetTeam() != actor.GetTeam()){continue;}
            if (battlingActors[i].GetLocation() == target)
            {
                return battlingActors[i];
            }
        }
        return null;
    }
    // From The Actors POV
    public TacticActor GetActorBehindActor(TacticActor actor)
    {
        int location = actor.GetLocation();
        int direction = (actor.GetDirection() + 3) % 6;
        int target = mapUtility.PointInDirection(location, direction, mapSize);
        if (target < 0){return null;}
        return GetActorOnTile(target);
    }
    // Less Strict For PVP
    public List<TacticActor> GetActorsBehindActor(TacticActor actor)
    {
        int location = actor.GetLocation();
        int direction1 = (actor.GetDirection() + 3) % 6;
        int direction2 = (actor.GetDirection() + 2) % 6;
        int direction3 = (actor.GetDirection() + 4) % 6;
        int target1 = mapUtility.PointInDirection(location, direction1, mapSize);
        int target2 = mapUtility.PointInDirection(location, direction2, mapSize);
        int target3 = mapUtility.PointInDirection(location, direction3, mapSize);
        List<int> targets = new List<int>();
        targets.Add(target1);
        targets.Add(target2);
        targets.Add(target3);
        List<TacticActor> behindActors = new List<TacticActor>();
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] < 0){continue;}
            TacticActor targetActor = GetActorOnTile(targets[i]);
            if (targetActor == null){continue;}
            behindActors.Add(targetActor);
        }
        return behindActors;
    }
    // The tankiest adjacent/closest guarding ally will take the hit for you.
    public TacticActor GetGuardingAlly(TacticActor target, TacticActor attacker)
    {
        List<TacticActor> adjAllies = GetAdjacentAllies(target);
        // If melee then need to be in range of both, else just be adjacent to the target.
        if (DistanceBetweenActors(target, attacker) <= 1)
        {
            adjAllies = AdjustForMeleeGuardRange(adjAllies, attacker);
        }
        int defenseValue = -1;
        int index = -1;
        for (int i = 0; i < adjAllies.Count; i++)
        {
            if (adjAllies[i].Guarding() && adjAllies[i].GetDefense() > defenseValue && adjAllies[i].GetHealth() > 0)
            {
                index = i;
                defenseValue = adjAllies[i].GetDefense();
            }
        }
        if (index < 0)
        {
            return GetGuardingAllyInRange(target, attacker);
        }
        return adjAllies[index];
    }
    protected List<TacticActor> AdjustForMeleeGuardRange(List<TacticActor> allies, TacticActor attacker)
    {
        for (int i = allies.Count - 1; i >= 0; i--)
        {
            if (!allies[i].Guarding() || DistanceBetweenActors(attacker, allies[i]) > allies[i].GetGuardRange())
            {
                allies.RemoveAt(i);
            }
        }
        return allies;
    }
    // Any guard that is in guard range, only if there are no adjacent guards.
    protected TacticActor GetGuardingAllyInRange(TacticActor target, TacticActor attacker)
    {
        List<TacticActor> allies = AllAllies(target);
        // If melee then need to be in range of both, else just have target in guard range.
        if (DistanceBetweenActors(target, attacker) <= 1)
        {
            allies = AdjustForMeleeGuardRange(allies, attacker);
        }
        int index = -1;
        int defense = -1;
        for (int i = 0; i < allies.Count; i++)
        {
            if (allies[i].Guarding() && DistanceBetweenActors(target, allies[i]) <= allies[i].GetGuardRange())
            {
                if (allies[i].GetHealth() > 0 && allies[i].GetDefense() > defense)
                {
                    index = i;
                    defense = allies[i].GetDefense();
                }
            }
        }
        if (index < 0){return null;}
        return allies[index];
    }

    public List<TacticActor> GetAdjacentEnemies(TacticActor actor)
    {
        List<TacticActor> all = new List<TacticActor>();
        all = GetAdjacentActors(actor.GetLocation());
        for (int i = all.Count - 1; i >= 0; i--)
        {
            if (all[i].GetTeam() == actor.GetTeam()){all.RemoveAt(i);}
        }
        return all;
    }
    // MOVEMENT UTILITIES.
    public void GrabSkill(TacticActor grabber, string target, string specifics)
    {
        int targetTile = mapUtility.PointInDirection(grabber.GetLocation(), grabber.GetDirection(), mapSize);
        if (targetTile < 0) return;
        if (GetActorOnTile(targetTile) != null){return;}
        TacticActor targetActor = FindActorByTargetSpecifics(grabber, target, specifics);
        if (targetActor == null){return;}
        MoveActorToTile(targetActor, targetTile, true);
    }
    public void DashToSkill(TacticActor dasher, string target, string specifics)
    {
        TacticActor targetActor = FindActorByTargetSpecifics(dasher, target, specifics);
        if (targetActor == null){return;}
        int targetTile = GetClosestEmptyTile(targetActor);
        if (targetTile < 0) return;
        MoveActorToTile(dasher, targetTile, true);
    }
    public void DisplaceSkill(TacticActor displacer, List<int> targetedTiles, string displaceType, int force)
    {
        battleMapUtility.DisplaceSkill(displacer, targetedTiles, displaceType, force, this);
    }
    public void DisplaceDamage(TacticActor actor, int force, int nextTile, bool actorCollide = false, TacticActor actor2 = null, bool buildingCollide = false)
    {
        int displaceDamage = Mathf.Max(actor.GetWeight(), 1) * force * 6;
        int collideDamage = actor.TakeEffectDamage(displaceDamage);
        if (buildingCollide)
        {
            UpdateCombatLog(actor.GetPersonalName() + " collides with a " + GetBuildingOnTile(nextTile) + " and takes " + collideDamage + " damage.");
        }
        else if (actorCollide)
        {
            UpdateCombatLog(actor.GetPersonalName() + " collides with " + actor2.GetPersonalName() + " and takes " + collideDamage + " damage.");
            collideDamage = actor2.TakeEffectDamage(displaceDamage);
            UpdateCombatLog(actor2.GetPersonalName() + " takes " + collideDamage + " damage.");
        }
        else
        {
            UpdateCombatLog(actor.GetPersonalName() + " collides with a " + mapInfo[nextTile] + " and takes " + collideDamage + " damage.");
        }
    }
    public void FallingDamage(TacticActor actor, int fallHeight)
    {
        //Flyers don't take falling damage.
        if (actor.GetMoveType() == "Flying"){return;}
        int potentiaFallDamage = Mathf.Max(actor.GetWeight(), 1) * fallHeight * fallHeight * 6;
        int fallDamage = actor.TakeEffectDamage(potentiaFallDamage);
        UpdateCombatLog(actor.GetPersonalName() + " takes " + fallDamage + " damage from falling.");
    }
    public void CommandMovement(TacticActor actor, bool forward = true)
    {
        int dir = actor.GetDirection();
        if (!forward)
        {
            dir = (dir + 3) % 6;
        }
        int newTile = mapUtility.PointInDirection(actor.GetLocation(), dir, mapSize);
        MoveActorToTile(actor, newTile);
    }
    public bool TeleportToTarget(TacticActor mover, TacticActor target, string direction)
    {
        return battleMapUtility.TeleportToTarget(mover, target, direction, this);
    }
    public void MoveSkill(TacticActor mover, int targetTile, string moveDirection, int distance)
    {
        battleMapUtility.MoveSkill(mover, targetTile, moveDirection, distance, this);
    }
    public void MoveThroughSkill(TacticActor mover, int tile)
    {
        battleMapUtility.MoveThroughSkill(mover, tile, this);
    }
    // ALL Movement Should Go Through Here.
    public void AIMoveActorToTile(TacticActor actor, int tile, bool forced = false)
    {
        // Extra safety check, since you can never be too safe?
        if (tile < 0){return;}
        if (!forced)
        {
            actor.SetDirection(mapUtility.DirectionBetweenLocations(actor.GetLocation(), tile, mapSize));
        }
        actor.SetLocation(tile);
        ApplyMovePassiveEffects(actor, tile);
    }
    public void MoveActorToTile(TacticActor actor, int tile, bool forced = false)
    {
        AIMoveActorToTile(actor, tile, forced);
        UpdateMap();
    }
    public void ApplyMovePassiveEffects(TacticActor actor, int location)
    {
        // Updates the combat log when moving.
        ApplyMovingTileEffect(actor, location);
        List<string> movingPassives = actor.GetMovingPassives();
        List<string> passiveInfo = new List<string>();
        //int location = mover.GetLocation();
        for (int i = 0; i < movingPassives.Count; i++)
        {
            passiveInfo = movingPassives[i].Split("|").ToList();
            if (passiveInfo.Count < 6) { continue; }
            // Only apply passives that affect the user or the map.
            switch (passiveInfo[3])
            {
                case "Self":
                    if (passiveEffect.CheckMovingCondition(actor, passiveInfo[1], passiveInfo[2], location, this))
                    {
                        passiveEffect.AffectActor(actor, passiveInfo[4], passiveInfo[5]);
                    }
                    break;
                case "MoveCost":
                    break;
                case "Map":
                    if (passiveEffect.CheckMovingCondition(actor, passiveInfo[1], passiveInfo[2], location, this))
                    {
                        passiveEffect.AffectMap(actor, passiveInfo[4], passiveInfo[5], this);
                    }
                    break;
            }
        }
    }
    public void MoveActor(TacticActor actor, string effect, string specifics)
    {
        int tile = -1;
        int start = actor.GetLocation();
        switch (effect)
        {
            default:
            break;
            case "DirMove":
            int direction = int.Parse(specifics);
            tile = mapUtility.PointInDirection(start, direction, mapSize);
            if (tile == start || tile < 0){return;}
            if (GetActorOnTile(tile) != null){return;}
            MoveActorToTile(actor, tile);
            break;
        }
    }
    // ALL Movement Should Go Through Here. Thus update the combat log here, the actor's direction should be updated before this is called, based on how the actor moved into the tile.
    public bool ApplyMovingTileEffect(TacticActor actor, int tileNumber)
    {
        UpdateCombatLog(actor.GetPersonalName() + " moves to " + mapUtility.GetRowColumnCoordinateString(tileNumber, mapSize));
        actor.UpdateRoundMoveTracker();
        if (moveManager != null)
        {
            UpdateCombatLog("Move Cost: " + moveManager.MoveCostOfTile(tileNumber), true);
        }
        ApplyTileMovingEffect(actor, tileNumber);
        ApplyTerrainMovingEffect(actor, tileNumber);
        ApplyBorderMovingEffect(actor, tileNumber); // Direction Is Stored In The Actor?
        ApplyBuildingMovingEffect(actor, tileNumber);
        ApplyInteractableEffect(actor, tileNumber);
        // Check if the actor moved into any auras.
        // Check if the actor's aura moved into any actors.
        ApplyAuraEffects(actor);
        // You can die while moving now, due to auras dealing damage.
        if (actor.GetHealth() <= 0)
        {
            actor.ResetActions();
            actor.movement = 0;
        }
        return false;
    }
    protected void ApplyTileMovingEffect(TacticActor actor, int tileNumber)
    {
        List<string> movingPassives = ReturnTileMovingPassive(actor);
        ApplyMovingPassiveEffects(movingPassives, actor, tileNumber);
    }
    protected void ApplyTerrainMovingEffect(TacticActor actor, int tileNumber)
    {
        // Get the terrain info.
        string terrainEffect = terrainEffectTiles[tileNumber];
        if (terrainEffect.Length < 1) { return; }
        List<string> movingPassives = terrainEffectData.ReturnMovingPassive(terrainEffect);
        ApplyMovingPassiveEffects(movingPassives, actor, tileNumber);
    }
    protected void ApplyBuildingMovingEffect(TacticActor actor, int tileNumber)
    {
        string building = GetBuildingOnTile(actor.GetLocation());
        if (building.Length < 1) { return; }
        List<string> movingPassives = buildingEffectData.ReturnMovingPassive(building);
        ApplyMovingPassiveEffects(movingPassives, actor, tileNumber);
    }
    protected void ApplyInteractableEffect(TacticActor actor, int tileNumber)
    {
        List<Interactable> triggered = GetInteractablesOnTile(tileNumber);
        if (triggered.Count <= 0){return;}
        for (int i = 0; i < triggered.Count; i++)
        {
            triggered[i].MoveTrigger(this, actor);
        }
    }
    public void NextRound()
    {
        // Apply weather/tile to terrain effects.
        string t_w = "";
        string t_t = "";
        List<int> spreadingEffects = new List<int>();
        List<int> expandingEffects = new List<int>();
        List<int> removedEffects = new List<int>();
        for (int i = 0; i < terrainEffectTiles.Count; i++)
        {
            if (terrainEffectTiles[i] == ""){ continue; }
            t_w = terrainEffectTiles[i] + "-" + GetWeather();
            t_t = mapInfo[i] + "-" + terrainEffectTiles[i];
            if (terrainWeatherInteractions.ReturnValue(t_w) == "Remove" || terrainTileInteractions.ReturnValue(t_t) == "Remove")
            {
                removedEffects.Add(i);
                continue;
            }
            else if (terrainWeatherInteractions.ReturnValue(t_w) == "Expand" || terrainTileInteractions.ReturnValue(t_t) == "Expand")
            {
                expandingEffects.Add(i);
                continue;
            }
            else if (terrainWeatherInteractions.ReturnValue(t_w) == "Spread" || terrainTileInteractions.ReturnValue(t_t) == "Spread")
            {
                spreadingEffects.Add(i);
                continue;
            }
        }
        for (int i = 0; i < spreadingEffects.Count; i++)
        {
            RandomlySpreadTerrainEffect(spreadingEffects[i]);
        }
        for (int i = 0; i < expandingEffects.Count; i++)
        {
            SpreadTerrainEffect(expandingEffects[i]);
        }
        // Apply weather effects to tiles.
        for (int i = 0; i < mapInfo.Count; i++)
        {
            t_w = mapInfo[i] + "-" + GetWeather();
            string eAndS = tileWeatherInteractions.ReturnValue(t_w);
            string[] blocks = eAndS.Split("-");
            if (blocks.Length < 2) { continue; }
            switch (blocks[0])
            {
                case "Tile":
                    ChangeTerrain(i, blocks[1]);
                    break;
                case "Feature":
                    ChangeTEffect(i, blocks[1]);
                    break;
            }
        }
        for (int i = 0; i < removedEffects.Count; i++)
        {
            ChangeTEffect(removedEffects[i], "");
        }
        AuraNextRound();
        UpdateMap();
    }
}
