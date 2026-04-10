using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionMap : MapManager
{
    public string outputDelimiter = "&";
    public MapPathfinder pathfinder;
    public ColorDictionary colorDictionary;
    public FactionMapData mapData;
    protected override void Start()
    {
        // Load everything at the start, then save when leaving the scene or change the day.
        Load();
        pathfinder.SetMapSize(mapSize);
        pathfinder.SetMoveCosts(moveCosts);
        UpdateMap();
    }
    protected void Load()
    {
        mapData.LoadMap(this);
        cities.Load();
        units.Load();
    }
    protected void Save()
    {
        mapData.SaveMap(this);
        cities.SaveCityData();
        units.SaveUnitData();
    }
    protected override void UpdateCurrentTiles()
    {
        currentTiles = currentTileManager.GetCurrentTilesFromCenter(centerTile, mapSize, gridSize);
    }
    [ContextMenu("Test New Map")]
    public void TestNewMap()
    {
        mapData.GenerateNewMap(this);
        Save();
        UpdateMap();
    }
    public List<int> moveCosts;
    public void ResetMoveCosts()
    {
        moveCosts.Clear();
    }
    public int GetMoveCost(int tile)
    {
        // Out of bounds tiles are not passable.
        if (tile < 0){return 99;}
        // Default move cost is 1.
        else if (tile >= moveCosts.Count){return 1;}
        return moveCosts[tile];
    }
    protected List<string> actorTiles;
    public void SetActorTiles(List<string> newInfo)
    {
        actorTiles = new List<string>(newInfo);
    }
    public List<string> GetActorTiles(){return actorTiles;}
    public void ResetActorTiles()
    {
        InitializeEmptyList();
        actorTiles = new List<string>(emptyList);
    }
    public void UpdateActorTile(int tileNumber, string aName)
    {
        actorTiles[tileNumber] = aName;
    }
    public bool ActorOnTile(int tileNumber)
    {
        return actorTiles[tileNumber] != "";
    }
    public override void UpdateMap()
    {
        InitializeEmptyList();
        UpdateCurrentTiles();
        // Tiles.
        mapDisplayers[0].DisplayCurrentTiles(mapTiles, mapInfo, currentTiles);
        // Buildings
        mapDisplayers[1].DisplayCurrentTiles(mapTiles, tileBuildings, currentTiles);
        // Luxurys.
        mapDisplayers[2].DisplayCurrentTiles(mapTiles, luxuryTiles, currentTiles);
        // Actors.
        mapDisplayers[3].DisplayCurrentTiles(mapTiles, actorTiles, currentTiles);
        // Highlights.
        mapDisplayers[4].HighlightCurrentTiles(mapTiles, highlightedTiles, currentTiles);
    }
    protected List<string> highlightedTiles;
    public List<string> GetHighlights(){return highlightedTiles;}
    public void ResetHighlights()
    {
        InitializeEmptyList();
        highlightedTiles = new List<string>(emptyList);
    }
    public void SetHighlightedTiles(List<string> newHighlights)
    {
        highlightedTiles = new List<string>(newHighlights);
    }
    public void UpdateHighlightedTile(int tile, string highlight)
    {
        highlightedTiles[tile] = highlight;
    }
    public StatDatabase buildableData; // Maps what buildings can be placed on what tiles.
    public List<string> BuildablesOnTiles(List<int> tileList)
    {
        List<string> buildables = new List<string>();
        for (int i = 0; i < tileList.Count; i++)
        {
            buildables.Add(buildableData.ReturnValue(mapInfo[tileList[i]]));
        }
        return buildables;
    }
    protected List<string> tileBuildings; // Buildings upgrade tile outputs and are agnostic to factions. Whoever owns the tile also owns the building.
    public string cityString;
    public void MakeCity(int tileNumber)
    {
        tileBuildings[tileNumber] = cityString;
    }
    public int ClosestCityDistance(int tileNumber)
    {
        int distance = mapSize * mapSize;
        for (int i = 0; i < tileBuildings.Count; i++)
        {
            if (tileBuildings[i] == cityString)
            {
                int newDist = mapUtility.DistanceBetweenTiles(tileNumber, i, mapSize);
                if (newDist < distance)
                {
                    distance = newDist;
                }
            }
        }
        return distance;
    }
    // Need to build up a local population before making a building.
    public string bbbS;
    public bool BuildingOnTile(int tileNumber)
    {
        // Houses don't count.
        if (tileBuildings[tileNumber].Contains(bbbS)){return false;}
        return tileBuildings[tileNumber] != "";
    }
    public int ReturnClosestBuildingTile(int start)
    {
        int tile = -1;
        int distance = mapSize * mapSize;
        for (int i = 0; i < tileBuildings.Count; i++)
        {
            if (!BuildingOnTile(i)){continue;}
            int newDist = mapUtility.DistanceBetweenTiles(start, i, mapSize);
            if (newDist < distance)
            {
                distance = newDist;
                tile = i;
            }
        }
        return tile;
    }
    public void DestroyBuilding(int tileNumber)
    {
        // Destroying cities is different than destroying other buildings.
        if (tileBuildings[tileNumber] == cityString){return;}
        tileBuildings[tileNumber] = houseBuildings[houseBuildings.Count / 2];
        RefreshTileOutput(tileNumber);
    }
    public void DestroyRandomBuilding(List<int> tiles)
    {
        // Shuffle the tiles.
        utility.ShuffleIntList(tiles);
        // Iterate, skipping cities.
        for (int i = 0; i < tiles.Count; i++)
        {
            if (BuildingOnTile(tiles[i]))
            {
                // If it rolls on the capital then you've lucked out this round.
                DestroyBuilding(tiles[i]);
                return;
            }
        }
    }
    // Have to set up houses before the final building.
    public List<string> houseBuildings;
    public bool TryToBuildOnTile(string building, int tileNumber)
    {
        // Check if it's buildable.
        if (BuildingOnTile(tileNumber)){return false;}
        List<string> buildable = buildableData.ReturnValue(mapInfo[tileNumber]).Split("|").ToList();
        if (buildable.Contains(building))
        {
            // Check if the appropriate number of houses have been built.
            int indexOf = houseBuildings.IndexOf(tileBuildings[tileNumber]);
            if (indexOf < houseBuildings.Count - 1)
            {
                tileBuildings[tileNumber] = houseBuildings[indexOf + 1];
                return true;
            }
            else if (indexOf == houseBuildings.Count - 1)
            {
                tileBuildings[tileNumber] = building;
                RefreshTileOutput(tileNumber);
                return true;
            }
        }
        return false;
    }
    public List<int> FilterTilesWithoutBuildings(List<int> tileList)
    {
        List<int> filteredList = new List<int>(tileList);
        for (int i = filteredList.Count - 1; i >= 0; i--)
        {
            if (BuildingOnTile(filteredList[i]))
            {
                filteredList.RemoveAt(i);
            }
        }
        return filteredList;
    }
    public int CountBuildingsOnTiles(List<int> tileList)
    {
        int count = 0;
        for (int i = 0; i < tileList.Count; i++)
        {
            if (BuildingOnTile(tileList[i]))
            {
                count++;
            }
        }
        return count;
    }
    public List<string> GetTileBuildings(){return tileBuildings;}
    public void ResetTileBuildings()
    {
        InitializeEmptyList();
        tileBuildings = new List<string>(emptyList);
    }
    public void SetTileBuildings(List<string> newInfo)
    {
        tileBuildings = newInfo;
        if (tileBuildings.Count < mapSize * mapSize)
        {
            InitializeEmptyList();
            tileBuildings = new List<string>(emptyList);
        }
    }
    protected List<string> luxuryTiles;
    public List<string> GetLuxuryTiles(){return luxuryTiles;}
    public void ResetLuxuryTiles()
    {
        InitializeEmptyList();
        luxuryTiles = new List<string>(emptyList);
    }
    public void SetLuxuryTiles(List<string> newInfo)
    {
        luxuryTiles = new List<string>(newInfo);
        if (luxuryTiles.Count < mapSize * mapSize)
        {
            InitializeEmptyList();
            luxuryTiles = new List<string>(emptyList);
        }
    }
    public void SetLuxuryTile(int tileNumber, string luxury)
    {
        luxuryTiles[tileNumber] = luxury;
    }
    public int RandomTileOfType(string type)
    {
        List<int> possibleNumbers = ReturnTileNumbersOfTileType(type);
        for (int i = possibleNumbers.Count - 1; i >= 0; i--)
        {
            if (luxuryTiles[possibleNumbers[i]] != "" || tileBuildings[possibleNumbers[i]] != "")
            {
                possibleNumbers.RemoveAt(i);
            }
        }
        if (possibleNumbers.Count <= 0)
        {
            return Random.Range(0, mapSize * mapSize);
        }
        return possibleNumbers[Random.Range(0, possibleNumbers.Count)];
    }
    public override int ReturnRandomTileOfTileTypes(List<string> tileTypes)
    {
        List<int> possibleNumbers = ReturnTileNumbersOfTileTypes(tileTypes);
        // Can't spawn on luxury tiles or buildings.
        for (int i = possibleNumbers.Count - 1; i >= 0; i--)
        {
            if (luxuryTiles[possibleNumbers[i]] != "" || tileBuildings[possibleNumbers[i]] != "")
            {
                possibleNumbers.RemoveAt(i);
            }
        }
        if (possibleNumbers.Count <= 0)
        {
            return Random.Range(0, mapSize * mapSize);
        }
        return possibleNumbers[Random.Range(0, possibleNumbers.Count)];
    }
    // Outputs are based on base tile + luxury + building.
    public StatDatabase baseOutputs;
    public StatDatabase luxuryOutputs;
    public StatDatabase buildingOutputs;
    protected List<string> tileOutputs; // Fight over tiles with good outputs.
    public bool OutputOnTile(int tile, string output)
    {
        string[] tOutput = ReturnTileOutput(tile).Split(outputDelimiter);
        return tOutput.Contains(output);
    }
    public int ReturnClosestTileWithOutput(int start, string output)
    {
        int tile = -1;
        int distance = mapSize * mapSize;
        // Forget about efficiency, just do it. X^3 is fine desu.
        for (int i = 0; i < tileOutputs.Count; i++)
        {
            if (ReturnTileOutput(i) == ""){continue;}
            string[] outputs = ReturnTileOutput(i).Split(outputDelimiter);
            if (outputs.Contains(output))
            {
                int newDist = mapUtility.DistanceBetweenTiles(start, i, mapSize);
                if (newDist < distance)
                {
                    distance = newDist;
                    tile = i;
                }
            }
        }
        return tile;
    }
    public int ReturnTileWithLargestOutput(List<int> tileList)
    {
        int output = 0;
        int tile = -1;
        for (int i = 0; i < tileList.Count; i++)
        {
            // Skip tiles without output.
            if (ReturnTileOutput(tileList[i]) == ""){continue;}
            string[] outputs = ReturnTileOutput(tileList[i]).Split(outputDelimiter);
            if (outputs.Length > output)
            {
                output = outputs.Length;
                tile = tileList[i];
            }
        }
        return tile;
    }
    public void RefreshTileOutput(int tileNumber, bool save = true)
    {
        string newOutputs = "";
        newOutputs += baseOutputs.ReturnValue(mapInfo[tileNumber]) + outputDelimiter;
        newOutputs += luxuryOutputs.ReturnValue(luxuryTiles[tileNumber]) + outputDelimiter;
        newOutputs += buildingOutputs.ReturnValue(tileBuildings[tileNumber]);
        tileOutputs[tileNumber] = newOutputs;
        if (buildingOutputs.ReturnValue(tileBuildings[tileNumber]).Contains("Remove"))
        {
            tileOutputs[tileNumber] = "";
        }
        if (save)
        {
            Save();
        }
    }
    public void RefreshAllTileOutputs()
    {
        InitializeEmptyList();
        tileOutputs = new List<string>(emptyList);
        for (int i = 0; i < mapInfo.Count; i++)
        {
            RefreshTileOutput(i, false);
        }
        Save();
    }
    public string ReturnTileOutput(int tileNumber)
    {
        return tileOutputs[tileNumber];
    }
    public List<string> GetTileOutputs(){return tileOutputs;}
    public void SetTileOutputs(List<string> newInfo)
    {
        tileOutputs = new List<string>(newInfo);
        if (tileOutputs.Count < mapSize * mapSize)
        {
            InitializeEmptyList();
            tileOutputs = new List<string>(emptyList);
        }
    }
    // Not saved, obtained from the faction manager each turn.
    public bool fastTurns;
    public CityManager cities;
    public FactionUnitManager units;
    public void TestNewDay()
    {
        UpdateMap();
    }
}