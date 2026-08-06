using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemValueEntry
{
    public string name;
    public float value;
}

[System.Serializable]
public class ItemValueDatabaseJson
{
    public int version;
    public List<ItemValueEntry> items;
}

[System.Serializable]
public class ItemCombination
{
    public string combinationName;
    public string components;
    public float value;
    public ItemCombination(string name, string comps, float val)
    {
        combinationName = name;
        components = comps;
        value = val;
    }
}

public class AutoChessItemValueDatabase : MonoBehaviour
{
    public TextAsset jsonFile;

    public int expectedVersion = 1;

    private Dictionary<string,float> itemValues =
        new Dictionary<string,float>();

    private Dictionary<string,float> componentExpectedValues = new();
    public float GetComponentValue(string component)
    {
        if(componentExpectedValues.TryGetValue(component, out float value))
        {
            return value;
        }
        return 0f;
    }


    void Awake()
    {
        Load();
        CalculateComponentValue();
    }

    public void Load()
    {
        if(jsonFile == null)
        {
            Debug.LogWarning(
                "Missing item value JSON"
            );
            return;
        }

        ItemValueDatabaseJson data =
            JsonUtility.FromJson<ItemValueDatabaseJson>(
                jsonFile.text
            );

        if(data == null || data.items == null)
        {
            Debug.LogWarning(
                "Invalid item value JSON"
            );
            return;
        }

        if(data.version != expectedVersion)
        {
            Debug.LogWarning(
                "Item database version mismatch"
            );
        }

        itemValues.Clear();

        foreach(var item in data.items)
        {
            itemValues[item.name] = item.value;
        }

        Debug.Log(
            "Loaded item values: "
            + itemValues.Count
        );
        BuildCombinationCache();
    }

    public float GetItemTrainingScore(string item)
    {
        if(itemValues.TryGetValue(item, out float value))
        {
            return value;
        }

        // Unknown items get low value
        return 0.1f;
    }

    void CalculateComponentValue()
    {
        componentExpectedValues.Clear();
        for (int i = 0; i < itemOrder.Count; i++)
        {
            string component = itemOrder[i];
            float value = GetComponentFutureValue(component);
            componentExpectedValues[component] = value;
        }
    }

