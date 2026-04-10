using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectMultiList : SelectList
{
    public int maxSelections;
    public List<int> selectedIndices;
    public override void ResetSelected()
    {
        selectedIndices.Clear();
        ResetHighlights();
    }
    public override void Select(int index)
    {
        if (selectedIndices.Count >= maxSelections){ return; }
        selectedIndices.Add((currentPage * textObjects.Count) + index);
        HighlightSelected();
    }
    public override void HighlightSelected(string color = "Highlight")
    {
        ResetHighlights();
        for (int i = 0; i < selectedIndices.Count; i++)
        {
            textList[selectedIndices[i]].color = colors.GetColor(color);
        }
    }
}
