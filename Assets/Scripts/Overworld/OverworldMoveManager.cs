using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OverworldMoveManager", menuName = "ScriptableObjects/Overworld/OverworldMoveManager", order = 1)]
public class OverworldMoveManager : ScriptableObject
{
    public List<string> tileTypes;
    public List<int> moveCostMultipliers;
    public int baseMoveCost = 12;
    public int GetBaseMoveCost(){return baseMoveCost;}

    public int ReturnMoveCost(string tileName)
    {
        int indexOf = tileTypes.IndexOf(tileName);
        if (indexOf == -1){return baseMoveCost;}
        return moveCostMultipliers[indexOf]*baseMoveCost;
    }

    public int ReturnMoveCostByIndex(int index)
    {
        if (index < 0 || index >= moveCostMultipliers.Count){return baseMoveCost;}
        return moveCostMultipliers[index]*baseMoveCost;
    }
}
