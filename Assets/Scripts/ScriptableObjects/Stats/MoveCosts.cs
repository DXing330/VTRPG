using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveCosts", menuName = "ScriptableObjects/DataContainers/MoveCosts", order = 1)]
public class MoveCosts : ScriptableObject
{
    public string moveType;
    public List<string> tileTypes;
    public List<int> moveCosts;

    public int ReturnMoveCost(string tileType)
    {
        int indexOf = tileTypes.IndexOf(tileType);
        if (indexOf < 0){return 1;}
        return moveCosts[indexOf];
    }
}
