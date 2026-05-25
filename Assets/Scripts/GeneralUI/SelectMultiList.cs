using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectMultiList : SelectList
{
    public int maxSelections;
    public void SetMaxSelections(int newInfo = 1)
    {
        maxSelections = Mathf.Max(1, newInfo);
    }
    public List<int> selectedIndices;
    public List<int> GetAllSelected()
    {
        return selectedIndices;
    }
    public override void ResetSelected()
    {
        selectedIndices.Clear();
        ResetHighlights();
    }
    public int GetRecentlySelected()
    {
        if (selectedIndices.Count <= 0){return -1;}
        return selectedIndices[selectedIndices.Count - 1];
    }
    // True if just selected, false if just deselected.
    public bool selectedRecently = false;
    public bool SelectedRecently(){return selectedRecently;}
    public override void Select(int index)
    {
        // Selecting and already selected unselects it.
        int newIndex = (currentPage * textObjects.Count) + index;
        if (selectedIndices.Contains(newIndex))
        {
            selectedIndices.Remove(newIndex);
            HighlightSelected();
            selectedRecently = false;
            return;
        }
        if (selectedIndices.Count >= maxSelections)
        {
            return;
        }
        selectedIndices.Add(newIndex);
        HighlightSelected();
        selectedRecently = true;
    }
    // TODO Update This To Work On Multiple Pages.
    public override void HighlightSelected(string color = "Highlight")
    {
        ResetHighlights();
        for (int i = 0; i < selectedIndices.Count; i++)
        {
            textList[selectedIndices[i]].color = colors.GetColor(color);
        }
    }
}
