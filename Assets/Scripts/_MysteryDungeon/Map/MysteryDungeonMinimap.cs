using UnityEngine;
using UnityEngine.UI;

public class MysteryDungeonMinimap : MaskableGraphic
{
    public MysteryDungeonBattleMap map;
    [ContextMenu("Create Minimap")]
    public void CreateMinimap()
    {
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (map == null || map.mapInfo == null || map.mapInfo.Count == 0)
        {
            return;
        }
        int mapSize = Mathf.RoundToInt(Mathf.Sqrt(map.mapInfo.Count));
        // Maximum display size of the minimap.
        int displaySize = 96;
        float panelWidth = rectTransform.rect.width;
        float panelHeight = rectTransform.rect.height;
        // Pointy-top hex dimensions.
        float hexWidth = Mathf.Sqrt(3f);
        float hexHeight = 2f;
        float horizontalSpacing = hexWidth;
        float verticalSpacing = hexHeight * 0.75f;
        // Calculate the size of the FULL 128x128 display.
        float displayWidth = (displaySize - 1) * horizontalSpacing + hexWidth * 1.5f;
        float displayHeight = (displaySize - 1) * verticalSpacing + hexHeight;
        float scaleX = panelWidth / displayWidth;
        float scaleY = panelHeight / displayHeight;
        float scale = Mathf.Min(scaleX, scaleY);
        hexWidth *= scale;
        hexHeight *= scale;
        horizontalSpacing *= scale;
        verticalSpacing *= scale;
        // Calculate the size of the full display again after scaling.
        displayWidth =
            (displaySize - 1) * horizontalSpacing +
            hexWidth * 1.5f;
        displayHeight =
            (displaySize - 1) * verticalSpacing +
            hexHeight;
        // Center the 128x128 display in the panel.
        float offsetX = -displayWidth * 0.5f;
        float offsetY = -displayHeight * 0.5f;
        // Center the actual map inside the 128x128 display.
        int tileOffset = (displaySize - mapSize) / 2;
        // Draw the actual map.
        for (int tile = 0; tile < map.mapInfo.Count; tile++)
        {
            int row = tile / mapSize;
            int col = tile % mapSize;
            // Move the actual map into the center.
            int displayRow = row + tileOffset;
            int displayCol = col + tileOffset;
            float x = displayCol * horizontalSpacing;
            // Pointy-top odd-row offset.
            if ((row & 1) == 1)
            {
                x += hexWidth * 0.5f;
            }
            float y = (displaySize - 1 - displayRow) * verticalSpacing;
            Vector2 center = new Vector2(
                x + offsetX + hexWidth * 0.5f,
                y + offsetY + hexHeight * 0.5f
            );
            AddHex(vh, center, scale, GetTileColor(tile));
        }
    }
    private void AddHex(VertexHelper vh, Vector2 center, float size,Color color)
    {
        int startIndex = vh.currentVertCount;
        // Center vertex.
        vh.AddVert(center, color, Vector2.zero);
        // Six corners.
        for (int i = 0; i < 6; i++)
        {
            float angle = (60f * i + 30f) * Mathf.Deg2Rad;
            Vector2 corner = center + new Vector2(
                Mathf.Cos(angle) * size,
                Mathf.Sin(angle) * size
            );
            vh.AddVert(corner, color, Vector2.zero);
        }
        // Six triangles making the hex.
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            vh.AddTriangle(startIndex, startIndex + i + 1, startIndex + next + 1);
        }
    }
    // TODO Add Enemies/Items Traps
    private Color GetTileColor(int tile)
    {
        // Player always visible.
        if (tile == map.playerLocation)
            return Color.green;
        if (map.AllyOnTile(tile))
            return new Color(0.56f, 0.93f, 0.56f, 1f);
        // Unexplored.
        if (map.tileExplored == null || tile < 0 || tile >= map.tileExplored.Count || map.tileExplored[tile] == 0 && !map.revealLayout)
        {
            return Color.clear;
        }
        // Temporary terrain check.
        if (!IsPassable(map.mapInfo[tile]))
            return Color.clear;
        bool visible = map.visibleTiles.Contains(tile);
        bool stairs = map.stairsLocation == tile;
        bool enemy = map.EnemyOnTile(tile);
        bool item = map.itemLocations.Contains(tile);
        bool trap = map.trapLocations.Contains(tile);
        if (enemy && (visible || map.revealEnemies))
        {
            return Color.red;
        }
        if (stairs && (visible || map.revealStairs))
        {
            return Color.grey;
        }
        if (item && (visible || map.revealItems))
            return Color.blue;
        if ((trap && map.revealTraps) || trap && map.TrapTriggered(tile))
            return new Color(0.5450981f, 0f, 0f, 1f);
        if (visible)
            return Color.white;
        return new Color(1f, 1f, 1f, 0.35f);
    }
    private bool IsPassable(string terrain)
    {
        switch (terrain)
        {
            case "Plains":
            case "Forest":
            case "Mountain":
            case "Water":
            case "Desert":
            case "Volcano":
            case "Snow":
            case "SnowForest":
            case "SnowMountain":
            case "Oasis":
                return true;
            case "Pit":
            case "Wall":
            case "DeepWater":
                return false;
        }
        return false;
    }
    public void UpdateMinimap()
    {
        SetVerticesDirty();
    }
}