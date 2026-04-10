using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleMapManager : MonoBehaviour
{
    public List<MapTile> mapTiles;
    public List<int> currentTiles;
    public List<string> mapInfo;

    public virtual void ClickOnTile(int tileNumber)
    {
        Debug.Log(tileNumber);
    }
}
