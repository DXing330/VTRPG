using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Very Similar To The Battle Map.
// Use Start/End For Tile/Effect/Weather/Etc. Each Turn.
public class MysteryDungeonBattleMap : BattleMap
{
    protected override void ResetAll()
    {
        base.ResetAll();
        revealLayout = false;
        revealTraps = false;
        revealItems = false;
        revealEnemies = false;
        revealStairs = false;
    }
    [Header("Mystery Dungeon Map Stats")]
    public bool updateMap = true;
    public int floor = 0;
    public void SetFloor(int newFloor){floor = newFloor;}
    public void NewFloor(){floor++;}
    public string dungeonType = "Forest";
    public void SetDungeonType(string newType){dungeonType = newType;}
    public bool revealLayout = false;
    public bool revealTraps = false;
    public bool revealItems = false;
    public bool revealEnemies = false;
    public bool revealStairs = false;
    public override void Reveal(string specifics)
    {
        switch (specifics)
        {
            case "All":
            revealLayout = true;
            revealTraps = true;
            revealItems = true;
            revealEnemies = true;
            revealStairs = true;
            break;
            case "XRay":
            revealItems = true;
            revealEnemies = true;
            break;
            case "Layout":
            revealLayout = true;
            break;
            case "Traps":
            revealTraps = true;
            break;
            case "Items":
            revealItems = true;
            break;
            case "Enemies":
            revealEnemies = true;
            break;
            case "Stairs":
            revealStairs = true;
            break;
        }
        UpdateMap();
    }
    public MysteryDungeonManager MDGeneralManager;
    public int monsterHouseRoom = -1;
    [ContextMenu("Debug Player Room")]
    protected void DebugPlayerRoom()
    {
        Debug.Log(GetRoomOfTile(playerLocation));
    }
    protected void DetermineMonsterHouseRoom()
    {
        monsterHouseRoom = -1;
        int monsterHouseChance = Mathf.Min(25, 5 * (floor / 5));
        if (UnityEngine.Random.Range(0, 100) >= monsterHouseChance){return;}
        // Pick a random room aside from the player spawning room.
        List<int> allRoomsNumbers = new List<int>(tileRooms).Distinct().ToList();
        allRoomsNumbers.Remove(-1);
        allRoomsNumbers.Remove(GetRoomOfTile(playerLocation));
        if (allRoomsNumbers.Count <= 0){return;}
        monsterHouseRoom = allRoomsNumbers[UnityEngine.Random.Range(0, allRoomsNumbers.Count)];
    }
    public void CheckMonsterHouse()
    {
        if (monsterHouseRoom < 0){return;}
        if (GetRoomOfTile(playerLocation) == monsterHouseRoom)
        {
            GenerateMonsterHouse();
            monsterHouseRoom = -1;
        }
    }
    public void GenerateMonsterHouse()
    {
        int room = GetRoomOfTile(playerLocation);
        if (room < 0){return;}
        List<int> roomTiles = GetTilesOfRoom(room);
        UpdateCombatLog("It's A Monster House!");
        // Randomly Spawn Enemies Up To The Room Size.
        updateMap = false;
        for (int i = 0; i < roomTiles.Count; i++)
        {
            string enemyName = RandomEnemy();
            int enemyLocation = roomTiles[UnityEngine.Random.Range(0, roomTiles.Count)];
            if (GetActorOnTile(enemyLocation) == null)
            {
                SpawnAndAddActor(enemyLocation, enemyName, 1);
            }
        }
        updateMap = true;
        UpdateMap();
    }
    // Monster Houses Ignore This.
    public int GetMaxEnemyCount()
    {
        return 3 + Mathf.RoundToInt(Mathf.Sqrt(floor));
    }
    public int GetEnemyCount()
    {
        int count = 0;
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetTeam() != 0){count++;}
        }
        return count;
    }
    // Get The Enemy Pool From The State In The Manager.
    public void GenerateStartingEnemy()
    {
        string enemy = GenerateRandomEnemy();
        if (enemy == ""){return;}
        int location = GenerateEnemyLocation();
        if (startingEnemyLocations.Contains(location)){return;}
        startingEnemies.Add(enemy);
        startingEnemyLocations.Add(location);
    }
    public void SpawnStartingEnemies()
    {
        for (int i = 0; i < startingEnemies.Count; i++)
        {
            SpawnAndAddActor(startingEnemyLocations[i], startingEnemies[i], 1);
        }
    }
    private int turnSinceSpawn = 0;
    [SerializeField] private int minSpawnInterval = 20;
    [SerializeField] private int maxSpawnInterval = 50;
    public void UpdateEnemySpawning()
    {
        turnSinceSpawn++;
        int realUpperBound = Mathf.Max(minSpawnInterval, maxSpawnInterval - Mathf.RoundToInt(Mathf.Sqrt(floor)));
        if (turnSinceSpawn >= UnityEngine.Random.Range(minSpawnInterval, realUpperBound))
        {
            turnSinceSpawn = 0;
            SpawnRandomEnemy();
        }
    }
    public void SpawnRandomEnemy()
    {
        string enemyName = GenerateRandomEnemy();
        int enemyLocation = GenerateEnemyLocation();
        if (GetActorOnTile(enemyLocation) == null)
        {
            SpawnAndAddActor(enemyLocation, enemyName, 1);
        }
    }
    protected string RandomEnemy()
    {
        return possibleEnemies[UnityEngine.Random.Range(0, possibleEnemies.Count)];
    }
    public string GenerateRandomEnemy()
    {
        if (GetEnemyCount() >= GetMaxEnemyCount()){return "";}
        return RandomEnemy();
    }
    public int GenerateEnemyLocation()
    {
        // Pick A Random Tile In A Room That Is Not Visible.
        UpdateVisibleTiles();
        List<int> allRoomTiles = GetAllRoomTiles();
        List<int> allVisible = GetVisibleTiles();
        List<int> availableTiles = allRoomTiles.Except(allVisible).ToList();
        if (availableTiles.Count == 0){return -1;}
        return availableTiles[UnityEngine.Random.Range(0, availableTiles.Count)];
    }
    public PartyDungeonState partyDungeonState;
    public MysteryDungeonMapUtility MDMapUtility;
    public StatDatabase dungeonTypeSubTerrains;
    public StatDatabase dungeonTypeWeathers;
    // Takes In The Floor + Type And Returns A Pool Of Items.
    public StatDatabase dungeonTypeItemPool;
    public string GenerateItemFromPool()
    {
        List<string> allItems = dungeonTypeItemPool.GetAllKeys();
        List<int> allItemWeights = new List<int>();
        for (int i = allItems.Count - 1; i >= 0; i--)
        {
            string[] itemData = dungeonTypeItemPool.ReturnValue(allItems[i]).Split("|");
            // Min Floor > Current Floor OR Excluded Type
            if (int.Parse(itemData[1]) > floor || itemData[2].Contains(dungeonType))
            {
                allItems.RemoveAt(i);
                continue;
            }
            allItemWeights.Insert(0, int.Parse(itemData[0]));
        }
        string randomItem = utility.ReturnRandomValueBasedOnWeights(allItems, allItemWeights);
        return randomItem == "" ? "Apple" : randomItem;
    }
    public StatDatabase dungeonItems;
    // Each Tile Is Either A Room (Room #) Or Not (-1).
    [System.NonSerialized]
    public List<int> tileRooms = new();
    [ContextMenu("Debug Tile Rooms")]
    protected void DebugTileRooms()
    {
        Debug.Log(String.Join("|", tileRooms));
    }
    public List<int> GetTilesOfRoom(int roomNumber)
    {
        List<int> roomTiles = new List<int>();
        for (int i = 0; i < tileRooms.Count; i++)
        {
            if (GetRoomOfTile(i) == roomNumber){roomTiles.Add(i);}
        }
        return roomTiles;
    }
    public override List<int> GetRoomTiles(int startTile)
    {
        int room = tileRooms[startTile];
        List<int> roomTiles = new List<int>();
        for (int i = 0; i < tileRooms.Count; i++)
        {
            if (GetRoomOfTile(i) == room)
            {
                roomTiles.Add(i);
            }
        }
        return roomTiles;
    }
    public List<int> GetAllRoomTiles()
    {
        List<int> allRoomTiles = new List<int>();
        for (int i = 0; i < tileRooms.Count; i++)
        {
            if (tileRooms[i] >= 0){allRoomTiles.Add(i);}
        }
        return allRoomTiles;
    }
    public int GetRoomOfTile(int tileNumber){return tileRooms[tileNumber];}
    // Track Which Tiles Were Explored, So The Map Is Built While Exploring.
    [System.NonSerialized]
    public List<int> tileExplored = new();
    public List<int> visibleTiles;
    public List<int> GetVisibleTiles(){return visibleTiles;}
    // For Use For Enemy Targeting.
    public List<int> GetActorVisibleTiles(int actorLocation)
    {
        return MDMapUtility.GetVisibleTiles(actorLocation, this);
    }
    public void UpdateVisibleTiles()
    {
        if (tileExplored.Count < mapInfo.Count)
        {
            tileExplored = Enumerable.Repeat(0, mapInfo.Count).ToList();
        }
        visibleTiles = GetActorVisibleTiles(playerLocation);
        visibleTiles = visibleTiles.Distinct().ToList();
        foreach (int tile in visibleTiles)
        {
            tileExplored[tile] = 1;
        }
    }
    public override void UpdateMovingHighlights(TacticActor selectedActor, bool current = true)
    {
        base.UpdateMovingHighlights(selectedActor, current);
        ApplyFogOfWar();
    }
    // Track Player Location.
    public int playerLocation;
    public TacticActor GetPartyLeader()
    {
        int leaderID = partyDungeonState.GetLeaderID();
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetID() == leaderID)
            {
                return battlingActors[i];
            }
        }
        return null;
    }
    public bool RefreshPlayerLocation()
    {
        return UpdatePlayerLocation(GetPartyLeader().GetLocation());
    }
    public bool UpdatePlayerLocation(int newLocation)
    {
        playerLocation = newLocation;
        if (playerLocation == stairsLocation)
        {
            MDGeneralManager.NewFloor();
            return true;
        }
        CheckMonsterHouse();
        return false;
    }
    public int GetPlayerLocation(){return playerLocation;}
    // Track Stairs.
    public int stairsLocation;
    public int GetStairsLocations(){return stairsLocation;}
    // Track Traps.
    public StatDatabase dungeonTraps;
    public string GenerateTrap()
    {
        List<string> allItems = dungeonTraps.GetAllKeys();
        return allItems[UnityEngine.Random.Range(0, allItems.Count)];
    }
    public List<string> traps;
    // Traps Are Normally Hidden Until Triggered.
    public List<bool> trapTriggered;
    public List<int> trapLocations;
    public void ActorAttackTrap(int tileNumber)
    {
        int indexOf = trapLocations.IndexOf(tileNumber);
        if (indexOf < 0){return;}
        trapTriggered[indexOf] = true;
    }
    public void ActorStepOnTrap(TacticActor actor)
    {
        int location = actor.GetLocation();
        int indexOf = trapLocations.IndexOf(location);
        // No Trap On Tile.
        if (indexOf < 0){return;}
        // Enemies Won't Trigger Traps Unless Already Revealed.
        if (!trapTriggered[indexOf] && actor.GetTeam() != 0){return;}
        trapTriggered[indexOf] = true;
        if (actor.AvoidTraps())
        {
            UpdateCombatLog(actor.GetPersonalName() + " avoided a " + traps[indexOf]);
            return;
        }
        UpdateCombatLog(actor.GetPersonalName() + " triggered a " + traps[indexOf]);
        MDGeneralManager.TriggerTrap(actor, traps[indexOf]);
    }
    public bool TrapTriggered(int tileNumber)
    {
        int indexOf = trapLocations.IndexOf(tileNumber);
        return indexOf >= 0 ? trapTriggered[indexOf] : false;
    }
    public void RemoveTraps()
    {
        traps.Clear();
        trapTriggered.Clear();
        trapLocations.Clear();
    }
    public void RemoveTrapAtLocation(int location)
    {
        int indexOf = trapLocations.IndexOf(location);
        if (indexOf < 0){return;}
        traps.RemoveAt(indexOf);
        trapLocations.RemoveAt(indexOf);
        trapTriggered.RemoveAt(indexOf);
    }
    public void AddTrap(int location)
    {
        if (trapLocations.Contains(location)){return;}
        // Generate A Random Trap.
        traps.Add(GenerateTrap());
        trapLocations.Add(location);
        trapTriggered.Add(false);
    }
    // Track Treasures.
    public List<string> items;
    public List<int> itemLocations;
    public void ActorPickUpItem(TacticActor actor)
    {
        int indexOf = itemLocations.IndexOf(actor.GetLocation());
        if (indexOf < 0){return;}
        // Enemies/Allies Pick Up Items Automatically.
        if (actor.GetTeam() != 0 && actor.GetDungeonHeldItem() == "")
        {
            UpdateCombatLog(actor.GetPersonalName() + " picked up " + items[indexOf]);
            actor.SetDungeonHeldItem(items[indexOf]);
            RemoveItemAtLocation(actor.GetLocation());
        }
        else if (actor.GetTeam() == 0 && MDGeneralManager.PickUpItem(items[indexOf]))
        {
            UpdateCombatLog(actor.GetPersonalName() + " picked up " + items[indexOf]);
            RemoveItemAtLocation(actor.GetLocation());
        }
    }
    public void RemoveItemAtLocation(int location)
    {
        int indexOf = itemLocations.IndexOf(location);
        if (indexOf >= 0)
        {
            items.RemoveAt(indexOf);
            itemLocations.RemoveAt(indexOf);
        }
    }
    public void AddItemToLocation(string itemName, int location)
    {
        // TODO Check If Added To A Store.
        items.Add(itemName);
        itemLocations.Add(location);
        UpdateMap();
    }
    // Track Stores.
    public int storeRoom = -1;
    public int storeOwnerTile = -1;
    public List<int> storeTiles;
    public void DetermineStoreRoom()
    {
        storeRoom = -1;
        storeOwnerTile = -1;
        storeTiles.Clear();
        // Calculate Chance Of Store.
        int chance = Mathf.Min(25, 5 * (floor / 3));
        if (UnityEngine.Random.Range(0, 100) >= chance){return;}
        // Pick A Random Room, Excluding Monster House Room + Player Room.
        List<int> allRoomsNumbers = new List<int>(tileRooms).Distinct().ToList();
        allRoomsNumbers.Remove(-1);
        allRoomsNumbers.Remove(GetRoomOfTile(playerLocation));
        allRoomsNumbers.Remove(monsterHouseRoom);
        if (allRoomsNumbers.Count <= 0){return;}
        storeRoom = allRoomsNumbers[UnityEngine.Random.Range(0, allRoomsNumbers.Count)];
        List<int> roomTiles = GetTilesOfRoom(storeRoom);
        utility.ShuffleIntList(roomTiles);
        // Find Valid Tile
        for (int i = 0; i < roomTiles.Count; i++)
        {
            List<int> adjacent = mapUtility.AdjacentTiles(roomTiles[i], mapSize);
            // If Somehow On A Border/Edge Then Skip.
            if (adjacent.Count < 6){continue;}
            // If Adjacent To Stairs Then Skip.
            if (adjacent.Contains(stairsLocation)){continue;}
            // If Adjacent To Wall/Hall Then Skip.
            if (!MDMapUtility.AllRoomTiles(adjacent, this)){continue;}
            storeOwnerTile = roomTiles[i];
            storeTiles = adjacent;
            break;
        }
        if (storeOwnerTile < 0)
        {
            storeRoom = -1;
            return;
        }
        // Remove Any Items/Traps On Tiles.
        RemoveTrapAtLocation(storeOwnerTile);
        RemoveItemAtLocation(storeOwnerTile);
        for (int i = 0; i < storeTiles.Count; i++)
        {
            RemoveTrapAtLocation(storeTiles[i]);
            RemoveItemAtLocation(storeTiles[i]);
        }
        // Generate The Store + Items.
    }
    public List<string> storeItems;
    public List<int> storeItemLocations;
    protected bool storeRobbed = false;
    protected List<string> unpaidStoreItems;
    protected List<string> soldToStoreItems;
    public List<string> possibleEnemies;
    public List<string> startingEnemies;
    public List<int> startingEnemyLocations;
    public bool TilePassable(int tileNumber)
    {
        return MDMapUtility.TilePassable(mapInfo[tileNumber]);
    }
    public void InitializeMapFromGenerator(MysteryDungeonGenerator generator, string newType, List<string> newPossibleEnemies)
    {
        combatLog.ForceStart();
        mapInfo.Clear();
        possibleEnemies = newPossibleEnemies;
        SetDungeonType(newType);
        tileRooms = new List<int>(generator.tileRooms);
        for (int i = 0; i < generator.allTiles.Count; i++)
        {
            if (generator.allTiles[i] == 1)
            {
                mapInfo.Add(GetWallTerrain(dungeonType));
            }
            else
            {
                // Start With A Plains Default.
                mapInfo.Add("Plains");
            }
        }
        UpdateMapSize();
        // Resets Terrain/Elevations/Borders/Buildings
        ResetAll();
        UpdatePlayerLocation(generator.startLocation);
        stairsLocation = generator.stairsLocation;
        itemLocations = new List<int>(generator.itemLocations);
        trapLocations = new List<int>(generator.trapLocations);
        trapTriggered = Enumerable.Repeat(false, trapLocations.Count).ToList();
        // TODO Decide What Items/Traps Exist.
        items = new List<string>();
        for (int i = 0; i < itemLocations.Count; i++)
        {
            items.Add(GenerateItemFromPool());
        }
        traps = new List<string>();
        for (int i = 0; i < trapLocations.Count; i++)
        {
            traps.Add(GenerateTrap());
        }
        tileExplored = Enumerable.Repeat(0, mapInfo.Count).ToList();
        int timeRNG = UnityEngine.Random.Range(0, 24);
        if (timeRNG < 16)
        {
            SetTime("Day");
        }
        else
        {
            SetTime("Night");
        }
        string[] possibleWeather = dungeonTypeWeathers.ReturnValue(dungeonType).Split("|");
        SetWeather(possibleWeather[UnityEngine.Random.Range(0, possibleWeather.Length)]);
        // Group only walkable tiles that belong to a room.
        Dictionary<int, List<int>> roomFloorTiles =
        new Dictionary<int, List<int>>();
        for (int tile = 0; tile < generator.allTiles.Count; tile++)
        {
            if (generator.allTiles[tile] != 0)
                continue;
            int room = tileRooms[tile];
            // Room ID -1 means hallway or other non-room floor.
            if (room < 0)
                continue;
            if (!roomFloorTiles.ContainsKey(room))
            {
                roomFloorTiles[room] = new List<int>();
            }
            roomFloorTiles[room].Add(tile);
        }
        // Second pass: give each room its own biome and generate clustered terrain inside that room.
        foreach (var room in roomFloorTiles)
        {
            CreateRoomBiome(dungeonType, room.Value);
        }
        startingEnemies.Clear();
        startingEnemyLocations.Clear();
        for (int i = 0; i < Mathf.Max(1, GetMaxEnemyCount() / 2); i++)
        {
            GenerateStartingEnemy();
        }
        SpawnStartingEnemies();
        DetermineMonsterHouseRoom();
        DetermineStoreRoom();
        UpdateMoveCostManager();
    }
    protected void CreateRoomBiome(string dungeonType, List<int> roomTiles)
    {
        string[] dungeonTypeBiomes = dungeonTypeSubTerrains.ReturnValue(dungeonType).Split("|");
        string mainBiome = dungeonTypeBiomes[UnityEngine.Random.Range(0, dungeonTypeBiomes.Length)];
        string subBiome = dungeonTypeBiomes[UnityEngine.Random.Range(0, dungeonTypeBiomes.Length)];
        // TODO Make The Biome Clusters.
        HashSet<int> roomSet = new HashSet<int>(roomTiles);
        HashSet<int> changedTiles = new HashSet<int>();
        int patchCount = Mathf.Max(1, roomTiles.Count / 12);
        for (int patch = 0; patch < patchCount; patch++)
        {
            // Choose a seed that has not already been changed.
            List<int> availableSeeds = roomTiles.Where(tile => !changedTiles.Contains(tile)).ToList();
            if (availableSeeds.Count == 0)
                break;
            int seed = availableSeeds[UnityEngine.Random.Range(0, availableSeeds.Count)];
            // Keep patches irregular by varying their size.
            int targetSize = UnityEngine.Random.Range(4, 9);
            GrowTerrainPatch(seed, mainBiome, subBiome, targetSize, roomSet, changedTiles);
        }
    }
    protected void GrowTerrainPatch(int seed, string terrain, string subTerrain, int targetSize, HashSet<int> roomTiles, HashSet<int> changedTiles)
    {
        if (!roomTiles.Contains(seed) || changedTiles.Contains(seed)){return;}
        List<int> mainTerrainTiles = new List<int>{seed};
        List<int> borders = mapUtility.AdjacentBorders(mainTerrainTiles, mapSize);
        // Only Get Tiles Inside The Room.
        borders = borders.Intersect(roomTiles).ToList();
        if (borders.Count <= 0){return;}
        // Randomly Add A Tile And Keep Spreading.
        for (int i = 0; i < targetSize; i++)
        {
            mainTerrainTiles.Add(borders[UnityEngine.Random.Range(0, borders.Count)]);
            borders = mapUtility.AdjacentBorders(mainTerrainTiles, mapSize);
            borders = borders.Intersect(roomTiles).ToList();
            if (borders.Count <= 0){return;}
        }
        for (int i = 0; i < mainTerrainTiles.Count; i++)
        {
            mapInfo[mainTerrainTiles[i]] = terrain;
            changedTiles.Add(mainTerrainTiles[i]);
        }
        // Surround the main terrain with subterrain.
        borders = mapUtility.AdjacentBorders(mainTerrainTiles, mapSize);
        borders = borders.Intersect(roomTiles).ToList();
        for (int i = 0; i < borders.Count; i++)
        {
            mapInfo[borders[i]] = subTerrain;
        }
    }
    protected string GetWallTerrain(string dungeonType)
    {
        switch (dungeonType)
        {
            case "Forest":
                return "Wall";
            case "Mountain":
                return "Pit";
            case "Water":
                return "DeepWater";
            case "Underground":
                return "Pit";
            case "Snow":
                return "Wall";
            case "Desert":
                return "Wall";
        }
        return "Wall";
    }
    // This Is Basically Start()
    public void LoadMapStateFromString(string savedState)
    {
        // TODO Clear Buildings + Etc.
        if (string.IsNullOrEmpty(savedState))
            return;
        combatLog.ForceStart();
        string[] sections = savedState.Split('|');
        foreach (string section in sections)
        {
            if (string.IsNullOrEmpty(section))
                continue;
            string[] keyValue = section.Split(new char[] { '=' }, 2);
            if (keyValue.Length != 2)
                continue;
            string key = keyValue[0];
            string value = keyValue[1];
            switch (key)
            {
                case "Floor":
                    floor = int.Parse(value);
                    break;
                case "Type":
                    dungeonType = value;
                    break;
                case "Tiles":
                    mapInfo = value.Split(",").ToList();
                    break;
                case "MapSize":
                    mapSize = int.Parse(value);
                    break;
                case "TileRooms":
                    tileRooms = string.IsNullOrEmpty(value) ? new List<int>() : value.Split(',').Select(int.Parse).ToList();
                    break;
                case "TerrainEffects":
                    terrainEffectTiles = value.Split(",").ToList();
                    break;
                case "Weather":
                    weather = value;
                    break;
                case "Time":
                    time = value;
                    break;
                case "Player":
                    playerLocation = int.Parse(value);
                    break;
                case "Stairs":
                    stairsLocation = int.Parse(value);
                    break;
                case "Traps":
                    traps = string.IsNullOrEmpty(value) ? new List<string>() : value.Split(',').ToList();
                    break;
                case "TrapLocations":
                    trapLocations = string.IsNullOrEmpty(value) ? new List<int>() : value.Split(',').Select(int.Parse).ToList();
                    trapTriggered = Enumerable.Repeat(false, trapLocations.Count).ToList();
                    break;
                case "Items":
                    items = string.IsNullOrEmpty(value) ? new List<string>() : value.Split(',').ToList();
                    break;
                case "ItemLocations":
                    itemLocations = string.IsNullOrEmpty(value) ? new List<int>() : value.Split(',').Select(int.Parse).ToList();
                    break;
                case "PossibleEnemies":
                    possibleEnemies = string.IsNullOrEmpty(value) ? new List<string>() : value.Split(',').ToList();
                    break;
                case "StartingEnemies":
                    startingEnemies = string.IsNullOrEmpty(value) ? new List<string>() : value.Split(',').ToList();
                    break;
                case "EnemyLocations":
                    startingEnemyLocations = string.IsNullOrEmpty(value) ? new List<int>() : value.Split(',').Select(int.Parse).ToList();
                    break;
                case "MonsterHouseRoom":
                    monsterHouseRoom = int.Parse(value);
                    break;
                case "StoreRoom":
                    storeRoom = int.Parse(value);
                    break;
                case "StoreTile":
                    storeOwnerTile = int.Parse(value);
                    break;
                case "StoreItems":
                    storeItems = string.IsNullOrEmpty(value) ? new List<string>() : value.Split(',').ToList();
                    break;
                case "StoreItemLocations":
                    storeItemLocations = string.IsNullOrEmpty(value) ? new List<int>() : value.Split(',').Select(int.Parse).ToList();
                    break;
            }
        }
        ResetAll();
        UpdateVisibleTiles();
        SpawnStartingEnemies();
        UpdateMoveCostManager();
    }
    // This Is Basically Save()
    public string ReturnMapStateString()
    {
        return MDMapUtility.ReturnMapStateString(this);
    }
    public MysteryDungeonMinimap miniMap;
    // TODO Locked Rooms?
    public void PublicUpdateCurrentTiles(){UpdateCurrentTiles();}
    protected override void UpdateCurrentTiles()
    {
        currentTiles = currentTileManager.GetCurrentTilesFromCenter(playerLocation, mapSize, gridSize);
    }
    public override void UpdateActors()
    {
        GetActorTiles();
        if (!DisplayLayerExists(actorLayer)){return;}
        actorTiles[stairsLocation] = "Stairs";
        for (int i = 0; i < itemLocations.Count; i++)
        {
            if (actorTiles[itemLocations[i]] == "")
            {
                actorTiles[itemLocations[i]] = items[i];
            }
        }
        mapDisplayers[actorLayer].DisplayCurrentStyledTiles(mapTiles, actorTiles, currentTiles, true, actorDirections);
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].ResetHealthBar();
        }
        for (int i = 0; i < battlingActors.Count; i++)
        {
            // Get The Current Map Tile Showing Each Actor.
            int indexOf = currentTiles.IndexOf(battlingActors[i].GetLocation());
            if (indexOf < 0){continue;}
            mapTiles[indexOf].UpdateHealthBar(battlingActors[i].GetHealth(), battlingActors[i].GetBaseHealth());
        }
    }
    protected List<string> trapTiles;
    protected void GetTrapTiles()
    {
        trapTiles = new List<string>(emptyList);
        for (int i = 0; i < traps.Count; i++)
        {
            if (!trapTriggered[i]){continue;}
            trapTiles[trapLocations[i]] = traps[i];
        }
    }
    public void UpdateTraps()
    {
        GetTrapTiles();
        if (!DisplayLayerExists(buildingLayer)){return;}
        mapDisplayers[buildingLayer].DisplayCurrentTiles(mapTiles, trapTiles, currentTiles);
    }
    public void ApplyFogOfWar()
    {
        for (int i = 0; i < currentTiles.Count; i++)
        {
            if (!visibleTiles.Contains(currentTiles[i]))
            {
                mapTiles[i].ResetAllLayers();
                mapTiles[i].ResetDirectionArrows();
                mapTiles[i].ResetHighlight();
                mapTiles[i].ResetHealthBar();
            }
        }
    }
    public override void UpdateMap()
    {
        if (!updateMap){return;}
        UpdateVisibleTiles();
        // Tile Layer.
        BaseUpdateMap();
        UpdateTraps();
        UpdateActors();
        // TODO HP Bars.
        UpdateTerrain();
        // TODO Aura Layer.
        ApplyFogOfWar();
        // Update The Minimap Based On Explored/Visible Tiles.
        miniMap.CreateMinimap();
    }
    public override void MoveActorToTile(TacticActor actor, int tile, bool forced = false)
    {
        AIMoveActorToTile(actor, tile, forced);
        ActorPickUpItem(actor);
        ActorStepOnTrap(actor);
        UpdateMap();
    }
    public override void WarpPassive(TacticActor actor, string effect, string specifics)
    {
        MoveEffect(actor, effect, specifics);
    }
    public void MoveEffect(TacticActor moved, string effect, string specifics, TacticActor mover = null)
    {
        if (moved == null){return;}
        int currentLocation = moved.GetLocation();
        List<int> possibleLocations = new List<int>();
        switch (effect)
        {
            case "Warp":
            // Add All Room Tiles, Ignore Hallways.
            for (int i = 0; i < tileRooms.Count; i++)
            {
                if (tileRooms[i] >= 0)
                {
                    possibleLocations.Add(i);
                }
            }
            break;
            case "WarpStairs":
            // Stairs + Adjacent Tiles
            possibleLocations.AddRange(mapUtility.AdjacentTiles(stairsLocation, mapSize));
            possibleLocations.Add(stairsLocation);
            break;
            case "WarpLeader":
            // Adjacent To Party Leader.
            possibleLocations = mapUtility.AdjacentTiles(playerLocation, mapSize);
            break;
            // Get The First Unit In A Line.
            case "Swap":
            possibleLocations = mapUtility.GetTilesInLineDirection(moved.GetLocation(), moved.GetDirection(), int.Parse(specifics), mapSize);
            for (int i = 0; i < possibleLocations.Count; i++)
            {
                if (possibleLocations[i] != currentLocation && GetActorOnTile(possibleLocations[i]) != null && MDMapUtility.TilePassable(mapInfo[possibleLocations[i]]))
                {
                    TacticActor swapped = GetActorOnTile(possibleLocations[i]);
                    moved.SetLocation(swapped.GetLocation());
                    swapped.SetLocation(currentLocation);
                    UpdateMap();
                    return;
                }
            }
            return;
            case "Push":
            battleMapUtility.DisplaceActor(moved, DirectionBetweenActors(mover, moved), int.Parse(specifics), this);
            break;
            case "Forward":
            List<int> forwardMoveTiles = MDMapUtility.GetPassableTilesInDirection(mover, this, mover.GetDirection(), int.Parse(specifics));
            for (int i = 0; i < forwardMoveTiles.Count; i++)
            {
                MoveActorToTile(mover, forwardMoveTiles[i], true);
            }
            break;
            // Throw At Nearest Ally If Possible.
            case "Hurl":
            List<TacticActor> allAllies = AllAllies(moved);
            allAllies.Remove(moved);
            TacticActor closestAlly = GetClosestActor(moved, allAllies);
            // No Ally / Ally Too Far > Thrown Backward If Possible Else Don't Move.
            moved.TakeEffectDamage(mover.GetAttack() * Mathf.Max(1, moved.GetWeight()));
            if (closestAlly == null || DistanceBetweenActors(closestAlly, moved) > int.Parse(specifics))
            {
                possibleLocations = mapUtility.AdjacentTiles(mapUtility.PointInDirection(moved.GetLocation(), DirectionBetweenActors(mover, moved), mapSize), mapSize);
            }
            else
            {
                possibleLocations = mapUtility.AdjacentTiles(closestAlly.GetLocation(), mapSize);
                closestAlly.TakeEffectDamage(mover.GetAttack() * Mathf.Max(1, moved.GetWeight()));
            }
            break;
        }
        if (possibleLocations.Count > 0)
        {
            utility.ShuffleIntList(possibleLocations);
            for (int i = 0; i < possibleLocations.Count; i++)
            {
                // Don't Move To Unpassable/Occupied Tiles.
                if (GetActorOnTile(possibleLocations[i]) != null || !MDMapUtility.TilePassable(mapInfo[possibleLocations[i]])){continue;}
                moved.SetLocation(possibleLocations[i]);
                return;
            }
        }
        UpdateMap();
    }
    protected void RemoveTileOfType(int tileNumber, string type)
    {
        if (type.Contains(mapInfo[tileNumber]) || string.Equals(type, "All", StringComparison.OrdinalIgnoreCase))
        {
            mapInfo[tileNumber] = "Plains";
        }
    }
    public void MDChangeMap(TacticActor user, string effect, string specifics)
    {
        List<int> tiles = new List<int>();
        List<TacticActor> targets = new List<TacticActor>();
        switch (effect)
        {
            // Remove All Tiles Of Type, Replace With Plains.
            case "Remove":
            for (int i = 0; i < mapInfo.Count; i++)
            {
                RemoveTileOfType(i, specifics);
            }
            break;
            case "RemoveFront":
            RemoveTileOfType(mapUtility.PointInDirection(user.GetLocation(), user.GetDirection(), mapSize), specifics);
            break;
            case "Weather":
            SetWeather(specifics);
            break;
            case "Itemize":
            TacticActor itemizeTarget = MDMapUtility.GetClosestEnemy(user, this);
            if (!itemLocations.Contains(itemizeTarget.GetLocation()))
            {
                items.Add(GenerateItemFromPool());
                itemLocations.Add(itemizeTarget.GetLocation());
            }
            battlingActors.Remove(itemizeTarget);
            break;
            case "OneRoom":
            for (int i = 0; i < mapInfo.Count; i++)
            {
                tileRooms[i] = 0;
                mapInfo[i] = "Plains";
            }
            break;
            case "RemoveTraps":
            RemoveTraps();
            break;
            case "AddTrap":
            AddTrap(user.GetLocation());
            break;
            case "Explosion":
            tiles = mapUtility.AdjacentTiles(user.GetLocation(), mapSize);
            tiles.Add(user.GetLocation());
            targets = GetActorsOnTiles(tiles);
            for (int i = 0; i < tiles.Count; i++)
            {
                // Remove Item.
                RemoveItemAtLocation(tiles[i]);
                // Destroy Terrain.
                RemoveTileOfType(tiles[i], "All");
            }
            for (int i = 0; i < targets.Count; i++)
            {
                // Damage Actor.
                passiveEffect.AffectActor(targets[i], "FireDamage", "20");
            }
            break;
            case "Mimic":
            // TODO Change All Items To Enemies.
            break;
            case "MonsterHouse":
            // TODO Summon A Large Amount Of Enemies.
            break;
        }
        UpdateMap();
    }
}