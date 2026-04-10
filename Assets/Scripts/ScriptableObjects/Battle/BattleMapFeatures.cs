using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleMapFeatures", menuName = "ScriptableObjects/DataContainers/BattleMapFeatures", order = 1)]
public class BattleMapFeatures : ScriptableObject
{
    public string terrainType;
    public string delimiter = "|";
    public void SetTerrainType(string newType) { terrainType = newType; }
    public string GetTerrainType(){return terrainType;}
    public void ResetTerrainType() { terrainType = ""; }
    public StatDatabase baseTerrainLayouts;
    public StatDatabase tEffectLayouts;
    public StatDatabase interactableLayouts;

    public List<string> CurrentMapFeatures()
    {
        return baseTerrainLayouts.ReturnValue(GetTerrainType()).Split(delimiter).ToList();
    }
    public List<string> CurrentMapTerrainFeatures()
    {
        return tEffectLayouts.ReturnValue(GetTerrainType()).Split(delimiter).ToList();
    }
    public List<string> CurrentMapInteractables()
    {
        return interactableLayouts.ReturnValue(GetTerrainType()).Split(delimiter).ToList();
    }
}
