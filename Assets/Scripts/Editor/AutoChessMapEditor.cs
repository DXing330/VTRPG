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
        GUILayout.Space(10);
        GUILayout.Label("Tile Painter", EditorStyles.boldLabel);
        // Select which tile to paint.
        selectedTile = (StartingMapTile)EditorGUILayout.EnumPopup("Current Tile", selectedTile);
        GUILayout.Space(10);
        // Make sure the array exists.
        if (map.tiles == null || map.tiles.Length != 49)
        {
            map.tiles = new StartingMapTile[49];
        }
        // Draw the 7x7 grid.
        for (int y = 0; y < 7; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < 7; x++)
            {
                int index = y * 7 + x;
                GUI.backgroundColor = GetColor(map.tiles[index]);
                if (GUILayout.Button(GetLabel(map.tiles[index]), GUILayout.Width(40), GUILayout.Height(40)))
                {
                    Undo.RecordObject(map, "Paint Tile");
                    map.tiles[index] = selectedTile;
                    EditorUtility.SetDirty(map);
                    AssetDatabase.SaveAssets();
                    Repaint();
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
            default:
                return "?";
        }
    }
}
