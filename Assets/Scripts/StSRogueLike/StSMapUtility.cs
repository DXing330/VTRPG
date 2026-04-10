using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StSMapUtility : MonoBehaviour
{
    public RectMapUtility mapUtility;
    public RNGUtility mapRNGSeed;

    public int RandomPointRight(int location, int rows, int cols)
    {
        List<int> points = mapUtility.GetRightDiagonalAdjacentTiles(location, rows, cols);
        return points[Random.Range(0, points.Count)];
    }

    public int RandomPointLeft(int location, int rows, int cols)
    {
        List<int> points = mapUtility.GetLeftDiagonalAdjacentTiles(location, rows, cols);
        return points[Random.Range(0, points.Count)];
    }

    public List<int> CreatePath(int startRow, int rows = 9, int cols = 17)
    {
        List<int> path = new List<int>();
        int point = mapUtility.ReturnTileNumberFromRowCol(startRow, 0, cols);
        path.Add(point);
        for (int i = 0; i < cols - 1; i++)
        {
            point = RandomPointRight(point, rows, cols);
            path.Add(point);
        }
        return path;
    }
}
