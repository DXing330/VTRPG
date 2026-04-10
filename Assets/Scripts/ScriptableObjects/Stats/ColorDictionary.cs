using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "Colors", menuName = "ScriptableObjects/DataContainers/Colors", order = 1)]
public class ColorDictionary : ScriptableObject
{
    public Color defaultColor;
    public Color GetDefaultColor(){return defaultColor;}
    public List<string> keys;
    public List<string> colorNames;
    public List<Color> colors;

    public bool ColorNameExists(string colorName)
    {
        return colorNames.Contains(colorName);
    }

    public Color GetColor(string thing)
    {
        int indexOf = colorNames.IndexOf(thing);
        if (indexOf < 0)
        {
            return GetColorByKey(thing);
        }
        return colors[indexOf];
    }

    public Color GetColorByName(string colorName)
    {
        int indexOf = colorNames.IndexOf(colorName);
        if (indexOf < 0)
        {
            return defaultColor;
        }
        return colors[indexOf];
    }

    public Color GetColorByKey(string key)
    {
        int indexOf = keys.IndexOf(key);
        if (indexOf < 0)
        {
            return defaultColor;
        }
        return colors[indexOf];
    }

    public Color GetColorByIndex(int index)
    {
        if (index < 0 || index >= colors.Count){return defaultColor;}
        return colors[index];
    }

    public string GetColorNameByKey(string key)
    {
        int indexOf = keys.IndexOf(key);
        if (indexOf < 0)
        {
            return key;
        }
        return colorNames[indexOf];
    }
}
