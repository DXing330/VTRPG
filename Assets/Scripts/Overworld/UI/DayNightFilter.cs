using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DayNightFilter : MonoBehaviour
{
    public OverworldState overworldState;
    void Start()
    {
        UpdateFilter(overworldState.GetHour());
    }
    public Image filter;
    public List<Color> dayNightColors;
    public void UpdateFilter(int hour)
    {
        filter.color = new Color(dayNightColors[hour].r,dayNightColors[hour].g,dayNightColors[hour].b,dayNightColors[hour].a);
    }
    public List<string> binaryTimes;
    public List<Color> binaryDayNightColors;
    public void SetTime(string newInfo)
    {
        int indexOf = binaryTimes.IndexOf(newInfo);
        if (indexOf < 0) { return; }
        filter.color = new Color(binaryDayNightColors[indexOf].r, binaryDayNightColors[indexOf].g, binaryDayNightColors[indexOf].b, binaryDayNightColors[indexOf].a);
    }
}
