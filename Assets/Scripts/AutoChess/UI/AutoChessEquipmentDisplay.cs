using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Part Of The UI, In Charge Of All It's Own Data + Updating.
public class AutoChessEquipmentDisplay : MonoBehaviour
{
    public AutoChessDataManager dataManager;
    public List<string> allUniqueEquipment;
    public List<int> uniqueEquipmentQuantity;
    public void UpdateEquipmentLists()
    {
        allUniqueEquipment = dataManager.GetEquipment().Distinct().ToList();
        uniqueEquipmentQuantity.Clear();
        for (int i = 0; i < allUniqueEquipment.Count; i++)
        {
            uniqueEquipmentQuantity.Add(dataManager.GetEquipmentCount(allUniqueEquipment[i]));
        }
    }
    public SpriteContainer masterSprites;
    public GeneralUtility utility;
    public List<GameObject> equipDisplayObjects;
    public List<GameObject> changePageObjects;
    public int page = 0;
    public void ChangePage(bool right = true)
    {
        page = utility.ChangePage(page, right, equipDisplayObjects, allUniqueEquipment);
        UpdateCurrentPage();
    }
    public List<TMP_Text> equipQuantities;
    public void ResetDisplay()
    {
        page = 0;
        utility.DisableGameObjects(changePageObjects);
    }
    public void UpdateDisplay()
    {
        ResetDisplay();
        UpdateEquipmentLists();
        if (allUniqueEquipment.Count > equipDisplayObjects.Count)
        {
            utility.EnableGameObjects(changePageObjects);
        }
        UpdateCurrentPage();
    }
    public List<AutoChessEquipmentToolTip> equipmentToolTips;
    public void UpdateCurrentPage()
    {
        utility.DisableGameObjects(equipDisplayObjects);
        List<string> currentPageStrings = utility.GetCurrentPageStrings(page, equipDisplayObjects, allUniqueEquipment);
        for (int i = 0; i < currentPageStrings.Count; i++)
        {
            int indexOf = allUniqueEquipment.IndexOf(currentPageStrings[i]);
            equipDisplayObjects[i].SetActive(true);
            equipQuantities[i].text = uniqueEquipmentQuantity[indexOf].ToString();
            equipmentToolTips[i].SetEquipName(allUniqueEquipment[indexOf]);
            masterSprites.ApplyToImage(equipmentToolTips[i].GetEquipImage(), allUniqueEquipment[indexOf]);
        }
    }
    public EquipmentDetailViewerSwitch equipmentDescriptions;
    public void ViewEquipment(int index)
    {
        if (index < 0 || index >= equipmentToolTips.Count){return;}
        string equipName = equipmentToolTips[index].GetEquipName();
        equipmentToolTips[index].ShowTooltip(equipName + ":\n" + equipmentDescriptions.ReturnAutoChessEquipmentDescription(equipName));
    }
}
