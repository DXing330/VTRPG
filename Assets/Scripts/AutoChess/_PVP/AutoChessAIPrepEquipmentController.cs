using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessAIPrepEquipmentController : MonoBehaviour
{
    protected AutoChessAIPrepController controller;
    bool EmblemSynergy(string emblemName)
    {
        string faction = emblemName.Replace(" Emblem", "");
        return controller.SynergyValue(faction) > 0;
    }
    protected AutoChessPVPGenome genome;
    protected AutoChessPrepManager prepManager;
    bool EquipEmblemToUnit(string emblemName, bool exists = true)
    {
        int bestIndex = -1;
        float worstScore = float.MaxValue;
        string emblemFaction = emblemName.Replace(" Emblem", "");
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            AutoActorRollUpData unit = prepManager.fieldSlots[i];
            // Equip It To The Worst Unit With Open Slots That Is Not Of The Faction.
            if (unit.GetOpenEquipmentSlots() <= 0){continue;}
            if (unit.FactionExists(emblemFaction) || unit.EmblemExists(emblemFaction)){continue;}
            float score = controller.KeepScore(unit);
            if (score < worstScore)
            {
                worstScore = score;
                bestIndex = i;
            }
        }
        if (bestIndex >= 0)
        {
            prepManager.fieldSlots[bestIndex].EquipEquipment(emblemName);
            if (exists)
            {
                prepManager.dataManager.UseEquipment(emblemName);
            }
            return true;
        }
        return false;
    }
    bool EquipItemToUnit(string itemName, bool exists = true)
    {
        // Find The Best Matching Unit For The Equipment.
        int bestIndex = -1;
        float bestScore = 0;
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            AutoActorRollUpData unit = prepManager.fieldSlots[i];
            // Don't Equip If Full Obviously.
            if (unit.GetOpenEquipmentSlots() <= 0){continue;}
            // Don't Equip If Tier Is Not High Enough
            if (controller.GetUnitTier(unit.GetName()) < genome.GetByName("W_MIN_TIER_FOR_ITEM")){continue;}
            // Don't Equip If The Equipment Exists Already And It's Not Stackable.
            if (unit.EquipmentExists(itemName) && !ItemStackable(itemName)){continue;}
            float score = itemValueDatabase.GetTagCompatibility(ReturnItemTypes(itemName), GetUnitRoles(unit.GetName()));
            // Don't Equip If Compatibility Score Is Negative Or Zero.
            if (score <= 0f){continue;}
            // Not only a match but check the value of the unit:
            score += genome.GetByName("W_ITEM_FOCUS_HIGH_TIER_UNIT") * controller.KeepScore(unit);
            // Also Check The Synergy Of The Unit?
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }
        if (bestIndex >= 0)
        {
            prepManager.fieldSlots[bestIndex].EquipEquipment(itemName);
            if (controller.recordAIDecisions)
            {
                prepManager.dataManager.AddLog($"ItemTarget:{itemName}->{prepManager.fieldSlots[bestIndex].GetName()},Score:{bestScore:F2}");
            }
            if (exists)
            {
                prepManager.dataManager.UseEquipment(itemName);
            }
            return true;
        }
        return false;
    }
    public void AutoPlaceEquipment(AutoChessAIPrepController newController)
    {
        controller = newController;
        genome = controller.genome;
        prepManager = newController.prepManager;
        // Heuristic Approach.
        // 1. Find regular equipment.
        List<string> combined = GetAvailableCombinedItems(prepManager.dataManager);
        // 2. Equip vs Save
        for (int i = 0; i < combined.Count; i++)
        {
            if (combined[i].Contains("Emblem") && EmblemSynergy(combined[i]))
            {
                if (controller.recordAIDecisions)
                {
                    prepManager.dataManager.AddLog($"EmblemDecision:{combined[i]},Synergy:{controller.SynergyValue(combined[i].Replace(" Emblem", "")):F2}");
                }
                EquipEmblemToUnit(combined[i]);
                continue;
            }
            float itemValue = ItemValue(combined[i]);
            if (controller.recordAIDecisions)
            {
                prepManager.dataManager.AddLog($"ItemEvaluate:{combined[i]},Value:{itemValue:F2},Save:{genome.GetByName("W_ITEM_SAVE"):F2}");
            }
            if (itemValue > genome.GetByName("W_ITEM_SAVE"))
            {
                if (EquipItemToUnit(combined[i]))
                {
                    if (controller.recordAIDecisions)
                    {
                        prepManager.dataManager.AddLog($"ItemDecision~Equip:{combined[i]},Value:{itemValue:F2}");
                    }
                }
            }
        }
        // 1. Find possible combinations
        List<string> components = GetAvailableComponents();
        List<ItemCombination> combinations = new();
        for(int i = 0; i < components.Count; i++)
        {
            for(int j = i + 1; j < components.Count; j++)
            {
                string result = CombineEquipment(components[i], components[j]);
                if(!string.IsNullOrEmpty(result))
                {
                    float combinedValue = ItemValue(result);
                    combinedValue -= genome.GetByName("W_ITEM_COMPONENT_SAVE") * GetComponentFutureValue(components[i]);
                    combinedValue -= genome.GetByName("W_ITEM_COMPONENT_SAVE") * GetComponentFutureValue(components[j]);
                    ItemCombination newCombination = new(result, components[i] + "|" + components[j], combinedValue);
                    if (controller.recordAIDecisions)
                    {
                        prepManager.dataManager.AddLog($"CombineEvaluate:{components[i]}+{components[j]}->{result},Value:{combinedValue:F2}");
                    }
                    combinations.Add(newCombination);
                }
            }
        }
        combinations.Sort((a, b) => a.value.CompareTo(b.value));
        for (int i = combinations.Count - 1; i >= 0; i--)
        {
            // Check If The Components Still Exist.
            if (!prepManager.dataManager.EquipmentComponentsExists(combinations[i].components)){continue;}
            float combinedValue = combinations[i].value;
            // If Emblem + Hit Threshold Then Go, Else Pass.
            if (combinations[i].combinationName.Contains("Emblem") && EmblemSynergy(combinations[i].combinationName))
            {
                if (EquipEmblemToUnit(combinations[i].combinationName, false))
                {
                    prepManager.dataManager.RemoveComponents(combinations[i].components);
                }
                continue;
            }
            // Check If Equip.
            if (combinedValue > genome.GetByName("W_ITEM_SAVE"))
            {
                if (EquipItemToUnit(combinations[i].combinationName, false))
                {
                    // Remove The Components If Equipped.
                    prepManager.dataManager.RemoveComponents(combinations[i].components);
                    if (controller.recordAIDecisions)
                    {
                        prepManager.dataManager.AddLog($"ItemDecision~Combine:{combinations[i].components}->{combinations[i].combinationName},Value:{combinedValue:F2}");
                    }
                }
            }
        }
    }
    List<string> GetAvailableComponents()
    {
        List<string> components = new();
        List<string> equipment = prepManager.dataManager.GetEquipment();
        for(int i = 0; i < equipment.Count; i++)
        {
            if(IsComponent(equipment[i]))
            {
                components.Add(equipment[i]);
            }
        }
        return components;
    }
    List<string> GetAvailableCombinedItems(AutoChessDataManager dataManager)
    {
        List<string> combined = new();
        List<string> equipment = dataManager.GetEquipment();
        for(int i = 0; i < equipment.Count; i++)
        {
            if(!IsComponent(equipment[i]))
            {
                combined.Add(equipment[i]);
            }
        }
        return combined;
    }
    float ItemValue(string itemName)
    {
        float value = 0f;
        // Value is based on history + need + best match.
        value += ItemTrainingScore(itemName);
        value -= genome.GetByName("W_ITEM_DUPLICATE_PENALTY") * ItemTypeDuplicatePenalty(ReturnItemTypes(itemName));
        value += genome.GetByName("W_ITEM_UNIT_MATCH") *
         ItemBestMatch(itemName);
        return value;
    }
    float ItemTypeDuplicatePenalty(List<string> itemTypes)
    {
        float itemTypeCount = 0f;
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            foreach (string itemName in prepManager.fieldSlots[i].GetEquipmentNames())
            {
                foreach (string itemType in itemTypes)
                {
                    if (ReturnItemTypes(itemName).Contains(itemType))
                    {
                        itemTypeCount++;
                        break;
                    }
                }
            }
        }
        return itemTypeCount;
    }
    public StatDatabase unitRoles;
    public List<string> GetUnitRoles(string unitName)
    {
        return unitRoles.ReturnStats(unitName);
    }
    public StatDatabase itemTypeData;
    public List<string> ReturnItemTypes(string itemName)
    {
        return itemTypeData.ReturnStats(itemName);
    }
    float ItemBestMatch(string itemName)
    {
        float bestMatch = 0;
        for (int i = 0; i < prepManager.fieldSlots.Count; i++)
        {
            if (prepManager.fieldSlots[i].GetOpenEquipmentSlots() <= 0){continue;}
            float match = itemValueDatabase.GetTagCompatibility(ReturnItemTypes(itemName), GetUnitRoles(prepManager.fieldSlots[i].GetName()));
            if (match > bestMatch)
            {
                bestMatch = match;
            }
        }
        return bestMatch;
    }
    public StatDatabase itemStackable;
    public bool ItemStackable(string itemName)
    {
        return itemStackable.ReturnValue(itemName) == "1";
    }
    public AutoChessItemValueDatabase itemValueDatabase;
    public string CombineEquipment(string firstItem, string secondItem)
    {
        return itemValueDatabase.CombineEquipment(firstItem, secondItem);
    }
    float ItemTrainingScore(string itemName)
    {
        return genome.GetByName("W_ITEM_VALUE") * itemValueDatabase.GetItemTrainingScore(itemName);
    }
    float GetComponentFutureValue(string component)
    {
        return itemValueDatabase.GetComponentValue(component);
    }
    bool IsComponent(string itemName)
    {
        return itemOrder.Contains(itemName);
    }
    protected List<string> itemOrder = new()
    {
        "Aegirian Blade",
        "Protection Drone",
        "Goliath Helmet",
        "Quick Combat Rations",
        "Laser Sight",
        "Tough Launcher",
        "Minature Accelerator",
        "Raid Grenade",
        "Damazti Isomorph",
        "Kjeragi Nevermeltice",
        "Lateran Clip",
        "Sargonian Teaspresso",
        "Victorian Hammer",
        "Yanese Dagger"
    };
}
