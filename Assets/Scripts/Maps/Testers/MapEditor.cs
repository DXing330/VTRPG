using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MapEditor : MapManager
{
    public MapEditorSaver savedData;
    [Header("Debug Loading")]
    public string debugMapNameToLoad;
    public GameObject nameRaterObject;
    // Change Name, Load Name, Copy To Name
    enum NameRaterState
    {
        renaming,
        loading,
        copying,
        newing
    }
    NameRaterState cNameRaterState;
    public NameRater nameRater;
    public PopUpMessage popUp;
    public string cMap;
    public TMP_Text cMapText;
    public void SetCMap(string newInfo)
    {
        cMap = newInfo;
        if (cMapText != null)
        {
            cMapText.text = cMap;
        }
    }
    [ContextMenu("Debug Load Map By Name")]
    public void DebugLoadMapByName()
    {
        if (savedData == null)
        {
            Debug.LogWarning("MapEditor debug load failed: missing savedData reference.");
            return;
        }
        if (string.IsNullOrWhiteSpace(debugMapNameToLoad))
        {
            Debug.LogWarning("MapEditor debug load failed: debugMapNameToLoad is empty.");
            return;
        }
        if (!savedData.KeyExists(debugMapNameToLoad))
        {
            Debug.LogWarning("MapEditor debug load failed: no saved map named " + debugMapNameToLoad);
            return;
        }
        SetCMap(debugMapNameToLoad);
        savedData.SetCurrentMap(this, cMap);
        Debug.Log("MapEditor debug loaded map: " + cMap);
    }
    [ContextMenu("Debug Refresh Saved Map Keys")]
    public void DebugRefreshSavedMapKeys()
    {
        if (savedData == null)
        {
            Debug.LogWarning("MapEditor debug refresh failed: missing savedData reference.");
            return;
        }
        if (selectMapToLoad != null)
        {
            selectMapToLoad.SetSelectables(savedData.GetAllKeys());
        }
        Debug.Log("MapEditor debug refreshed saved map keys.");
    }
    public void DeleteMap()
    {
        savedData.DeleteKey(cMap);
        SetCMap("TestMap0");
        savedData.SetCurrentMap(this, cMap);
    }
    public void MakeNewMap()
    {
        cNameRaterState = NameRaterState.newing;
        nameRaterObject.SetActive(true);
        nameRater.ResetNewName();
    }
    public void RenameCurrentMap()
    {
        cNameRaterState = NameRaterState.renaming;
        nameRaterObject.SetActive(true);
        nameRater.ResetNewName();
    }
    public SelectList selectMapToLoad;
    public void ChangeCurrentMap()
    {
        // Terribly Inefficent.
        // Should show a select list with keys and then select a key from the select list.
        if (selectMapToLoad.GetSelected() < 0){return;}
        if (editBattle)
        {
            cMap = selectMapToLoad.GetSelectedString();
        }
        else
        {
            SetCMap(selectMapToLoad.GetSelectedString());
        }
        savedData.SetCurrentMap(this, cMap);
        /*cNameRaterState = NameRaterState.loading;
        nameRaterObject.SetActive(true);
        nameRater.ResetNewName();*/
    }
    public void ForceCMap(string newMap)
    {
        cMap = newMap;
        savedData.SetCurrentMap(this, cMap);
    }
    public bool copying;
    // Need to be able to copy maps to other maps.
    public void CopyToOtherMap()
    {
        cNameRaterState = NameRaterState.copying;
        nameRaterObject.SetActive(true);
        nameRater.ResetNewName();
    }
    public void NameRaterConfirm()
    {
        string newName = nameRater.ConfirmName();
        if (newName.Length <= 0){return;}
        switch (cNameRaterState)
        {
            case NameRaterState.copying:
            // Overwrite the new key.
            savedData.SaveMapToName(this, newName);
            break;
            case NameRaterState.renaming:
            // Check if the new key is available.
            if (savedData.KeyExists(newName))
            {
                popUp.SetMessage("There Is Already A Map Saved With This Name");
                break;
            }
            // Remove the current key.
            savedData.DeleteKey(cMap);
            // Save to the new key.
            SetCMap(newName);
            savedData.SaveMap(this);
            selectMapToLoad.SetSelectables(savedData.GetAllKeys());
            break;
            case NameRaterState.newing:
            // Check if the new key is available.
            if (savedData.KeyExists(newName))
            {
                popUp.SetMessage("There Is Already A Map Saved With This Name");
                break;
            }
            SetCMap(newName);
            // Make a new map.
            InitializeNewMap();
            savedData.SaveMap(this);
            selectMapToLoad.SetSelectables(savedData.GetAllKeys());
            break;
            case NameRaterState.loading:
            // Check if the new key exists.
            if (!savedData.KeyExists(newName))
            {
                popUp.SetMessage("No Map Saved With This Name");
                break;
            }
            // Load the new key.
            SetCMap(newName);
            savedData.SetCurrentMap(this, cMap);
            break;
        }
        nameRaterObject.SetActive(false);
        nameRater.ResetNewName();
    }
    public string defaultTile = "Plains";
    [ContextMenu("Initialize")]
    public void InitializeNewMap()
    {
        mapInfo.Clear();
        tileElevations.Clear();
        terrainEffects.Clear();
        borders.Clear();
        buildings.Clear();
        for (int i = 0; i < mapTiles.Count; i++)
        {
            // Tiles.
            mapInfo.Add(defaultTile);
            // Elevations.
            tileElevations.Add("0");
            //TEffects.
            terrainEffects.Add("");
            // Borders.
            mapTiles[i].ResetBorders();
            borders.Add(mapTiles[i].ReturnBorderString());
            // Buildings.
            buildings.Add("");
        }
        UndoEdits();
        UpdateMap();
    }
    // Map Manager Stuff.
    public override void UpdateMap()
    {
        UpdateCurrentTiles();
        mapDisplayers[0].DisplayCurrentTiles(mapTiles, cMapInfo, currentTiles);
        mapDisplayers[1].DisplayCurrentTiles(mapTiles, cBuildings, currentTiles);
        mapDisplayers[3].DisplayCurrentTiles(mapTiles, cTerrainEffects, currentTiles);
        for (int i = 0; i < cTileElevations.Count; i++)
        {
            mapTiles[i].SetElevation(int.Parse(cTileElevations[i]));
            mapTiles[i].UpdateElevationSprite(elevationSprites.SpriteDictionary("E"+mapTiles[i].GetElevation().ToString()));
        }
        // Borders.
        for (int i = 0; i < cBorders.Count; i++)
        {
            mapTiles[i].SetBorders(cBorders[i].Split("|").ToList());
            UpdateTileBorderSprites(i);
        }
    }
    public void UpdateMapWithActors(List<string> actors, List<string> actorLocations)
    {
        UpdateMap();
        List<string> actorTiles = new List<string>();
        for (int i = 0; i < cMapInfo.Count; i++)
        {
            actorTiles.Add("");
        }
        for (int i = 0; i < actorLocations.Count; i++)
        {
            actorTiles[int.Parse(actorLocations[i])] = actors[i];
        }
        mapDisplayers[2].DisplayCurrentTiles(mapTiles, actorTiles, currentTiles);
    }
    public ColorDictionary colorDictionary;
    public void HighlightTile(int tile)
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].ResetHighlight();
        }
        mapTiles[tile].HighlightTile(colorDictionary.GetColorByName("Blue"));
    }
    public void HighlightSelectedTiles()
    {
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].ResetHighlight();
        }
        for (int i = 0; i < selectedTiles.Count; i++)
        {
            mapTiles[selectedTiles[i]].HighlightTile(colorDictionary.GetColorByName("Blue"));
        }
    }
    // Can Save And Undo Edits.
    public List<string> tileElevations;
    public List<string> terrainEffects;
    public List<string> borders;
    public List<string> buildings;
    public List<string> cMapInfo;
    public List<string> cTileElevations;
    public List<string> cTerrainEffects;
    public List<string> cBorders;
    public List<string> cBuildings;
    public void SaveEdits()
    {
        mapInfo = new List<string>(cMapInfo);
        terrainEffects = new List<string>(cTerrainEffects);
        borders = new List<string>(cBorders);
        buildings = new List<string>(cBuildings);
        tileElevations = new List<string>(cTileElevations);
        savedData.SaveMap(this);
        selectMapToLoad.SetSelectables(savedData.GetAllKeys());
    }
    public void UndoEdits()
    {
        cMapInfo = new List<string>(mapInfo);
        cTerrainEffects = new List<string>(terrainEffects);
        cBorders = new List<string>(borders);
        cBuildings = new List<string>(buildings);
        cTileElevations = new List<string>(tileElevations);
        UpdateMap();
    }
    // STATE THINGS.
    // Can Select Shape/Span To Multiselect Tiles.
    // Workflow: Select Tile -> Select Edits
    protected override void Start()
    {
        selectMapToLoad.SetSelectables(savedData.GetAllKeys());
        if (editBattle){return;}
        shapeSelect.SetSelectables(shapes);
        List<string> spans = new List<string>();
        for (int i = 0; i < mapSize / 2; i++)
        {
            spans.Add(i.ToString());
        }
        spanSelect.SetSelectables(spans);
        SetCMap("TestMap0");
        savedData.SetCurrentMap(this, cMap);
        ChangeLayer(true);
        ChangeLayer(false);
    }
    public SpinnerMenu shapeSelect;
    public List<string> shapes;
    public string GetShape()
    {
        return shapeSelect.GetSelected();
    }
    public SpinnerMenu spanSelect;
    public int GetSpan()
    {
        return int.Parse(spanSelect.GetSelected());
    }
    public SpinnerMenu layerSelect;
    public void ChangeLayer(bool right = true)
    {
        layerSelect.ChangeIndex(right);
        // Set Layer Specifics.
        switch (GetLayer())
        {
            default:
            SetLayerSpecifics(allTiles.GetAllKeys());
            break;
            case "TEffect":
            SetLayerSpecifics(allTEffects.GetAllKeys());
            break;
            case "Building":
            SetLayerSpecifics(allBuildings.GetAllKeys());
            break;
            case "Border":
            SetLayerSpecifics(borderSpecifics);
            break;
            case "Elevation":
            SetLayerSpecifics(elevationSpecifics);
            break;
        }
    }
    public string GetLayer()
    {
        return layerSelect.GetSelected();
    }
    // Need To Know Possible Tile/TEffects/Buildings.
    public StatDatabase allTiles;
    public StatDatabase allTEffects;
    public StatDatabase allBuildings;
    public string defaultBorder = "Wall";
    // Outer, All, None, NE/E/SE/SW/W/NW
    public List<string> borderSpecifics;
    // Increase, Decrease, Max, Min
    public List<string> elevationSpecifics;
    public SpinnerMenu layerSpecificSelect;
    public void ChangeLayerSpecifics(bool right = true)
    {
        layerSpecificSelect.ChangeIndex(right);
    }
    public void SetLayerSpecifics(List<string> newSpecifics)
    {
        layerSpecificSelect.SetSelectables(newSpecifics);
    }
    public string GetLayerSpecifics()
    {
        return layerSpecificSelect.GetSelected();
    }
    public List<int> selectedTiles;
    public BattleMapEditor battleMapEditor;
    public bool editBattle = false;
    public override void ClickOnTile(int tileNumber)
    {
        if (editBattle)
        {
            battleMapEditor.ClickOnTile(tileNumber);
            return;
        }
        if (tileNumber < 0)
        {
            selectedTiles.Clear();
            return;
        }
        if (GetShape() == "All")
        {
            selectedTiles.Clear();
            {
                for (int i = 0; i < mapInfo.Count; i++)
                {
                    selectedTiles.Add(i);
                }
            }
            HighlightSelectedTiles();
            return;
        }
        // Update the selected tiles.
        selectedTiles = mapUtility.GetTilesByShapeSpan(tileNumber, GetShape(), GetSpan(), mapSize);
        HighlightSelectedTiles();
    }
    public void ChangeLayer()
    {
        if (selectedTiles.Count <= 0){return;}
        switch (GetLayer())
        {
            // Change the tiles.
            default:
            for (int i = 0; i < selectedTiles.Count; i++)
            {
                ChangeTile(selectedTiles[i], GetLayerSpecifics());
            }
            break;
            case "TEffect":
            for (int i = 0; i < selectedTiles.Count; i++)
            {
                ChangeTEffect(selectedTiles[i], GetLayerSpecifics());
            }
            break;
            case "Building":
            for (int i = 0; i < selectedTiles.Count; i++)
            {
                ChangeBuilding(selectedTiles[i], GetLayerSpecifics());
            }
            break;
            // This requires special work.
            case "Border":
            ChangeBorders(selectedTiles, GetLayerSpecifics());
            break;
            // This requires special work.
            case "Elevation":
            for (int i = 0; i < selectedTiles.Count; i++)
            {
                ChangeElevation(selectedTiles[i], GetLayerSpecifics());
            }
            break;
        }
        UpdateMap();
    }
    protected void ChangeTile(int tileNumber, string change)
    {
        cMapInfo[tileNumber] = change;
        // Change the elevation.
        cTileElevations[tileNumber] = MiddleElevation(change).ToString();
    }
    protected void ChangeElevation(int tileNumber, string change)
    {
        int cElv = int.Parse(cTileElevations[tileNumber]);
        int maxElv = MaxElevation(cMapInfo[tileNumber]);
        int minElv = MinElevation(cMapInfo[tileNumber]);
        switch (change)
        {
            case "Increase":
            if (cElv < maxElv)
            {
                cElv++;
            }
            break;
            case "Decrease":
            if (cElv > minElv)
            {
                cElv--;
            }
            break;
            case "Max":
            cElv = maxElv;
            break;
            case "Min":
            cElv = minElv;
            break;
        }
        cTileElevations[tileNumber] = cElv.ToString();
    }
    protected void ChangeTEffect(int tileNumber, string change)
    {
        cTerrainEffects[tileNumber] = change;
    }
    protected void ChangeBuilding(int tileNumber, string change)
    {
        cBuildings[tileNumber] = change;
    }
    public List<string> directions;
    public List<int> dirInts;
    protected void ChangeBorders(List<int> tileNumbers, string change)
    {
        if (change == "Outer")
        {
            for (int i = 0; i < tileNumbers.Count; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    // Check if a tile is in the direction.
                    if (!tileNumbers.Contains(mapUtility.PointInDirection(tileNumbers[i], j, mapSize)))
                    {
                        // Add the border in that direction.
                        mapTiles[tileNumbers[i]].ChangeBorder(defaultBorder, j);
                    }
                }
                cBorders[tileNumbers[i]] = mapTiles[tileNumbers[i]].ReturnBorderString();
            }
            return;
        }
        for (int i = 0; i < tileNumbers.Count; i++)
        {
            if (change == "All")
            {
                mapTiles[tileNumbers[i]].ChangeAllBorders(defaultBorder);
            }
            else if (change == "None")
            {
                mapTiles[tileNumbers[i]].ResetBorders();
            }
            else
            {
                // Direction based.
                int direction = dirInts[directions.IndexOf(change)];
                mapTiles[tileNumbers[i]].ChangeBorder(defaultBorder, direction);
            }
            cBorders[tileNumbers[i]] = mapTiles[tileNumbers[i]].ReturnBorderString();
        }
    }
}
