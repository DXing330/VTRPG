using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessBattleMap : BattleMap
{
    public override void UpdateMap()
    {
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
        // Reset All Text.
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].ResetLayerSprite(2);
            mapTiles[i].ResetDirectionArrows();
        }
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetInvisible()){continue;}
            int location = battlingActors[i].GetLocation();
            mapTiles[location].ActivateLayerSprite(2);
            masterSprites.ApplyToImage(mapTiles[location].GetLayerSprite(2), battlingActors[i].GetSpriteName());
            mapTiles[location].ActivateDirectionArrow(battlingActors[i].GetDirection());
        }
    }
}
