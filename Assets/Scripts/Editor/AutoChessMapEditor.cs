using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AutoChessMapAsset))]
public class AutoChessMapEditor : Editor
{
    private enum MapEditorLayer
    {
        Terrain,
        TerrainEffects,
        PlayerSpawnZones,
        EnemySpawnZones
    }

    private MapEditorLayer currentLayer = MapEditorLayer.Terrain;

    private StartingMapTile selectedTile = StartingMapTile.Plains;
    private TerrainEffect selectedEffect = TerrainEffect.None;

    private bool selectedSpawnValid = true;

    public override void OnInspectorGUI()
    {
        AutoChessMapAsset map = (AutoChessMapAsset)target;

        DrawDefaultInspector();

        GUILayout.Space(10);
        GUILayout.Label("Map Painter", EditorStyles.boldLabel);

        // Layer selection
        currentLayer = (MapEditorLayer)GUILayout.Toolbar(
            (int)currentLayer,
            new string[]
            {
                "Terrain",
                "Effects",
                "Player Spawn",
                "Enemy Spawn"
            }
        );

        GUILayout.Space(10);

        // Ensure arrays exist
        int size = map.gridSize * map.gridSize;

        if (map.tiles == null || map.tiles.Length != size)
            System.Array.Resize(ref map.tiles, size);

        if (map.terrainEffects == null || map.terrainEffects.Length != size)
            System.Array.Resize(ref map.terrainEffects, size);

        if (map.playerSpawnZones == null || map.playerSpawnZones.Length != size)
            System.Array.Resize(ref map.playerSpawnZones, size);

        if (map.enemySpawnZones == null || map.enemySpawnZones.Length != size)
            System.Array.Resize(ref map.enemySpawnZones, size);

        // Paint selection
        switch (currentLayer)
        {
            case MapEditorLayer.Terrain:
                selectedTile = (StartingMapTile)EditorGUILayout.EnumPopup(
                    "Current Terrain", selectedTile);
                break;

            case MapEditorLayer.TerrainEffects:
                selectedEffect = (TerrainEffect)EditorGUILayout.EnumPopup(
                    "Current Effect", selectedEffect);
                break;

            case MapEditorLayer.PlayerSpawnZones:
            case MapEditorLayer.EnemySpawnZones:
                selectedSpawnValid = EditorGUILayout.Toggle(
                    "Valid Spawn Zone", selectedSpawnValid);

                GUI.backgroundColor = selectedSpawnValid
                    ? new Color(0.35f, 0.85f, 0.35f)
                    : new Color(0.85f, 0.35f, 0.35f);

                GUILayout.Label(
                    selectedSpawnValid ? "Painting: VALID" : "Painting: INVALID",
                    EditorStyles.boldLabel
                );

                GUI.backgroundColor = Color.white;
                break;
        }

        GUILayout.Space(10);

        // Column paint buttons
        EditorGUILayout.BeginHorizontal();

        GUILayout.Space(25);

        for (int x = 0; x < map.gridSize; x++)
        {
            if (GUILayout.Button("↓", GUILayout.Width(40), GUILayout.Height(20)))
            {
                Undo.RecordObject(map, "Paint Column");

                for (int y = 0; y < map.gridSize; y++)
                {
                    int index = y * map.gridSize + x;
                    PaintTile(map, index);
                }

                EditorUtility.SetDirty(map);
            }
        }

        EditorGUILayout.EndHorizontal();

        // Grid
        for (int y = 0; y < map.gridSize; y++)
        {
            EditorGUILayout.BeginHorizontal();

            // Row paint button
            if (GUILayout.Button("→", GUILayout.Width(20), GUILayout.Height(40)))
            {
                Undo.RecordObject(map, "Paint Row");

                for (int x = 0; x < map.gridSize; x++)
                {
                    int index = y * map.gridSize + x;
                    PaintTile(map, index);
                }

                EditorUtility.SetDirty(map);
            }

            // Individual tiles
            for (int x = 0; x < map.gridSize; x++)
            {
                int index = y * map.gridSize + x;

                GUI.backgroundColor = GetTileColor(map, index);

                if (GUILayout.Button(
                    GetTileLabel(map, index),
                    GUILayout.Width(40),
                    GUILayout.Height(40)))
                {
                    Undo.RecordObject(map, "Paint Tile");

                    PaintTile(map, index);

                    EditorUtility.SetDirty(map);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
    }

    private void PaintTile(AutoChessMapAsset map, int index)
    {
        switch (currentLayer)
        {
            case MapEditorLayer.Terrain:
                map.tiles[index] = selectedTile;
                break;

            case MapEditorLayer.TerrainEffects:
                map.terrainEffects[index] = selectedEffect;
                break;

            case MapEditorLayer.PlayerSpawnZones:
                map.playerSpawnZones[index] = selectedSpawnValid;
                break;

            case MapEditorLayer.EnemySpawnZones:
                map.enemySpawnZones[index] = selectedSpawnValid;
                break;
        }
    }

    private Color GetTileColor(AutoChessMapAsset map, int index)
    {
        switch (currentLayer)
        {
            case MapEditorLayer.Terrain:
                return GetColor(map.tiles[index]);

            case MapEditorLayer.TerrainEffects:
                return GetEffectColor(map.terrainEffects[index]);

            case MapEditorLayer.PlayerSpawnZones:
                return GetSpawnColor(map.playerSpawnZones[index]);

            case MapEditorLayer.EnemySpawnZones:
                return GetSpawnColor(map.enemySpawnZones[index]);
        }

        return Color.magenta;
    }

    private string GetTileLabel(AutoChessMapAsset map, int index)
    {
        switch (currentLayer)
        {
            case MapEditorLayer.Terrain:
                return GetLabel(map.tiles[index]);

            case MapEditorLayer.TerrainEffects:
                return GetEffectLabel(map.terrainEffects[index]);

            case MapEditorLayer.PlayerSpawnZones:
            case MapEditorLayer.EnemySpawnZones:
                return map.playerSpawnZones[index] &&
                    currentLayer == MapEditorLayer.PlayerSpawnZones ||
                    map.enemySpawnZones[index] &&
                    currentLayer == MapEditorLayer.EnemySpawnZones
                    ? "1" : "0";
        }

        return "?";
    }

    // Spawn zone colors
    private Color GetSpawnColor(bool valid)
    {
        return valid
            ? new Color(0.20f, 0.65f, 0.25f)
            : new Color(0.35f, 0.20f, 0.20f);
    }

    // Terrain colors
    private Color GetColor(StartingMapTile tile)
    {
        switch (tile)
        {
            case StartingMapTile.Plains: return new Color(0.65f, 0.78f, 0.42f);
            case StartingMapTile.Forest: return new Color(0.20f, 0.43f, 0.25f);
            case StartingMapTile.Water: return new Color(0.30f, 0.58f, 0.82f);
            case StartingMapTile.Mountain: return new Color(0.55f, 0.53f, 0.49f);
            case StartingMapTile.Desert: return new Color(0.85f, 0.72f, 0.43f);
            case StartingMapTile.Snow: return new Color(0.85f, 0.91f, 0.96f);
            case StartingMapTile.DeepWater: return new Color(0.12f, 0.30f, 0.52f);
            case StartingMapTile.Pit: return new Color(0.16f, 0.14f, 0.17f);
            default: return Color.magenta;
        }
    }

    // Terrain labels
    private string GetLabel(StartingMapTile tile)
    {
        switch (tile)
        {
            case StartingMapTile.Plains: return "P";
            case StartingMapTile.Forest: return "F";
            case StartingMapTile.Water: return "W";
            case StartingMapTile.Mountain: return "M";
            case StartingMapTile.Desert: return "D";
            case StartingMapTile.Snow: return "S";
            case StartingMapTile.DeepWater: return "DW";
            case StartingMapTile.Pit: return " ";
            default: return "?";
        }
    }

    // Terrain effect colors
    private Color GetEffectColor(TerrainEffect effect)
    {
        switch (effect)
        {
            case TerrainEffect.None: return new Color(0.25f, 0.25f, 0.25f);
            case TerrainEffect.Thorns: return new Color(0.20f, 0.55f, 0.20f);
            case TerrainEffect.Fire: return new Color(0.95f, 0.25f, 0.05f);
            case TerrainEffect.ToxicMist: return new Color(0.55f, 0.25f, 0.65f);
            case TerrainEffect.Fog: return new Color(0.65f, 0.70f, 0.75f);
            case TerrainEffect.Ice: return new Color(0.35f, 0.80f, 0.95f);
            case TerrainEffect.Oil: return new Color(0.18f, 0.18f, 0.18f);
            case TerrainEffect.OilFire: return new Color(0.65f, 0.12f, 0.02f);
            case TerrainEffect.Water: return new Color(0.15f, 0.45f, 0.85f);
            case TerrainEffect.ElectrifiedWater: return new Color(0.15f, 0.85f, 0.95f);
            default: return Color.magenta;
        }
    }

    // Terrain effect labels
    private string GetEffectLabel(TerrainEffect effect)
    {
        switch (effect)
        {
            case TerrainEffect.None: return "";
            case TerrainEffect.Thorns: return "TH";
            case TerrainEffect.Fire: return "FI";
            case TerrainEffect.ToxicMist: return "TM";
            case TerrainEffect.Fog: return "FO";
            case TerrainEffect.Ice: return "IC";
            case TerrainEffect.Oil: return "OI";
            case TerrainEffect.OilFire: return "OF";
            case TerrainEffect.Water: return "WA";
            case TerrainEffect.ElectrifiedWater: return "EW";
            default: return "?";
        }
    }
}