    float GetComponentFutureValue(string component)
    {
        float bestValue = 0f;
        float averageValue = 0f;
        for (int i = 0; i < itemOrder.Count; i++)
        {
            float comboValue = GetItemTrainingScore(CombineEquipment(component, itemOrder[i]));
            averageValue += comboValue;
            if (comboValue > bestValue)
            {
                bestValue = comboValue;
            }
        }
        averageValue /= itemOrder.Count;
        return (bestValue + averageValue) / 2;
    }
    private Dictionary<string,string> combinationCache = new();
    string CombinationKey(string a, string b)
    {
        return a + "|" + b;
    }
    void BuildCombinationCache()
    {
        combinationCache.Clear();
        for(int i = 0; i < itemOrder.Count; i++)
        {
            for(int j = 0; j < itemOrder.Count; j++)
            {
                combinationCache[CombinationKey(itemOrder[i], itemOrder[j])] = itemCombinations[i,j];
            }
        }
    }
    public string CombineEquipment(string first, string second)
    {
        string key = CombinationKey(first, second);
        if(combinationCache.TryGetValue(key,out string result))
            return result;
        return "";
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
    readonly string[,] itemCombinations =
    {
        // Aegirian Blade
        {
            "Death Blade", "Edge of Night", "Sterak's Gage", "Spear of Shojin", "Hextech Gunblade", "Bloodthirster", "Giant Slayer", "Infinity Edge", "Aegir Emblem", "Infinity Edge", "Giant Slayer", "Spear of Shojin", "Death Blade", "Hextech Gunblade"
        },
        // Protection Drone
        {
            "Edge of Night", "Bramble Vest", "Sunfire Cape", "Protector's Vow", "Crown Guard", "Gargoyle Stoneplate", "Titan's Resolve", "Steadfast Heart", "Aid Emblem", "Steadfast Heart", "Titan's Resolve", "Protector's Vow", "Edge of Night", "Crown Guard"
        },
        // Goliath Helmet
        {
            "Sterak's Gage", "Sunfire Cape", "Warmog's Armor", "Spirit Visage", "Morellonomicon", "Evenshroud", "Nashor's Tooth", "Striker's Flail", "Durable Emblem", "Striker's Flail", "Nashor's Tooth", "Spirit Visage", "Sterak's Gage", "Morellonomicon"
        },
        // Quick Combat Rations
        {
            "Spear of Shojin", "Protector's Vow", "Spirit Visage", "Blue Buff", "Archangel's Staff", "Adaptive Helm", "Void Staff", "Hand of Justice", "Swift Emblem", "Hand of Justice", "Void Staff", "Blue Buff", "Spear of Shojin", "Archangel's Staff"
        },
        // Laser Sight
        {
            "Hextech Gunblade", "Crown Guard", "Morellonomicon", "Archangel's Staff", "Rabadon's Deathcap", "Ionic Spark", "Guinsoo's Rageblade", "Jeweled Gauntlet", "Precision Emblem", "Jeweled Gauntlet", "Guinsoo's Rageblade", "Archangel's Staff", "Hextech Gunblade", "Rabadon's Deathcap"
        },
        // Tough Launcher
        {
            "Bloodthirster", "Gargoyle Stoneplate", "Evenshroud", "Adaptive Helm", "Ionic Spark", "Dragon's Claw", "Kraken's Fury", "Quicksilver", "Resilient Emblem", "Quicksilver", "Kraken's Fury", "Adaptive Helm", "Bloodthirster", "Ionic Spark"
        },
        // Minature Accelerator
        {
            "Giant Slayer", "Titan's Resolve", "Nashor's Tooth", "Void Staff", "Guinsoo's Rageblade", "Kraken's Fury", "Red Buff", "Last Whisper", "Agile Emblem", "Last Whisper", "Red Buff", "Void Staff", "Giant Slayer", "Guinsoo's Rageblade"
        },
        // Raid Grenade
        {
            "Infinity Edge", "Steadfast Heart", "Striker's Flail", "Hand of Justice", "Jeweled Gauntlet", "Quicksilver", "Last Whisper", "Thief's Gloves", "Raid Emblem", "Thief's Gloves", "Last Whisper", "Hand of Justice", "Infinity Edge", "Jeweled Gauntlet"
        },
        // Damazti Isomorph
        {
            "Aegir Emblem", "Aid Emblem", "Durable Emblem", "Swift Emblem", "Precision Emblem", "Resilient Emblem", "Agile Emblem", "Raid Emblem", "HR File", "Kjerag Emblem", "Laterano Emblem", "Sargon Emblem", "Victoria Emblem", "Yan Emblem"
        },
        // Kjeragi Nevermeltice
        {
            "Infinity Edge", "Steadfast Heart", "Striker's Flail", "Hand of Justice", "Jeweled Gauntlet", "Quicksilver", "Last Whisper", "Thief's Gloves", "Kjerag Emblem", "Kjeragandr's Tears", "Last Whisper", "Hand of Justice",
            "Infinity Edge", "Jeweled Gauntlet"
        },
        // Lateran Clip
        {
            "Giant Slayer", "Titan's Resolve", "Nashor's Tooth", "Void Staff", "Guinsoo's Rageblade", "Kraken's Fury", "Red Buff", "Last Whisper", "Laterano Emblem", "Last Whisper", "Gun-Knight's Might", "Void Staff", "Giant Slayer", "Guinsoo's Rageblade"
        },
        // Sargonian Teaspresso
        {
            "Spear of Shojin", "Protector's Vow", "Spirit Visage", "Blue Buff", "Archangel's Staff", "Adaptive Helm", "Void Staff", "Hand of Justice", "Sargon Emblem", "Hand of Justice", "Void Staff", "Desert Compass", "Spear of Shojin", "Archangel's Staff"
        },
        // Victorian Hammer
        {
            "Death Blade", "Edge of Night", "Sterak's Gage", "Spear of Shojin", "Hextech Gunblade", "Bloodthirster", "Giant Slayer", "Infinity Edge", "Victoria Emblem", "Infinity Edge", "Giant Slayer", "Spear of Shojin", "Steam Heart", "Hextech Gunblade"
        },
        // Yanese Dagger
        {
            "Hextech Gunblade", "Crown Guard", "Morellonomicon", "Archangel's Staff", "Rabadon's Deathcap", "Ionic Spark", "Guinsoo's Rageblade", "Jeweled Gauntlet", "Yan Emblem", "Jeweled Gauntlet", "Guinsoo's Rageblade", "Archangel's Staff", "Hextech Gunblade", "Tianshi's Cauldron"
        }
    };
}