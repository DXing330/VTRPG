using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectImageList : MonoBehaviour
{
    public GeneralUtility utility;
    public SpriteContainer allSprites;
    public List<string> selectable;
    public void ResetSelectables()
    {
        selectable.Clear();
    }
    public int selectedIndex = -1;
    public virtual void ResetSelected()
    {
        selectedIndex = -1;
        selected = "";
    }
    public int GetSelected(){return selectedIndex;}
    public string selected;
    public string GetSelectedString(){return selected;}
    public void SetSelectedString(string prevSelected)
    {
        int index = selectable.IndexOf(prevSelected);
        if (index < 0)
        {
            ResetSelected();
            return;
        }
        selectedIndex = index;
        selected = prevSelected;
    }
}
