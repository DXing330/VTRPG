using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Views all kinds of items (equipment/passives/skillbooks/relics/consumables)
public class ItemDetailViewer : MonoBehaviour
{
    public TMP_Text itemName;
    public TMP_Text itemInfo;
    // Either view an equipment or an item.
    public bool viewingEquipment = true;
    public void ViewEquip(){viewingEquipment = true;}
    public void ViewItem(){viewingEquipment = false;}
    public StatDatabase equipData;
    public Equipment equipment;
    public StatDatabase itemData;
    public ActiveSkill active;
    public ActiveDescriptionViewer itemDescriptions;
    public StatDatabase dungeonItemDescriptions;
    public SelectStatTextList passiveSelect;
    public PassiveDetailViewer passiveDetails;
    public StatDatabase relicData;
    public StatDatabase skillBookData;
    public SkillDisplay skillBookDisplay;
    public void ResetView()
    {
        itemName.text = "";
        itemInfo.text = "";
        if (passiveSelect != null)
        {
            passiveSelect.SetStatsAndData(new List<string>(), new List<string>());
        }
    }
    public void ShowEquipmentInfo(string newInfo)
    {
        equipment.SetAllStats(newInfo);
        itemName.text = equipment.GetName();
        itemInfo.text = "Grants the user: ";
        passiveSelect.SetStatsAndData(equipment.GetPassives(), equipment.GetPassiveLevels());
    }
    public void ViewPassiveDetails()
    {
        int index = passiveSelect.GetSelected();
        if (index < 0) { return; }
        passiveDetails.UpdatePassiveNames(equipment.GetPassives()[index], equipment.GetPassiveLevels()[index]);
    }
    public void ResetInfo()
    {
        itemName.text = "";
        itemInfo.text = "";
    }
    public void SetInfo(string newItem, string newDescription)
    {
        itemName.text = newItem;
        itemInfo.text = newDescription;
    }
    public void ShowDungeonItemInfo(string newItem)
    {
        SetInfo(newItem, dungeonItemDescriptions.ReturnValue(newItem));
    }
    public void ShowInfo(string newItem)
    {
        string data = "";
        itemName.text = newItem;
        if (viewingEquipment)
        {
            data = equipData.ReturnValue(newItem);
            equipment.SetAllStats(data);
            itemInfo.text = "Grants the user: ";
            for (int i = 0; i < equipment.passives.Count; i++)
            {
                itemInfo.text += equipment.passives[i];
                if (i < equipment.passives.Count - 1)
                {
                    itemInfo.text += ", ";
                }
                else
                {
                    itemInfo.text += ".";
                }
            }
        }
        else
        {
            data = itemData.ReturnValue(newItem);
            if (data == "")
            {
                itemInfo.text = "";
                return;
            }
            active.LoadSkillFromString(data, null);
            itemInfo.text = itemDescriptions.ReturnActiveDescription(active);
        }
    }
    public void ShowRelicInfo(string relicName)
    {

    }
    public void ShowSkillBookInfo(string skillBookName)
    {
        string[] skillBookDataBlocks = skillBookData.ReturnValue(skillBookName).Split("_");
        itemName.text = skillBookName;
        itemInfo.text = skillBookDisplay.ReturnSkillBookDescription(skillBookDataBlocks[1], skillBookDataBlocks[0]);
    }
}