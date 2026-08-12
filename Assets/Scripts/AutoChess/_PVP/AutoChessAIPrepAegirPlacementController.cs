using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessAIPrepAegirPlacementController : MonoBehaviour
{
    public AutoChessPrepManager prepManager;
    public MapUtility mapUtility;
    protected List<int> pitTiles = new List<int>();
    void GetPitTiles()
    {
        pitTiles.Clear();
        for (int i = 0; i < prepManager.dataManager.mapTiles.Count; i++)
        {
            if (prepManager.dataManager.mapTiles[i] == "Pit")
            {
                pitTiles.Add(i);
            }
        }
    }
    public int FindOpenSpot(int startColumn = 2, int direction = -1)
    {
        if (startColumn < 0 || startColumn >= 9)
        {
            return -1;
        }
        List<int> spots = mapUtility.GetTilesInColumn(startColumn, 9);
        // Remove Any Pits/Occupied Spaces.
        List<int> occupiedSpots = prepManager.GetTakenSpots();
        GetPitTiles();
        for (int i = spots.Count - 1; i >= 0; i--)
        {
            if (pitTiles.Contains(spots[i]) || occupiedSpots.Contains(spots[i]))
            {
                spots.RemoveAt(i);
            }
        }
        // Get The First Open Middle-Most Tile.
        int row = 4;
        for (int i = 0; i < 9; i++)
        {
            int tile = mapUtility.ReturnTileNumberFromRowCol(row, startColumn, 9);
            if (spots.Contains(tile)){return tile;}
            if (i % 2 == 0)
            {
                row += ((i + 1));
            }
            else
            {
                row -= ((i + 1));
            }
        }
        // Move To Next Column.
        return FindOpenSpot(startColumn + direction, direction);
    }
    public int FindBestTileForRole(AutoActorRollUpData actor)
    {
        if (actor.GetAttackRange() <= 1)
        {
            return FindOpenSpot(2, - 1);
        }
        else if (actor.healer)
        {
            return FindOpenSpot(0, 1);
        }
        else
        {
            return FindOpenSpot(1, 1);
        }
    }
    float GetAegirCarryScore(AutoActorRollUpData unit)
    {
        if (unit == null)
        {
            return float.MinValue;
        }
        float score = 0f;
        // Attack is the main thing we care about for now.
        score += unit.GetAttack() * unit.GetAttackRange();
        if (unit.AOE)
        {
            score *= 2;
        }
        return score;
    }
    public void AegirPlaceFieldUnits()
    {
        if (!prepManager.dataManager.factionData.FactionActive("Aegir")){return;}
        // Form a Chain Of Aegir Units, Ending With A Carry And Starting With Highest Attack NonAegir If Possible.
        // Find The Base Of The Chain And The End.
        // Make Sure The Directions Line Up.
        List<AutoActorRollUpData> aegirUnits = new();
        List<AutoActorRollUpData> nonAegirUnits = new();
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            AutoActorRollUpData unit = prepManager.fieldSlots[i];
            if (unit == null)
            {
                continue;
            }
            // Reset The Locations.
            unit.SetLocation(-1);
            if (unit.FactionExists("Aegir") || unit.FactionExists("Harmony") || unit.EmblemExists("Aegir"))
            {
                aegirUnits.Add(unit);
            }
            else
            {
                nonAegirUnits.Add(unit);
            }
        }
        int firstEatingLocation = -1;
        int chainEatingLocation = -1;
        aegirUnits.Sort((a, b) => {return GetAegirCarryScore(a).CompareTo(GetAegirCarryScore(b));});
        if (nonAegirUnits.Count > 0)
        {
            nonAegirUnits.Sort((a, b) => {return a.GetAttack().CompareTo(b.GetAttack());});
            nonAegirUnits[nonAegirUnits.Count - 1].SetLocation(FindOpenSpot(2, -1));
            firstEatingLocation = nonAegirUnits[nonAegirUnits.Count - 1].GetLocation();
            nonAegirUnits.RemoveAt(nonAegirUnits.Count - 1);
        }
        // Place An Aegir To Eat, Since No Non-Aegir Were Found.
        if (firstEatingLocation < 0)
        {
            aegirUnits[0].SetLocation(FindOpenSpot(2, -1));
            firstEatingLocation = aegirUnits[0].GetLocation();
            aegirUnits.RemoveAt(0);
        }
        if (firstEatingLocation < 0)
        {
            Debug.Log("First Eating Location Is Out Of Bounds");
        }
        // The The First Aegir To The Left (Direction 4), Which Will Be Column 1.
        aegirUnits[0].SetLocation(mapUtility.PointInDirection(firstEatingLocation, 4, 9));
        aegirUnits[0].SetDirection(1);
        chainEatingLocation = aegirUnits[0].GetLocation();
        if (chainEatingLocation < 0)
        {
            Debug.Log("Chain Eating Location Is Out Of Bounds");
        }
        List<int> adjacentTiles = mapUtility.AdjacentTiles(chainEatingLocation, 9);
        adjacentTiles.Remove(firstEatingLocation);
        aegirUnits.RemoveAt(0);
        for (int i = 0; i < adjacentTiles.Count; i++)
        {
            if (i >= aegirUnits.Count){break;}
            aegirUnits[i].SetLocation(adjacentTiles[i]);
            aegirUnits[i].SetDirection(mapUtility.DirectionBetweenLocations(adjacentTiles[i], chainEatingLocation, 9));
        }
        if (adjacentTiles.Count >= aegirUnits.Count)
        {
            // Assign the remaining non-aegir.
            for (int i = 0; i < nonAegirUnits.Count; i++)
            {
                int bestTile = FindBestTileForRole(nonAegirUnits[i]);
                nonAegirUnits[i].SetLocation(bestTile);
            }
            return;
        }
        else if (adjacentTiles.Count < aegirUnits.Count)
        {
            chainEatingLocation = aegirUnits[0].GetLocation();
            // Remove The Ones That Have Already Eaten.
            for (int i = adjacentTiles.Count - 1; i >= 0; i--)
            {
                aegirUnits.RemoveAt(i);
            }
        }
        // At Most 3 Additional Units, Since 7 Spots Taken, Hard Code It.
        // This Implies You've Reach Level 10, Have 10 Aegir Units, 9 Base + 1 Emblem, And 2 HR File Items Somehow, Should Autowin TBH.
        for (int i = 0; i < aegirUnits.Count; i++)
        {
            int newLocation = mapUtility.PointInDirection(chainEatingLocation, 5, 9);
            aegirUnits[i].SetLocation(newLocation);
            aegirUnits[i].SetDirection(2);
            chainEatingLocation = newLocation;
        }
        for (int i = 0; i < nonAegirUnits.Count; i++)
        {
            int bestTile = FindBestTileForRole(nonAegirUnits[i]);
            nonAegirUnits[i].SetLocation(bestTile);
        }
    }
    // Hard Code With Map Size 9 To Ensure No Units With Location Of -1
    public void AegirSafetyCheck()
    {
        if (!prepManager.dataManager.factionData.FactionActive("Aegir")){return;}
        List<int> occupiedSpots = prepManager.GetTakenSpots();
        int safeSpot = 0;
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            AutoActorRollUpData unit = prepManager.fieldSlots[i];
            if (unit.GetLocation() >= 0){continue;}
            Debug.LogError($"AEGIR INVALID PLACEMENT | " + $"Unit: {unit.GetName()}");
            // Move Them To A Safe Spot.
            for (int j = 0; j < 27; j++)
            {
                if (occupiedSpots.Contains(safeSpot))
                {
                    safeSpot += 9;
                    if (safeSpot > 80)
                    {
                        safeSpot -= 81;
                        safeSpot++;
                    }
                }
                else
                {
                    occupiedSpots.Add(safeSpot);
                    unit.SetLocation(safeSpot);
                    break;
                }
            }
        }
    }
}
