using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessPrepManager : MonoBehaviour
{
    public AutoChessDataManager dataManager;
    void Start()
    {
        // Load From Data Manager.
    }
    // Controls Store (Buy/Sell/Reroll), Unit Placement (Location/Direction/TurnOrder), Etc.
    public List<AutoActorRollUpData> benchSlots;
    public List<AutoActorRollUpData> fieldSlots;
    public int selectedActorLocation;
    public int selectedActorIndex;
    // Move From Map To Bench, Select Actor On Bench, Move From Bench To Bench
    public void ClickOnBenchTile(int index)
    {
        // Select Actor On Bench.
        if (selectedActorLocation < 0)
        {
            selectedActorLocation = 0;
            for (int i = 0; i < benchSlots.Count; i++)
            {
                if (benchSlots[i].location == index)
                {
                    selectedActorIndex = i;
                    break;
                }
            }
        }
        // Move From Bench To Bench.
        else if (selectedActorLocation == 0)
        {

        }
        // Move From Map To Bench.
        else
        {

        }
    }
    // Move From Bench To Map, Select Actor On Map, Move From Map To Map
    public void ClickOnTile(int index)
    {
        // Select Actor On Map.
        if (selectedActorLocation < 0)
        {
            selectedActorLocation = 1;
            for (int i = 0; i < fieldSlots.Count; i++)
            {
                if (fieldSlots[i].location == index)
                {
                    selectedActorIndex = i;
                    break;
                }
            }
        }
        // Move From Bench To Map.
        else if (selectedActorLocation == 0)
        {

        }
        // Move From Map To Map.
        else
        {

        }
    }
}