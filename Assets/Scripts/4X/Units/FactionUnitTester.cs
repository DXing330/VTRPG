using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FactionUnitTester : MonoBehaviour
{
    public string testData;
    public List<FactionUnit> units = new List<FactionUnit>();
    [ContextMenu("Test Load Unit")]
    public void TestLoadUnit()
    {
        FactionUnit loadedUnit = LoadUnit(testData);
        /* Print results to verify correctness
        Debug.Log($"Faction: {loadedUnit.faction}");
        Debug.Log($"Level: {loadedUnit.level}, Exp: {loadedUnit.exp}");
        Debug.Log($"Location: {loadedUnit.location}, Previous: {loadedUnit.previousLocation}");
        Debug.Log($"Sprites: {string.Join(",", loadedUnit.unitActorSprites)}");
        Debug.Log($"SquadSizes: {string.Join(",", loadedUnit.unitActorSquadSize)}");
        Debug.Log($"Stats: {string.Join(",", loadedUnit.unitActorStats)}");
        Debug.Log($"Equipment: {string.Join(",", loadedUnit.unitActorEquipment)}");*/
        Debug.Log(loadedUnit.GetStats() == testData);
        units.Add(loadedUnit);
    }
    public FactionUnit LoadUnit(string newInfo)
    {
        FactionUnit unit = new FactionUnit();
        unit.SetStats(newInfo);
        return unit;
    }
}
