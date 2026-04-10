using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 0 1 2 3
// 4 5 6 7
// 8 9 0 1
[CreateAssetMenu(fileName = "RectMapUtility", menuName = "ScriptableObjects/Utility/RectMapUtility", order = 1)]
public class RectMapUtility : ScriptableObject
{
    public GeneralUtility utility;
    public int GetRow(int tile, int rows, int columns)
    {
        return tile / columns;
    }
    public int GetColumn(int tile, int rows, int columns)
    {
        return (tile % columns);
    }
    public int ReturnTileNumberFromRowCol(int row, int column, int columns)
    {
        return row * columns + column;
    }
    public List<int> GetDiagonalAdjacentTiles(int tile, int rows, int cols)
    {
        List<int> neighbors = new List<int>(); // max 4 diagonals
        int row = tile / cols;
        int col = tile % cols;
        bool hasUp = row > 0;
        bool hasDown = row < rows - 1;
        bool hasLeft = col > 0;
        bool hasRight = col < cols - 1;
        if (hasUp && hasLeft)
        {
            neighbors.Add(tile - cols - 1); // up-left
        }
        if (hasDown && hasLeft)
        {
            neighbors.Add(tile + cols - 1); // down-left
        }
        if (hasUp && hasRight)
        {
            neighbors.Add(tile - cols + 1); // up-right
        }
        if (hasDown && hasRight)
        {
            neighbors.Add(tile + cols + 1); // down-right
        }
        return neighbors;
    }
    public List<int> GetRightDiagonalAdjacentTiles(int tile, int rows, int cols)
    {
        List<int> neighbors = new List<int>(); // max 4 diagonals
        int row = GetRow(tile, rows, cols);
        int col = GetColumn(tile, rows, cols);
        bool hasUp = row > 0;
        bool hasDown = row < rows - 1;
        bool hasLeft = col > 0;
        bool hasRight = col < cols - 1;
        if (hasUp && hasRight)
        {
            neighbors.Add(tile - cols + 1); // up-right
        }
        if (hasDown && hasRight)
        {
            neighbors.Add(tile + cols + 1); // down-right
        }
        return neighbors;
    }
    public List<int> GetLeftDiagonalAdjacentTiles(int tile, int rows, int cols)
    {
        List<int> neighbors = new List<int>(); // max 4 diagonals
        int row = GetRow(tile, rows, cols);
        int col = GetColumn(tile, rows, cols);
        bool hasUp = row > 0;
        bool hasDown = row < rows - 1;
        bool hasLeft = col > 0;
        bool hasRight = col < cols - 1;
        if (hasUp && hasLeft)
        {
            neighbors.Add(tile - cols - 1); // up-left
        }
        if (hasDown && hasLeft)
        {
            neighbors.Add(tile + cols - 1); // down-left
        }
        return neighbors;
    }
    public int VerticalDistanceBetweenTiles(int tileA, int tileB, int cols)
    {
        int rowA = tileA / cols;
        int rowB = tileB / cols;
        return Mathf.Abs(rowA - rowB);
    }
    public int GetUpRightDiagonal(int tile, int rows, int cols)
    {
        int row = GetRow(tile, rows, cols);
        int col = GetColumn(tile, rows, cols);
        bool hasUp = row > 0;
        bool hasDown = row < rows - 1;
        bool hasRight = col < cols - 1;
        if (hasUp && hasRight)
        {
            return tile - cols + 1;
        }
        return -1;
    }
    public int GetDownRightDiagonal(int tile, int rows, int cols)
    {
        int row = GetRow(tile, rows, cols);
        int col = GetColumn(tile, rows, cols);
        bool hasUp = row > 0;
        bool hasDown = row < rows - 1;
        bool hasRight = col < cols - 1;
        if (hasDown && hasRight)
        {
            return (tile + cols + 1); // down-right
        }
        return -1;
    }
}
