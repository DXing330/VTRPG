using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapDisplayer", menuName = "ScriptableObjects/UI/MapDisplayer", order = 1)]
public class MapDisplayer : ScriptableObject
{
    public int layer = 0;
    public List<SpriteContainer> layerSprites;
    public ColorDictionary colorDictionary;

    public void DebugDisplayCurrentTiles(List<MapTile> mapTiles, List<string> mapInfo, List<int> currentTiles)
    {
        int nextTile = -1;
        for (int i = 0; i < (mapTiles.Count); i++)
        {
            nextTile = currentTiles[i];
            if (nextTile < 0 || mapInfo[nextTile].Length < 1)
            {
                mapTiles[i].ResetLayerSprite(layer);
                continue;
            }
            mapTiles[i].UpdateLayerSprite(layerSprites[layer].SpriteDictionary(mapInfo[nextTile]), layer);
        }
    }

    public void ResetCurrentTiles(List<MapTile> mapTiles)
    {
        for (int i = 0; i < (mapTiles.Count); i++)
        {
            mapTiles[i].ResetLayerSprite(layer);
        }
    }

    public void DisplayCurrentOverworldTiles(List<MapTile> mapTiles, List<string> mapInfo, List<int> currentTiles, string outOfBoundsTile = "DeepWater")
    {
        int nextTile = -1;
        for (int i = 0; i < (mapTiles.Count); i++)
        {
            nextTile = currentTiles[i];
            if (nextTile < 0 || mapInfo[nextTile].Length < 1)
            {
                //mapTiles[i].ResetLayerSprite(layer);
                mapTiles[i].UpdateLayerSprite(layerSprites[layer].SpriteDictionary(outOfBoundsTile), layer);
                continue;
            }
            mapTiles[i].UpdateLayerSprite(layerSprites[layer].SpriteDictionary(mapInfo[nextTile]), layer);
        }
    }
    
    public void DisplayCurrentTiles(List<MapTile> mapTiles, List<string> mapInfo, List<int> currentTiles, bool updateDirections = false, List<string> actorDirections = null)
    {
        int nextTile = -1;
        for (int i = 0; i < (mapTiles.Count); i++)
        {
            nextTile = currentTiles[i];
            if (nextTile < 0 || nextTile >= mapInfo.Count || mapInfo[nextTile].Length < 1)
            {
                mapTiles[i].ResetLayerSprite(layer);
                continue;
            }
            mapTiles[i].UpdateLayerSprite(layerSprites[layer].SpriteDictionary(mapInfo[nextTile]), layer);
        }
        if (updateDirections)
        {
            for (int i = 0; i < (mapTiles.Count); i++)
            {
                mapTiles[i].UpdateDirectionArrow((actorDirections[currentTiles[i]]));
            }
        }
    }

    public void DisplayCurrentStyledTiles(List<MapTile> mapTiles, List<string> mapInfo, List<int> currentTiles, bool updateDirections = false, List<string> actorDirections = null)
    {
        int nextTile = -1;
        float spriteScale = 1f;
        for (int i = 0; i < (mapTiles.Count); i++)
        {
            nextTile = currentTiles[i];
            if (nextTile < 0 || nextTile >= mapInfo.Count || mapInfo[nextTile].Length < 1)
            {
                mapTiles[i].ResetLayerSprite(layer);
                continue;
            }
            if (!float.TryParse(layerSprites[layer].GetSize(mapInfo[nextTile]), out spriteScale))
            {
                spriteScale = 1f;
            }
            mapTiles[i].UpdateStyledLayerSprite(
                layerSprites[layer].SpriteDictionary(mapInfo[nextTile]),
                layerSprites[layer].GetColor(mapInfo[nextTile], mapTiles[i].GetDefaultLayerColor(layer)),
                spriteScale,
                layer
            );
        }
        if (updateDirections)
        {
            for (int i = 0; i < (mapTiles.Count); i++)
            {
                mapTiles[i].UpdateDirectionArrow((actorDirections[currentTiles[i]]));
            }
        }
    }

    public void ResetHighlights(List<MapTile> mapTiles)
    {
        for (int i = 0; i < (mapTiles.Count); i++)
        {
            mapTiles[i].ResetHighlight();
        }
    }

    public void HighlightCurrentTiles(List<MapTile> mapTiles, List<string> mapInfo, List<int> currentTiles)
    {
        for (int i = 0; i < (mapTiles.Count); i++)
        {
            if (currentTiles[i] < 0)
            {
                mapTiles[i].ResetHighlight();
                continue;
            }
            mapTiles[i].HighlightTile(colorDictionary.GetColorByName(mapInfo[currentTiles[i]]));
        }
    }

    public void HighlightTileSet(List<MapTile> mapTiles, List<int> tileSet, List<int> currentTiles, string highlightColor = "Blue")
    {
        for (int i = 0; i < tileSet.Count; i++)
        {
            int indexOf = currentTiles.IndexOf(tileSet[i]);
            if (indexOf < 0)
            {
                continue;
            }
            mapTiles[indexOf].HighlightTile(colorDictionary.GetColorByName(highlightColor));
        }
    }

    public void HighlightTilesInSetColor(List<MapTile> mapTiles, List<int> currentTiles, string color)
    {
        for (int i = 0; i < (currentTiles.Count); i++)
        {
            if (currentTiles[i] < 0)
            {
                mapTiles[i].ResetHighlight();
                continue;
            }
            mapTiles[currentTiles[i]].HighlightTile(colorDictionary.GetColorByName(color));
        }
    }
}
