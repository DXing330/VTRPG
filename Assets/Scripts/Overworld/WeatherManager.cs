using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeatherManager", menuName = "ScriptableObjects/Overworld/WeatherManager", order = 1)]
public class WeatherManager : ScriptableObject
{
    public List<string> weathers;

    public string ReturnNextWeather(string season)
    {
        string weather = weathers[Random.Range(0, weathers.Count)];
        if (weather == season) { return weather; }
        return weathers[Random.Range(0, weathers.Count)];
    }
}
