using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionUnitManager : MonoBehaviour
{
    public FactionMap map;
    public FactionUnitDataManager allUnits;
    public StatDatabase allActors;
    public StatDatabase allEquipment;
    public TacticActor dummyActor;
    public List<FactionUnit> units = new List<FactionUnit>();

    public void Load()
    {
        allUnits.Load();
        List<string> unitData = allUnits.GetSavedUnits();
        for (int i = 0; i < unitData.Count; i++)
        {
            FactionUnit newUnit = new FactionUnit();
            newUnit.ResetStats();
            newUnit.SetStats(unitData[i]);
            units.Add(newUnit);
        }
    }

    public void SaveUnitData()
    {
        allUnits.Save(units);
    }

    public void CityMakesUnit(FactionCity city)
    {
        // Make the unit based on the city.
        FactionUnit newUnit = new FactionUnit();
        newUnit.ResetStats();
        newUnit.SetFaction(city.GetFaction());
        newUnit.SetLocation(city.GetLocation());
        newUnit.SetPreviousLocation(city.GetLocation());
        // Determine the starting sprite / size / stat / equipment based on city levels.
        units.Add(newUnit);
    }
}
