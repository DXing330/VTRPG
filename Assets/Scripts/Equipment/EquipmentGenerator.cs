using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentGenerator : MonoBehaviour
{
    public Equipment dummyEquip;
    public List<string> equipmentRarities;
    public List<int> equipmentBasePassiveLevels;
    public List<int> equipmentMaxUpgrades;
    public List<string> equipmentSlots;
    public string weaponSlotName = "Weapon";
    public StatDatabase weaponBasePassives;
    public List<StatDatabase> equipmentPassiveMaxLevels;
    public List<StatDatabase> equipmentPassiveWeights;

    protected void GenerateRandomPassive(Equipment equip, int slot)
    {
        // Get a random passive.
        string passive = equipmentPassiveWeights[slot].ReturnRandomWeightedKey();
        // Check if that passive is already max leveled.
        int mLevel = int.Parse(equipmentPassiveMaxLevels[slot].ReturnValue(passive));
        int cLevel = equip.GetLevelOfPassive(passive);
        // If not then add it to the equipment.
        if (cLevel < mLevel)
        {
            equip.AddPassive(passive);
        }
        else
        {
            GenerateRandomPassive(equip, slot);
        }
    }

    [ContextMenu("Test Generate")]
    public void TestGenerateNewEquipment()
    {
        dummyEquip.ResetStats();
        int rarity = Random.Range(0, equipmentRarities.Count);
        Debug.Log(GenerateEquipmentOfRarity(equipmentRarities[rarity]));
    }
    public int massGenerateCount;
    [ContextMenu("Test Mass Generate")]
    public void TestMassGenerateNewEquipment()
    {
        for (int i = 0; i < massGenerateCount; i++)
        {
            TestGenerateNewEquipment();
        }
    }

    protected int maxRuneSlots = 6;

    public string GenerateEquipmentOfRarity(string rarityString)
    {
        dummyEquip.ResetStats();
        int rarity = equipmentRarities.IndexOf(rarityString);
        if (rarity < 0){rarity = 0;}
        int slot = Random.Range(0, equipmentSlots.Count);
        int basePassiveLevels = equipmentBasePassiveLevels[rarity];
        string slotName = equipmentSlots[slot];
        dummyEquip.SetSlot(slotName);
        string type = slotName;
        if (slotName == weaponSlotName)
        {
            type = weaponBasePassives.ReturnRandomKey();
            List<string> weaponPassives = weaponBasePassives.ReturnValue(type).Split(",").ToList();
            for (int i = 0; i < weaponPassives.Count; i++)
            {
                dummyEquip.AddPassive(weaponPassives[i]);
            }
        }
        dummyEquip.SetName(equipmentRarities[rarity] + " " + type);
        dummyEquip.SetType(type);
        dummyEquip.SetRarity(rarity.ToString());
        dummyEquip.SetMaxUpgrades(equipmentMaxUpgrades[rarity]);
        dummyEquip.SetRuneSlots(Random.Range(Mathf.Min(maxRuneSlots,rarity) / 2, Mathf.Min(maxRuneSlots,rarity) + 1));
        for (int i = 0; i < basePassiveLevels; i++)
        {
            GenerateRandomPassive(dummyEquip, slot);
        }
        dummyEquip.RefreshStats();
        return dummyEquip.GetStats();
    }
}
