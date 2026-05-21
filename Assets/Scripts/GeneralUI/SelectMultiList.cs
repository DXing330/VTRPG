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
    public override void ResetSelected()
    {
        selectedIndices.Clear();
        ResetHighlights();
    }
    public override void Select(int index)
    {
        // Selecting and already selected unselects it.
        int newIndex = (currentPage * textObjects.Count) + index;
        if (selectedIndices.Contains(newIndex))
        {
            selectedIndices.Remove(newIndex);
            HighlightSelected();
            return;
        }
        if (selectedIndices.Count >= maxSelections){ return; }
        selectedIndices.Add(newIndex);
        HighlightSelected();
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
