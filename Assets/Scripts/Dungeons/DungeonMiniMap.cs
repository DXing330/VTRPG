using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DungeonMiniMap : MonoBehaviour
{
    public Dungeon dungeon;
    public bool active = false;
    public bool copy = false;
    public int mapDiameter = 32;
    List<int> viewedTiles;
    public void UpdateViewedTiles()
    {
        viewedTiles = mainMap.GetCurrentTiles(mapDiameter);
    }
    public DungeonMap mainMap;
    public DungeonMiniMap copiedMap;
    public string miniMapText;
    public GameObject miniMapObject;
    public TMP_Text text;

    public void ToggleState()
    {
        active = !active;
        UpdateState();
    }

    protected void UpdateState()
    {
        if (active)
        {
            miniMapObject.SetActive(true);
            if (copy)
            {
                miniMapText = copiedMap.miniMapText;
            }
            else
            {
                UpdateMiniMapString();
            }
            UpdateMiniMap();
        }
        else{miniMapObject.SetActive(false);}
    }

    public void UpdateMiniMap()
    {
        text.text = miniMapText;
    }

    public void UpdateMiniMapString(List<int> currentTiles = null)
    {
        UpdateViewedTiles();
        miniMapText = "";
        int mapCount = 0;
        for (int j = 0; j < viewedTiles.Count; j++)
        {
            int i = viewedTiles[j];
            // If it's out of bounds.
            if (i < 0){miniMapText += "?";}
            // If you haven't explored yet.
            else if (dungeon.viewedTiles[i] == 0){miniMapText += "?";}
            else
            {
                // (YOU)
                if (i == dungeon.partyLocation){miniMapText += "<color=blue>P</color>";}
                // Viewable enemy.
                else if (dungeon.EnemyLocation(i) && currentTiles != null && currentTiles.Contains(i)){miniMapText += "<color=red>E</color>";}
                else if (dungeon.StairsDownLocation(i)){miniMapText += "<color=green>S</color>";}
                else if (dungeon.GoalTile(i)){miniMapText += "<color=green>Q</color>";}
                else if (dungeon.TreasureLocation(i)){miniMapText += "<color=green>T</color>";}
                else if (dungeon.ItemLocation(i)){miniMapText += "<color=green>I</color>";}
                else if (dungeon.GetMerchantLocation() == i){miniMapText += "<color=green>M</color>";}
                else if (dungeon.TilePassable(i)){miniMapText += "_";}
                else{miniMapText += "X";}
            }
            mapCount++;
            if (mapCount >= mapDiameter)
            {
                miniMapText += "\n";
                mapCount = 0;
            }
        }
    }
}
