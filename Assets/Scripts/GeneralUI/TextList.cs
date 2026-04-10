using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextList : MonoBehaviour
{
    public GeneralUtility utility;
    public List<string> allText;
    public void ResetTextList()
    {
        allText.Clear();
        page = 0;
        ResetPage();
    }
    public void SetTextList(List<string> newText)
    {
        allText = new List<string>(newText);
        page = 0;
        UpdatePage();
    }
    public List<TMP_Text> textList;
    protected void ResetPage()
    {
        for (int i = 0; i < textList.Count; i++)
        {
            textList[i].text = "";
        }
    }
    public List<GameObject> textObjects;
    public int page = 0;
    public void ChangePage(bool right = true)
    {
        page = utility.ChangePage(page, right, textObjects, allText);
        UpdatePage();
    }
    public List<GameObject> changePageObjects;
    public TMP_Text pageDisplay;
    public int MaxPages()
    {
        if (allText.Count < textObjects.Count){return 0;}
        return ((allText.Count - 1) / textObjects.Count);
    }
    public void UpdatePageDisplay()
    {
        if (pageDisplay == null){return;}
        pageDisplay.text = (page + 1)+"/"+(MaxPages()+1);
    }
    public void UpdatePage()
    {
        if (allText.Count <= 0)
        {
            ResetPage();
            return;
        }
        UpdateCurrentPage(utility.GetCurrentPageStrings(page, textObjects, allText));
        UpdatePageDisplay();
    }
    public void UpdateCurrentPage(List<string> cPageTexts)
    {
        ResetPage();
        for (int i = 0; i < cPageTexts.Count; i++)
        {
            textList[i].text = cPageTexts[i];
        }
    }
}
