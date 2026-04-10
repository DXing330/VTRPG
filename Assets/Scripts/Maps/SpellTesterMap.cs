using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpellTesterMap : MapManager
{
    protected override void Start(){}
    public void ResetAll()
    {
        learnSpellPanel.SetActive(false);
        mapInfo = new List<string>();
        emptyList = new List<string>();
        actorTiles = new List<string>();
        highlightedTiles = new List<string>();
        targetableTiles = new List<int>();
        targetedTiles = new List<int>();
        for (int i = 0; i < mapSize * mapSize; i++)
        {
            mapInfo.Add(defaultTile);
            emptyList.Add("");
            actorTiles.Add("");
            highlightedTiles.Add("");
        }
        UpdateMap();
    }
    public string defaultTile = "Plains";
    public List<string> highlightedTiles;
    public ColorDictionary colorDictionary;
    public void UpdateHighlights(List<int> newTiles, string colorKey = "MoveClose", int layer = 3)
    {
        string colorName = colorDictionary.GetColorNameByKey(colorKey);
        if (emptyList.Count < mapSize * mapSize) { InitializeEmptyList(); }
        highlightedTiles = new List<string>(emptyList);
        for (int i = 0; i < newTiles.Count; i++)
        {
            highlightedTiles[newTiles[i]] = colorName;
        }
        mapDisplayers[layer].HighlightCurrentTiles(mapTiles, highlightedTiles, currentTiles);
    }
    public void ResetHighlights()
    {
        if (emptyList.Count < mapSize * mapSize) { InitializeEmptyList(); }
        highlightedTiles = new List<string>(emptyList);
        mapDisplayers[3].HighlightCurrentTiles(mapTiles, highlightedTiles, currentTiles);
        mapDisplayers[4].HighlightCurrentTiles(mapTiles, highlightedTiles, currentTiles);
    }
    public List<string> actorTiles;
    public string actorSpriteName;
    public void SetActorSpriteName(string newInfo)
    {
        actorSpriteName = newInfo;
        actorTiles[centerTile] = actorSpriteName;
        mapDisplayers[1].DisplayCurrentTiles(mapTiles, actorTiles, currentTiles);
        ResetHighlights();
    }
    public MagicSpell dummySpell;
    public void LoadSpell(string newInfo)
    {
        dummySpell.LoadSkillFromString(newInfo);
        UpdateTargetableTiles();
        spellDescription.text = descriptionViewer.ReturnSpellDescription(dummySpell);
    }
    public List<int> targetableTiles;
    public void UpdateTargetableTiles()
    {
        string shape = dummySpell.GetRangeShape();
        int range = dummySpell.GetRange();
        targetableTiles = mapUtility.GetTilesByShapeSpan(centerTile, shape, range, mapSize);
        UpdateHighlights(targetableTiles);
    }
    public int targetedTile;
    public List<int> targetedTiles;
    public override void ClickOnTile(int tileNumber)
    {
        if (!targetableTiles.Contains(tileNumber)) { return; }
        targetedTile = tileNumber;
        UpdateTargetedTiles();
    }
    public void UpdateTargetedTiles()
    {
        string shape = dummySpell.GetShape();
        int range = dummySpell.GetSpan();
        targetedTiles = mapUtility.GetTilesByShapeSpan(targetedTile, shape, range, mapSize, centerTile);
        UpdateHighlights(targetedTiles, "Attack", 4);
    }
    public ActiveDescriptionViewer descriptionViewer;
    public TMP_Text spellDescription;
    public GameObject learnSpellPanel;
    public void LearnSpellChance(string tomeC = "1", string manaC = "1")
    {
        learnSpellPanel.SetActive(true);
        tomeCost.text = tomeC;
        manaCost.text = manaC;
    }
    public TMP_Text tomeCost;
    public TMP_Text manaCost;
}
