using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeasonManager", menuName = "ScriptableObjects/Overworld/SeasonManager", order = 1)]
public class SeasonManager : ScriptableObject
{
    public List<string> seasons;
    public int seasonLength;

    public string ReturnNextSeason(int day)
    {
        return seasons[(day / seasonLength) % seasons.Count];
    }
}
