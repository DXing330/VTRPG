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
    protected void UpdateAutoChessActors()
    {
        // Reset All Text.
        for (int i = 0; i < mapTiles.Count; i++)
        {
            mapTiles[i].UpdateText();
        }
        for (int i = 0; i < battlingActors.Count; i++)
        {
            if (battlingActors[i].GetInvisible()){continue;}
            mapTiles[battlingActors[i].GetLocation()].UpdateText(battlingActors[i].GetPersonalName() + "\n" + battlingActors[i].GetHealth() + "/" + battlingActors[i].GetBaseHealth());
        }
    }
}
