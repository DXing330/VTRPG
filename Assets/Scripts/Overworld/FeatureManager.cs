using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeatureManager : MonoBehaviour
{
    public StatDatabase featureTerrainToScenes;
    public StatDatabase featureTerrainToSpecifics;

    public string ReturnSceneName(string featureTerrain)
    {
        return featureTerrainToScenes.ReturnValue(featureTerrain);
    }

    public string ReturnFeatureSpecifics(string featureTerrain)
    {
        return featureTerrainToSpecifics.ReturnValue(featureTerrain);
    }

    public List<string> ReturnFeatureSpecificsList(string featureTerrain)
    {
        string specifics = ReturnFeatureSpecifics(featureTerrain);
        return specifics.Split(featureTerrainToSpecifics.valueDelimiter).ToList();
    }

    // Specifically returns enemies lists for now.
    public List<string> ReturnRandomFeatureSpecificsList(string featureTerrain)
    {
        List<string> possibleFeatures = ReturnFeatureSpecificsList(featureTerrain);
        int index = Random.Range(0, possibleFeatures.Count);
        return possibleFeatures[index].Split(",").ToList();
    }
}
