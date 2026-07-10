using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ImageSelectList : MonoBehaviour
{
    public GeneralUtility utility;
    public ColorDictionary colors;
    public SpriteContainer masterSprites;
    public List<GameObject> changePageObjects;
    public List<GameObject> imageBGObjects;
    public List<GameObject> imageObjects;
    public List<Image> imageList;
    public List<string> selectable;
    public void ResetSelectables()
    {
        selectable.Clear();
        StartingPage();
    }
    public void SetSelectables(List<string> newList)
    {
        selectable = new List<string>(newList);
        StartingPage();
        if (newList.Count > imageObjects.Count)
        {
            utility.EnableGameObjects(changePageObjects);
        }
        else { utility.DisableGameObjects(changePageObjects); }
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
    public int currentPage;
    public int GetPage(){return currentPage;}
    public void SetPage(int newInfo)
    {
        currentPage = newInfo;
        // In case the new max page is less than the previous.
        if (currentPage > MaxPages())
        {
            currentPage = MaxPages();
        }
        UpdateCurrentPage(utility.GetCurrentPageStrings(currentPage, imageObjects, selectable));
    }
    public TMP_Text pageDisplay;
    public void UpdatePageDisplay()
    {
        if (pageDisplay == null){return;}
        pageDisplay.text = (currentPage + 1) + "/" + (MaxPages() + 1);
    }
    public int MaxPages()
    {
        if (selectable.Count < imageObjects.Count){return 0;}
        return ((selectable.Count - 1) / imageObjects.Count);
    }
    [ContextMenu("Right")]
    public void ChangeRight(){ChangePage();}
    [ContextMenu("Left")]
    public void ChangeLeft(){ChangePage(false);}
    public void ChangePage(bool right = true)
    {
        ResetSelected();
        currentPage = utility.ChangePage(currentPage, right, imageObjects, selectable);
        UpdateCurrentPage(utility.GetCurrentPageStrings(currentPage, imageObjects, selectable));
    }
    protected virtual void ResetPage()
    {
        for (int i = 0; i < imageObjects.Count; i++)
        {
            imageBGObjects[i].SetActive(false);
            imageObjects[i].SetActive(false);
        }
    }
    public void StartingPage()
    {
        currentPage = 0;
        ResetSelected();
        UpdateCurrentPage(utility.GetCurrentPageStrings(currentPage, imageObjects, selectable));
    }
    protected void UpdateCurrentPage(List<string> newPageStrings)
    {
        ResetPage();
        UpdatePageDisplay();
        for (int i = 0; i < newPageStrings.Count; i++)
        {
            imageBGObjects[i].SetActive(true);
            imageObjects[i].SetActive(true);
            masterSprites.ApplyToImage(imageList[i], newPageStrings[i]);
        }
    }
    public virtual void Select(int index)
    {
        selectedIndex = (currentPage * imageObjects.Count) + index;
        selected = selectable[currentPage * imageObjects.Count + index];
    }
}
