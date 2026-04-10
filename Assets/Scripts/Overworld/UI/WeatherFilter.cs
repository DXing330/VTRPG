using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeatherFilter : MonoBehaviour
{
    public bool subGame = false;
    public string currentWeather;
    public SpriteContainer sprites;
    public GameObject filterObject;
    public Image filter;
    public StatusDetailViewer weatherDetailViewer;
    public PopUpMessage weatherDetails;
    public void UpdateFilter(string weather)
    {
        currentWeather = weather;
        filterObject.SetActive(true);
        SetImage(sprites.SpriteDictionary(weather));
        if (filter.sprite == null){ filterObject.SetActive(false); }
    }
    protected void SetImage(Sprite newSprite) { filter.sprite = newSprite; }
    public void ShowWeatherDetails()
    {
        weatherDetails.SetMessage(weatherDetailViewer.ReturnWeatherDetails(currentWeather));
    }
}
