using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CurrentMapPassiveViewer : BattleUIBaseClass
{
    public BattleMap map;
    public StatusDetailViewer detailViewer;
    public TacticActor cActor;
    public override void SetActor(TacticActor actor)
    {
        cActor = actor;
    }
    public TMP_Text passiveText;
    public override void ResetUI()
    {
        passiveText.text = "";
    }
    public bool time;
    public bool weather;
    public bool tEffect;
    public bool tile;
    public override void UpdateUI()
    {
        ResetUI();
        int location = -1;
        if (cActor != null)
        {
            location = cActor.GetLocation();
        }
        if (time)
        {
            passiveText.text = detailViewer.ReturnWeatherDetails(map.GetTime());
        }
        else if (weather)
        {
            passiveText.text = detailViewer.ReturnWeatherDetails(map.GetWeather());
        }
        else if (tEffect)
        {
            if (location < 0){return;}
            passiveText.text = detailViewer.MapTEffectPassives(map, location);
        }
        else
        {
            if (location < 0){return;}
            string tile = map.mapInfo[location];
            passiveText.text = detailViewer.MapTilePassives(map, location);
        }
    }
}
