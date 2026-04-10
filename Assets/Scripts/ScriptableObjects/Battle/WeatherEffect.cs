using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeatherEffect", menuName = "ScriptableObjects/BattleLogic/WeatherEffect", order = 1)]
public class WeatherEffect : SkillEffect
{
    public string timing;
    public List<string> effects;
    public List<string> specifics;

    public void Reset()
    {
        timing = "";
        effects.Clear();
        specifics.Clear();
    }

    public void LoadWeather(string newInfo)
    {
        string[] blocks = newInfo.Split("|");
        if (blocks.Length < 3)
        {
            Reset();
            return;
        }
        timing = blocks[0];
        effects = blocks[1].Split(",").ToList();
        specifics = blocks[2].Split(",").ToList();
    }

    protected bool Timing(string time)
    {
        if (timing == "ALL") { return true; }
        return timing == time;
    }

    public void ApplyEffects(TacticActor actor, string time)
    {
        if (!Timing(time))
        {
            return;
        }
        for (int i = 0; i < effects.Count; i++)
        {
            AffectActor(actor, effects[i], specifics[i]);
        }
    }
}
