using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillRangeViewer : MaskableGraphic
{
    private const int PREVIEW_SIZE = 7;
    [Header("Colors")]
    [SerializeField] private Color actorColor = Color.green;
    [SerializeField] private Color rangeColor = Color.blue;
    [SerializeField] private Color effectColor = Color.red;
    [SerializeField] private Color emptyColor = Color.gray;
    public MapUtility mapUtility;
    public ActiveSkill activeSkill;
    public StatDatabase activeData;
    public int range;
    public string rangeShape;
    public int span;
    public string targetShape;
    public void DrawActiveSkill(string skillName, TacticActor skillActor = null)
    {
        activeSkill.LoadSkillFromString(activeData.ReturnValue(skillName), skillActor);
        range = activeSkill.GetRange(skillActor);
        rangeShape = activeSkill.GetRangeShape();
        span = activeSkill.GetSpan(skillActor);
        targetShape = activeSkill.GetShape();
        SetVerticesDirty();
    }
    public void DrawActiveSkill(ActiveSkill skill, TacticActor skillActor = null)
    {
        range = activeSkill.GetRange(skillActor);
        rangeShape = activeSkill.GetRangeShape();
        span = activeSkill.GetSpan(skillActor);
        targetShape = activeSkill.GetShape();
        SetVerticesDirty();
    }
    public void ClearSkill()
    {
        range = 0;
        rangeShape = "";
        span = 0;
        targetShape = "";
        SetVerticesDirty();
    }
    [ContextMenu("Create Skill Graphic")]
    public void CreateSkillGraphic()
    {
        SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (activeSkill == null)
            return;
        DrawSkill(vh);
    }
    private void DrawSkill(VertexHelper vh)
    {
        int size = PREVIEW_SIZE;
        if (mapUtility == null)
            return;
        int actorRow = size - 2;
        int actorCol = 1;
        int actorTile = actorRow * size + actorCol;
        int targetTile = range == 0 ? actorTile : mapUtility.PointInDirection(actorTile, 0, size);
        List<int> rangeTiles = mapUtility.GetTilesByShapeSpan(actorTile, rangeShape, range, size, actorTile);
        List<int> effectTiles = mapUtility.GetTilesByShapeSpan(targetTile, targetShape, span, size, actorTile);
        DrawTiles(vh, actorTile, rangeTiles, effectTiles, size);
    }
    private void DrawTiles(VertexHelper vh, int actorTile, List<int> rangeTiles, List<int> effectTiles, int size)
    {
        float panelWidth = rectTransform.rect.width;
        float panelHeight = rectTransform.rect.height;
        float hexWidth = Mathf.Sqrt(3f);
        float hexHeight = 2f;
        float horizontalSpacing = hexWidth;
        float verticalSpacing = hexHeight * 0.75f;
        float displayWidth = (size - 1) * horizontalSpacing + hexWidth * 1.5f;
        float displayHeight = (size - 1) * verticalSpacing + hexHeight;
        float scaleX = panelWidth / displayWidth;
        float scaleY = panelHeight / displayHeight;
        float scale = Mathf.Min(scaleX, scaleY);
        hexWidth *= scale;
        hexHeight *= scale;
        horizontalSpacing *= scale;
        verticalSpacing *= scale;
        displayWidth = (size - 1) * horizontalSpacing + hexWidth * 1.5f;
        displayHeight = (size - 1) * verticalSpacing + hexHeight;
        float offsetX = -displayWidth * 0.5f;
        float offsetY = -displayHeight * 0.5f;

        for (int tile = 0; tile < size * size; tile++)
        {
            Color color = emptyColor;
            if (tile == actorTile)
                color = actorColor;
            else if (effectTiles.Contains(tile))
                color = effectColor;
            else if (rangeTiles.Contains(tile))
                color = rangeColor;
            if (color.a <= 0f)
                continue;
            int row = tile / size;
            int col = tile % size;
            float x = col * horizontalSpacing;
            if ((row & 1) == 1)
                x += hexWidth * 0.5f;
            float y = (size - 1 - row) * verticalSpacing;
            Vector2 center = new Vector2(x + offsetX + hexWidth * 0.5f, y + offsetY + hexHeight * 0.5f);
            AddHex(vh, center, scale, color);
        }
    }
    private void AddHex(VertexHelper vh, Vector2 center, float size, Color color)
    {
        int startIndex = vh.currentVertCount;
        vh.AddVert(center, color, Vector2.zero);
        for (int i = 0; i < 6; i++)
        {
            float angle = (60f * i + 30f) * Mathf.Deg2Rad;
            Vector2 corner = center + new Vector2(Mathf.Cos(angle) * size, Mathf.Sin(angle) * size);
            vh.AddVert(corner, color, Vector2.zero);
        }
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;
            vh.AddTriangle(startIndex, startIndex + i + 1, startIndex + next + 1);
        }
    }
    public void UpdateSkillGraphic()
    {
        SetVerticesDirty();
    }
}