using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CustomEditor(typeof(AutoChessMapAsset))]
public class AutoChessMapEditor : Editor
{
    private StartingMapTile selectedTile = StartingMapTile.Plains;
    public override void OnInspectorGUI()
    {
        AutoChessMapAsset map = (AutoChessMapAsset)target;
        DrawDefaultInspector();
        GUILayout.Space(10);
        GUILayout.Label("Tile Painter", EditorStyles.boldLabel);
        // Select which tile to paint.
        selectedTile = (StartingMapTile)EditorGUILayout.EnumPopup("Current Tile", selectedTile);
        GUILayout.Space(10);
        // Make sure the array exists.
        int size = map.gridSize * map.gridSize;
        if (map.tiles == null)
        {
            map.tiles = new StartingMapTile[size];
        }
        // Top row (column buttons)
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(25); // Space for row buttons
        for (int x = 0; x < map.gridSize; x++)
        {
            if (GUILayout.Button("↓", GUILayout.Width(40), GUILayout.Height(20)))
            {
                Undo.RecordObject(map, "Paint Column");
                for (int y = 0; y < map.gridSize; y++)
                {
                    map.tiles[y * map.gridSize + x] = selectedTile;
                }
                EditorUtility.SetDirty(map);
            }
        }
        EditorGUILayout.EndHorizontal();
        // Grid
        for (int y = 0; y < map.gridSize; y++)
        {
            EditorGUILayout.BeginHorizontal();
            // Row button
            if (GUILayout.Button("→", GUILayout.Width(20), GUILayout.Height(40)))
            {
                Undo.RecordObject(map, "Paint Row");
                for (int x = 0; x < map.gridSize; x++)
                {
                    map.tiles[y * map.gridSize + x] = selectedTile;
                }
                EditorUtility.SetDirty(map);
            }
            // Tiles
            for (int x = 0; x < map.gridSize; x++)
            {
                int index = y * map.gridSize + x;
                GUI.backgroundColor = GetColor(map.tiles[index]);
                if (GUILayout.Button(GetLabel(map.tiles[index]), GUILayout.Width(40), GUILayout.Height(40)))
                {
                    Undo.RecordObject(map, "Paint Tile");
                    map.tiles[index] = selectedTile;
                    EditorUtility.SetDirty(map);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        GUI.backgroundColor = Color.white;
    }
    private Color GetColor(StartingMapTile tile)
    {
        switch (tile)
        {
            case StartingMapTile.Plains:
                return new Color(0.5f, 0.9f, 0.5f);
            case StartingMapTile.Forest:
                return new Color(0.2f, 0.6f, 0.2f);
            case StartingMapTile.Water:
                return new Color(0.3f, 0.6f, 1f);
            case StartingMapTile.Mountain:
                return Color.gray;
            default:
                return Color.white;
        }
    }
    private string GetLabel(StartingMapTile tile)
    {
        switch (tile)
        {
            case StartingMapTile.Plains:
                return "P";
            case StartingMapTile.Forest:
                return "F";
            case StartingMapTile.Water:
                return "W";
            case StartingMapTile.Mountain:
                return "M";
            case StartingMapTile.Pit:
                return " ";
            default:
                return "?";
        }
    }
}
