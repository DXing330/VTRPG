using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionCityBuilding : MonoBehaviour
{
    // Level is adjustable.
    public int level;
    public void ResetLevel(){level = 1;}
    public void SetLevel(int newLevel){level = Mathf.Max(1, newLevel);}
    public void GainLevel()
    {
        if (level >= maxLevel){return;}
        level++;
    }
    public int GetLevel(){return level;}
    public string delimiter = "-";
    public string GetBuildingString()
    {
        return GetBuildingName() + delimiter + GetLevel();
    }
    // Fixed stats.
    public string buildingName;
    public string GetBuildingName(){return buildingName;}
    public bool MatchName(string newInfo){return buildingName == newInfo;}
    public int maxLevel;
    public bool MaxLevel(){return level >= maxLevel;}public int baseUpgradeCost;
    protected int GetScalingByLevel(string scaling)
    {
        switch (scaling)
        {
            default:
            return GetLevel();
            case "Quadratic":
            return GetLevel() * GetLevel();
        }
    }
    public string upgradeScaling;
    public int GetUpgradeCost()
    {
        int cost = baseUpgradeCost;
        cost *= GetScalingByLevel(upgradeScaling);
        return cost;
    }
    public int basePopulationRequirement;
    public string populationRequirementScaling;
    public int GetPopulationRequirement()
    {
        int cost = basePopulationRequirement;
        cost *= GetScalingByLevel(populationRequirementScaling);
        return cost;
    }
}
