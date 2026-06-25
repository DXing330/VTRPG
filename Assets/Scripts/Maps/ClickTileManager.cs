using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickTileManager : MonoBehaviour
{
    public virtual void ClickOnTile(int tileNumber)
    {
        Debug.Log(tileNumber);
    }
    public virtual void ClickDirection(int tileNumber, int direction)
    {
        Debug.Log(tileNumber + "-" + direction);
    }
}
