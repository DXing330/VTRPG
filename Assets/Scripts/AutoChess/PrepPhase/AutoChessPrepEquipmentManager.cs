using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// In charge of assigning/combining equipment.
public class AutoChessPrepEquipmentManager : MonoBehaviour
{
    public AutoChessDataManager dataManager;
    public AutoChessEquipmentDisplay equipmentDisplay;
    public GeneralUtility utility;
    public List<string> allUniqueEquipment;
    public List<int> uniqueEquipmentQuantity;
    public void UpdateEquipmentSelectList()
    {
        allUniqueEquipment = dataManager.GetEquipment().Distinct().ToList();
        uniqueEquipmentQuantity.Clear();
        for (int i = 0; i < allUniqueEquipment.Count; i++)
        {
            uniqueEquipmentQuantity.Add(dataManager.GetEquipmentCount(allUniqueEquipment[i]));
        }
        selectEquipList.SetSelectables(allUniqueEquipment);
    }
    public GameObject selectEquipObject;
    public ImageSelectList selectEquipList;
    public GameObject currentEquipObject;
    public SpriteContainer masterSprites;
    public List<GameObject> currentEquipBGObjects;
    public List<GameObject> currentEquipObjects;
    public List<AutoChessEquipmentToolTip> currentEquipToolTips;
    public List<Image> currentEquipImages;
    public AutoActorRollUpData currentActor;
    public void UpdateCurrentEquipment()
    {
        utility.DisableGameObjects(currentEquipBGObjects);
        utility.DisableGameObjects(currentEquipObjects);
        List<string> equipNames = currentActor.GetEquipmentNames();
        for (int i = 0; i < Mathf.Min(currentEquipImages.Count, equipNames.Count); i++)
        {
            currentEquipBGObjects[i].SetActive(true);
            currentEquipObjects[i].SetActive(true);
            currentEquipToolTips[i].SetEquipName(equipNames[i]);
            masterSprites.ApplyToImage(currentEquipImages[i], equipNames[i]);
        }
    }
    public EquipmentDetailViewerSwitch equipmentDescriptions;
    public void ViewEquipment(AutoChessEquipmentToolTip clickedToolTip)
    {
        string equipName = clickedToolTip.GetEquipName();
        clickedToolTip.ShowTooltip(equipName + ":\n" + equipmentDescriptions.ReturnAutoChessEquipmentDescription(equipName));
    }
    public void ViewCurrentEquipment(AutoChessEquipmentToolTip clickedToolTip)
    {
        if (currentActor == null){return;}
        List<string> equipped = currentActor.GetEquipmentNames();
        string equipName = equipped[clickedToolTip.tooltipIndex];
        clickedToolTip.ShowTooltip(equipName + ":\n" + equipmentDescriptions.ReturnAutoChessEquipmentDescription(equipName));
    }
    public void ViewEquipmentInInventory(AutoChessEquipmentToolTip clickedToolTip)
    {
        int indexOf = selectEquipList.GetSelected();
        if (indexOf < 0){return;}
        string equipName = allUniqueEquipment[indexOf];
        clickedToolTip.ShowTooltip(equipName + ":\n" + equipmentDescriptions.ReturnAutoChessEquipmentDescription(equipName));
    }
    public void StopManagingEquipment()
    {
        selectEquipObject.SetActive(false);
        currentEquipObject.SetActive(false);
        currentActor = null;
    }
    public void SetCurrentActor(AutoActorRollUpData actor)
    {
        currentActor = actor;
    }
    public void ManageActorEquipment(AutoActorRollUpData actor)
    {
        if (actor == null)
        {
            StopManagingEquipment();
            return;
        }
        SetCurrentActor(actor);
        UpdateEquipmentSelectList();
        UpdateCurrentEquipment();
        selectEquipObject.SetActive(true);
        currentEquipObject.SetActive(true);
    }
    // This Is The Equip Equipment Function.
    public void SelectEquipment()
    {
        // Max Of 3 Equipment/Actor
        if (currentActor == null){return;}
        if (currentActor.GetEquipmentNames().Count >= 3){return;}
        // Determine The Equipment.
        int indexOf = selectEquipList.GetSelected();
        if (indexOf < 0){return;}
        string equipName = allUniqueEquipment[indexOf];
        dataManager.AddLog("Equipped " + equipName + " to " + currentActor.GetName());
        // Check For Any Merges.
        // Only Need To Check The Previous Item, Since Any Other Items Would Have Already Been Combined.
        string latestEquip = currentActor.GetLatestEquipment();
        string combinedEquip = CombineEquipment(equipName, latestEquip);
        // Add The Equipment To The Actor.
        if (combinedEquip != "")
        {
            currentActor.RemoveLatestEquipment();
            currentActor.EquipEquipment(combinedEquip);
            dataManager.AddLog(equipName + " combined with " + latestEquip + " to form " + combinedEquip);
        }
        else
        {
            currentActor.EquipEquipment(equipName);
        }
        // Remove The Equipment From The DataManager + Update UI
        dataManager.UseEquipment(equipName);
        selectEquipList.ResetSelected();
        UpdateCurrentEquipment();
        UpdateEquipmentSelectList();
        equipmentDisplay.UpdateDisplay();
    }
    public string CombineEquipment(string firstItem, string secondItem)
    {
        int firstIndex = itemOrder.IndexOf(firstItem);
        int secondIndex = itemOrder.IndexOf(secondItem);
        if (firstIndex < 0 || secondIndex < 0)
        {
            return "";
        }
        return itemCombinations[firstIndex, secondIndex];
    }
    [ContextMenu("Test Equipment Combinations")]
    protected void TestEquipmentCombinations()
    {
        TestCombination("Aegirian Blade", "Protection Drone");
        TestCombination("Aegirian Blade", "Laser Sight");
        TestCombination("Protection Drone", "Raid Grenade");
        TestCombination("Damazti Isomorph", "Victorian Hammer");
        TestCombination("Lateran Clip", "Lateran Clip");
        TestCombination("Sargonian Teaspresso", "Sargonian Teaspresso");
        TestCombination("Victorian Hammer", "Victorian Hammer");
        TestCombination("Yanese Dagger", "Yanese Dagger");
    }
    protected void TestCombination(string itemA, string itemB)
    {
        int indexA = itemOrder.IndexOf(itemA);
        int indexB = itemOrder.IndexOf(itemB);
        if (indexA < 0 || indexB < 0)
        {
            Debug.LogError($"Missing item index: {itemA} + {itemB}");
            return;
        }
        string result = itemCombinations[indexA, indexB];
        Debug.Log($"{itemA} + {itemB} = {result}");
    }
    [ContextMenu("Test Item Combination Table")]
    protected void TestItemCombinationTable()
    {
        bool passed = true;
        // Check the table is symmetric.
        for (int row = 0; row < itemOrder.Count; row++)
        {
            for (int col = 0; col < itemOrder.Count; col++)
            {
                if (itemCombinations[row, col] != itemCombinations[col, row])
                {
                    Debug.LogError(
                        $"Mismatch! [{row},{col}] {itemOrder[row]} + {itemOrder[col]} = {itemCombinations[row, col]}, " +
                        $"but [{col},{row}] = {itemCombinations[col, row]}");
                    passed = false;
                }
            }
        }
        // Print the table in spreadsheet order.
        Debug.Log("===== ITEM COMBINATION TABLE =====");
        for (int row = 0; row < itemOrder.Count; row++)
        {
            Debug.Log($"--- {itemOrder[row]} ---");

            for (int col = 0; col < itemOrder.Count; col++)
            {
                Debug.Log($"{itemOrder[row]} + {itemOrder[col]} = {itemCombinations[row, col]}");
            }
        }
        if (passed)
            Debug.Log("✓ Combination table is symmetric.");
        else
            Debug.LogError("✗ Combination table contains symmetry errors.");
    }
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
}
