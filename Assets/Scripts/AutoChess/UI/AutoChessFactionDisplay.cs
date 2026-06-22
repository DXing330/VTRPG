using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Shows All Factions, Sorting By Stack Count.
// Show Stack Count Of Each Faction And Whether It's Active Or Not.
public class AutoChessFactionDisplay : MonoBehaviour
{
    public List<GameObject> factionDisplayObjects;
    public List<RectTransform> rectTiles;
    public RectTransformAdjustor adjustor;
    public List<AutoChessFactionSlot> factionSlots;
}
