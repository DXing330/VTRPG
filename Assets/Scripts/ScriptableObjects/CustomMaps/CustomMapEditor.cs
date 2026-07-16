using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class MapPaintBrush
{
    // 0 = Tile, 1 = Building, 2 = TEffect, 3 = Elevation, 4 = Border
    public int layer = 0;
    public int radius = 0;
    public MapTileTypes selectedTile = MapTileTypes.Plains;
    public MapBuildingTypes selectedBuilding = MapBuildingTypes.None;
    public MapEffectTypes selectedEffects = MapEffectTypes.None;
    public string selectedBorder = "None";
    public string selectedBorderDirection = "NorthEast";
    public int selectedElevation = 0;
}
public class CustomMapEditor : ClickTileManager
{
    public GeneralUtility utility;
    public MapUtility mapUtility;
    public MapPaintBrush brush = new();
    public SpinnerMenu brushLayers;
    public void ChangeLayer()
    {
        brush.layer = brushLayers.GetSelectedIndex();
        utility.DisableGameObjects(brushLayerObjects);
        brushLayerObjects[brush.layer].SetActive(true);
        brushDirectionObject.SetActive(brush.layer == 4);
    }
    public SpinnerMenu brushRadius;
    public void ChangeRadius()
    {
        brush.radius = brushRadius.GetSelectedIndex();
    }
    public List<GameObject> brushLayerObjects;
    public GameObject brushDirectionObject;
    public SpinnerMenu brushTiles;
    public void ChangeSelectedTile()
    {
        brush.selectedTile = Enum.Parse<MapTileTypes>(brushTiles.GetSelected());
    }
    public SpinnerMenu brushBuildings;
    public void ChangeSelectedBuilding()
    {
        brush.selectedBuilding = Enum.Parse<MapBuildingTypes>(brushBuildings.GetSelected());
    }
    public SpinnerMenu brushEffects;
    public void ChangeSelectedEffect()
    {
        brush.selectedEffects = Enum.Parse<MapEffectTypes>(brushEffects.GetSelected());
    }
    public SpinnerMenu brushElevations;
    public void ChangeSelectedElevation()
    {
        brush.selectedElevation = int.Parse(brushElevations.GetSelected());
    }
    public SpinnerMenu brushBorders;
    public void ChangeSelectedBorder()
    {
        brush.selectedBorder = brushBorders.GetSelected();
    }
    public SpinnerMenu brushBorderDirections;
    public void ChangeSelectedDirection()
    {
        brush.selectedBorderDirection = brushBorderDirections.GetSelected();
    }
    void Start()
    {
        UpdateCurrentTiles();
        UpdateMap();
    }
    public List<int> GetPaintedTiles(int tileNumber)
    {
        int radius = brush.radius;
        return mapUtility.GetTilesInCircleShape(tileNumber, radius, mapSize);
    }
    public override void ClickOnTile(int tileNumber)
    {
        PaintDraggedTile(tileNumber);
    }
    public void PaintDraggedTile(int tileNumber)
    {
        List<int> tiles = GetPaintedTiles(tileNumber);
        for (int i = 0; i < tiles.Count; i++)
        {
            PaintTile(tiles[i]);
        }
        UpdateMap();
    }
    protected void PaintTile(int index)
    {
        switch (brush.layer)
        {
            // 0
            default:
            currentMap.tiles[index] = brush.selectedTile;
            break;
            case 1:
            currentMap.buildings[index] = brush.selectedBuilding;
            break;
            case 2:
            currentMap.effects[index] = brush.selectedEffects;
            break;
            case 3:
            currentMap.elevations[index] = brush.selectedElevation;
            break;
            case 4:
            currentMap.borders[index].UpdateBorder(brush.selectedBorder, brush.selectedBorderDirection);
            break;
        }
    }
    public List<CustomMapAsset> customMaps;
    // TODO Select Map From A Select List Of Map Names.
    public CustomMapAsset currentMap;
    public void LoadMap(CustomMapAsset map)
    {
        currentMap = map;
        RefreshDisplayLists();
        UpdateMap();
    }
    public void SaveMap()
    {
        if(currentMap == null){return;}
        #if UNITY_EDITOR
            EditorUtility.SetDirty(currentMap);
            AssetDatabase.SaveAssets();
        #endif
    }
    public List<MapTile> mapTiles;
    public List<MapDisplayer> mapDisplayers;
    public SpriteContainer elevationSprites;
    public SpriteContainer borderSprites;
    public int mapSize = 15;
    public List<int> currentTiles;
    protected virtual void UpdateCurrentTiles()
    {
        currentTiles = new List<int>();
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            currentTiles.Add(i);
        }
    }
    // Get The Lists From The Current Map.
    List<string> mapInfo = new List<string>();
    protected List<string> GetMapInfo()
    {
        mapInfo.Clear();
        for (int i = 0; i < currentMap.tiles.Length; i++)
        {
            mapInfo.Add(currentMap.tiles[i].ToString());
        }
        return mapInfo;
    }
    List<string> buildingInfo = new List<string>();
    protected List<string> GetBuildingInfo()
    {
        buildingInfo.Clear();
        for (int i = 0; i < currentMap.buildings.Length; i++)
        {
            buildingInfo.Add(currentMap.buildings[i].ToString());
        }
        return buildingInfo;
    }
    List<string> effectInfo = new List<string>();
    protected List<string> GetTerrainEffectInfo()
    {
        effectInfo.Clear();
        for (int i = 0; i < currentMap.effects.Length; i++)
        {
            effectInfo.Add(currentMap.effects[i].ToString());
        }
        return effectInfo;
    }
    List<int> elevations = new List<int>();
    protected List<int> GetElevationInfo()
    {
        elevations.Clear();
        for (int i = 0; i < currentMap.elevations.Length; i++)
        {
            elevations.Add(currentMap.elevations[i]);
        }
        return elevations;
    }
    List<string> borders = new List<string>();
    protected List<string> GetBorderInfo()
    {
        borders.Clear();
        for (int i = 0; i < currentMap.borders.Length; i++)
        {
            borders.Add(currentMap.borders[i].ReturnBorderString());
        }
        return borders;
    }
    protected void UpdateTileBorderSprites(int tileNumber)
    {
        List<string> borders = mapTiles[tileNumber].GetBorders();
        for (int i = 0; i < borders.Count; i++)
        {
            mapTiles[tileNumber].UpdateBorderImage(i, borderSprites.SpriteDictionary(borders[i]));
        }
    }
    protected void RefreshDisplayLists()
    {
        GetMapInfo();
        GetBuildingInfo();
        GetTerrainEffectInfo();
        GetElevationInfo();
        GetBorderInfo();
    }
    public void UpdateMap()
    {
        if (currentMap == null){return;}
        RefreshDisplayLists();
        // Update The Map Based On The Current Custom Map.
        mapDisplayers[0].DisplayCurrentTiles(mapTiles, mapInfo, currentTiles);
        mapDisplayers[1].DisplayCurrentTiles(mapTiles, buildingInfo, currentTiles);
        mapDisplayers[3].DisplayCurrentTiles(mapTiles, effectInfo, currentTiles);
        for (int i = 0; i < elevations.Count; i++)
        {
            mapTiles[i].SetElevation(elevations[i]);
            mapTiles[i].UpdateElevationSprite(elevationSprites.SpriteDictionary("E" + mapTiles[i].GetElevation().ToString()));
        }
        // Borders.
        for (int i = 0; i < borders.Count; i++)
        {
            mapTiles[i].SetBorders(borders[i].Split("|").ToList());
            UpdateTileBorderSprites(i);
        }
    }
}