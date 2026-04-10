using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapPatterns", menuName = "ScriptableObjects/Utility/MapPatterns", order = 1)]
public class MapPatternLocations : ScriptableObject
{
    public MapUtility mapUtility;

    protected int ReturnOscillatingFromMiddle(int number)
    {
        int sign = 1;
        if (number%2 == 0){sign = -1;}
        return (((number+1)/2)*(sign));
    }

    public List<int> ReturnTilesOfPattern(string pattern, int number, int mapSize)
    {
        List<int> patternTiles = new List<int>();
        for (int i = 0; i < number; i++)
        {
            switch (pattern)
            {
                default:
                patternTiles.Add(SingleSideSpawnPattern(pattern, i, mapSize));
                break;
                case "Center":
                patternTiles.Add(CenterSpawnPattern(i, mapSize));
                break;
                case "Outer":
                patternTiles.Add(OuterSpawnPattern(i, mapSize));
                break;
            }
        }
        return patternTiles;
    }

    protected int CenterSpawnPattern(int index, int mapSize)
    {
        return mapUtility.SpiralOutward(index, mapSize);
    }

    protected int OuterSpawnPattern(int index, int mapSize)
    {
        return mapUtility.SpiralInward(index, mapSize);
    }

    protected int SingleSideSpawnPattern(string pattern, int index, int mapSize)
    {
        int row = -1;
        int column = -1;
        int adjustment = ReturnOscillatingFromMiddle(index);
        switch (pattern)
        {
            // Right.
            case "Right":
                column = mapSize - 1;
                row = mapSize/2 + adjustment;
                if (row < 0 || row >= mapSize)
                {
                    for (int i = 0; i < mapSize; i++)
                    {
                        column--;
                        index -= mapSize;
                        row = mapSize/2 + ReturnOscillatingFromMiddle(index);
                        if (row >= 0 && row < mapSize){break;}
                    }
                }
                break;
            // Left.
            case "Left":
                column = 0;
                row = mapSize/2 + adjustment;
                if (row < 0 || row >= mapSize)
                {
                    for (int i = 0; i < mapSize; i++)
                    {
                        column++;
                        index -= mapSize;
                        row = mapSize/2 + ReturnOscillatingFromMiddle(index);
                        if (row >= 0 && row < mapSize){break;}
                    }
                }
                break;
            // Up.
            case "Top":
                row = 0;
                column = mapSize/2 + adjustment;
                if (column < 0 || column >= mapSize)
                {
                    for (int i = 0; i < mapSize; i++)
                    {
                        row++;
                        index -= mapSize;
                        column = mapSize/2 + ReturnOscillatingFromMiddle(index);
                        if (column >= 0 && column < mapSize){break;}
                    }
                }
                break;
            // Down.
            case "Bot":
                row = mapSize - 1;
                column = mapSize/2 + adjustment;
                if (column < 0 || column >= mapSize)
                {
                    for (int i = 0; i < mapSize; i++)
                    {
                        row--;
                        index -= mapSize;
                        column = mapSize/2 + ReturnOscillatingFromMiddle(index);
                        if (column >= 0 && column < mapSize){break;}
                    }
                }
                break;
        }
        return mapUtility.ReturnTileNumberFromRowCol(row, column, mapSize);
    }
}
