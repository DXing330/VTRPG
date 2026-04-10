using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpinnerMenu : MonoBehaviour
{
    public GeneralUtility utility;
    public List<string> selectables;
    public void SetSelectables(List<string> newInfo)
    {
        selectables = new List<string>(newInfo);
        ResetSelectedIndex();
    }
    public TMP_Text selectedText;
    public string selected;
    public string GetSelected(){return selected;}
    public int index;
    public void ResetSelectedIndex()
    {
        SetSelectedIndex(0);
    }
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
