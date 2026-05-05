using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ToolTipPopUp : MonoBehaviour
{
    public GameObject tooltipObject;
    public RectTransform tooltipRect;
    public RectTransform canvasRect;
    public TMP_Text tooltipText;
    public Vector2 offset;
    public void ShowTooltip(string text, RectTransform anchor)
    {
        tooltipText.text = text;
        tooltipObject.SetActive(true);
        tooltipRect.position = anchor.position + (Vector3)offset;
        ClampToCanvas(tooltipRect, canvasRect);
    }
    public void HideTooltip()
    {
        tooltipText.text = "";
        tooltipObject.SetActive(false);
    }
    public void ToggleTooltip(string text, RectTransform anchor)
    {

    }
    public void ClampToCanvas(RectTransform tooltip, RectTransform canvas)
    {
        Vector3[] corners = new Vector3[4];
        tooltip.GetWorldCorners(corners);
        Vector3[] canvasCorners = new Vector3[4];
        canvas.GetWorldCorners(canvasCorners);
        Vector3 offset = Vector3.zero;
        // Right
        if (corners[2].x > canvasCorners[2].x)
            offset.x -= corners[2].x - canvasCorners[2].x;
        // Left
        if (corners[0].x < canvasCorners[0].x)
            offset.x += canvasCorners[0].x - corners[0].x;
        // Top
        if (corners[2].y > canvasCorners[2].y)
            offset.y -= corners[2].y - canvasCorners[2].y;
        // Bottom
        if (corners[0].y < canvasCorners[0].y)
            offset.y += canvasCorners[0].y - corners[0].y;
        tooltip.position += offset;
    }
}
