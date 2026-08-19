using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessEquipmentGrid : MonoBehaviour
{
    public GeneralUtility utility;
    public AutoChessDataManager dataManager;
    public void UpdateGrid(bool full = true)
    {
        utility.DisableGameObjects(equipToolTipObjects);
        if (full || dataManager == null)
        {
            utility.EnableGameObjects(equipToolTipObjects);
            return;
        }
        // Update Based On Equipment Inventory.
        List<string> availableEquipment = dataManager.GetEquipment();
        int index = 0;
        for (int row = 0; row < 15; row++)
        {
            for (int col = 0; col < 15; col++)
            {
                AutoChessEquipmentToolTip tip = equipToolTips[index];
                GameObject tooltipObject = equipToolTipObjects[index];
                bool show = false;
                // Top-left corner.
                if (row == 0 && col == 0)
                {
                    show = false;
                }
                else if (row == 0)
                {
                    show = availableEquipment.Contains(itemOrder[col - 1]);
                }
                // Header column
                else if (col == 0)
                {
                    show = availableEquipment.Contains(itemOrder[row - 1]);
                }
                else
                {
                    // Combination.
                    string first = itemOrder[row - 1];
                    string second = itemOrder[col - 1];
                    if (first == second)
                    {
                        // Same component needs two copies.
                        int count = 0;
                        foreach (string equipment in availableEquipment)
                        {
                            if (equipment == first){count++;}
                        }
                        show = count >= 2;
                    }
                    else
                    {
                        show = availableEquipment.Contains(first)
                            && availableEquipment.Contains(second);
                    }
                }
                tooltipObject.SetActive(show);
                index++;
            }
        }
    }
    public List<GameObject> equipToolTipObjects;
    public List<AutoChessEquipmentToolTip> equipToolTips;
    [SerializeField]
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
    public SpriteContainer masterSprites;
    [ContextMenu("Initialize")]
    public void Initialize()
    {
        int index = 0;
        for (int row = 0; row < 15; row++)
        {
            for (int col = 0; col < 15; col++)
            {
                AutoChessEquipmentToolTip tip = equipToolTips[index++];
                // Top-left corner
                if (row == 0 && col == 0)
                {
                    tip.SetEquipName("");
                }
                // Header row
                else if (row == 0)
                {
                    tip.SetEquipName(itemOrder[col - 1]);
                    masterSprites.ApplyToImage(tip.GetEquipImage(), itemOrder[col - 1]);
                }
                // Header column
                else if (col == 0)
                {
                    tip.SetEquipName(itemOrder[row - 1]);
                    masterSprites.ApplyToImage(tip.GetEquipImage(), itemOrder[row - 1]);
                }
                // Combination
                else
                {
                    tip.SetEquipName(itemCombinations[row - 1, col - 1]);
                    masterSprites.ApplyToImage(tip.GetEquipImage(), itemCombinations[row - 1, col - 1]);
                }
            }
        }
    }
}
