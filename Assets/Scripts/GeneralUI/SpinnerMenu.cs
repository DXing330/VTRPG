using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpinnerMenu : MonoBehaviour
{
    void Start()
    {
        if (selectableKeys != null)
        {
            List<string> allKeys = selectableKeys.GetAllKeys();
            if (includeNone)
            {
                allKeys.Insert(0, "None");
            }
            SetSelectables(allKeys);
        }
        else
        {
            ResetSelectedIndex();
        }
    }
    public GeneralUtility utility;
    public StatDatabase selectableKeys;
    public bool includeNone = true;
    public List<string> selectables;
    public void SetSelectables(List<string> newInfo)
    {
        selectables = new List<string>(newInfo);
        ResetSelectedIndex();
    }
    public TMP_Text selectedText;
    public void SetSelectedText(string newText)
    {
        // Set the index based on the text.
        int indexOf = selectables.IndexOf(newText);
        if (indexOf < 0)
        {
            ResetSelectedIndex();
            return;
        }
        SetSelectedIndex(indexOf);
    }
    public string selected;
    public string GetSelected(){return selected;}
    public int index;
    public void ResetSelectedIndex()
    {
        SetSelectedIndex(0);
    }
    public int GetSelectedIndex(){return index;}
    public void SetSelectedIndex(int newInfo)
    {
        index = newInfo;
        selected = selectables[index];
        selectedText.text = selected;
    }
    public void ChangeIndex(bool right = true)
    {
        int newIndex = utility.ChangeIndex(index, right, selectables.Count - 1);
        SetSelectedIndex(newIndex);
    }
}
