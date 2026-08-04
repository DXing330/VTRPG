using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessBattleMap : BattleMap
{
    public bool fast = false;
    public override void UpdateMap()
    {
        if (fast){return;}
        BaseUpdateMap();
        GetActorTiles();
        ResetHighlights();
        UpdateBuildings();
        UpdateTerrain();
        UpdateAutoChessActors();
    }
    // Update Actors With Text, Not Images For Now.
    // Also Show The Castle Health?
    public SpriteContainer masterSprites;
    protected void UpdateAutoChessActors()
    {
        // Reset AutoChess Stuff.
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].ResetLayerSprite(2);
            mapTiles[i].UpdateText();
            mapTiles[i].ResetDirectionArrows();
            mapTiles[i].ResetHealthBar();
            mapTiles[i].ResetAutoChessEquipment();
        }
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetInvisible()){continue;}
            int location = battlingActors[i].GetLocation();
            mapTiles[location].ActivateLayerSprite(2);
            masterSprites.ApplyToImage(mapTiles[location].GetLayerSprite(2), battlingActors[i].GetSpriteName());
            mapTiles[location].ActivateDirectionArrow(battlingActors[i].GetDirection());
            mapTiles[location].UpdateHealthBar(battlingActors[i].GetHealth(), battlingActors[i].GetBaseHealth());
            List<string> equipNames = battlingActors[i].GetAutoChessEquipmentNames();
            for (int j = 0; j < equipNames.Count; j++)
            {
                if (equipNames[j].Length <= 0){continue;}
                mapTiles[location].EnableEquipSlot(j);
                masterSprites.ApplyToImage(mapTiles[location].GetEquipSlotImage(j), equipNames[j]);
            }
        }
        for (int i = 0; i < buildingLocations.Count; i++)
        {
            mapTiles[buildingLocations[i]].UpdateText(buildingHealths[i].ToString());
        }
    }
}
