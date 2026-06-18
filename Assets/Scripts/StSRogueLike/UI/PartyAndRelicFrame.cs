using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyAndRelicFrame : MonoBehaviour
{
    void Start()
    {
        relicPage = 0;
        UpdateFrame();
    }
    public void UpdateFrame()
    {
        UpdateGoldText();
        UpdatePartyRelics();
        // TODO Update The Relics.
    }
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public SceneMover sceneMover;
    public void ReturnToMainMenu()
    {
        sceneMover.ReturnToMainMenu();
    }
    public StatDatabase relicDB;
    public SpriteContainer relicSprites;
    public List<string> partyRelics;
    public void UpdatePartyRelics()
    {
        partyRelics = new List<string>(partyData.dungeonBag.GetRelics());
        ResetRelicButtons();
        List<string> pageRelics = utility.GetCurrentPageStrings(relicPage, relicButtons, partyRelics);
        for (int i = 0; i < pageRelics.Count; i++)
        {
            relicButtons[i].EnableGO();
            relicSprites.ApplyToImage(relicButtons[i].GetImage(), pageRelics[i]);
        }
        // TODO Enable Change Page Buttons As Needed.
        int maxPage = utility.GetMaxPage(relicButtons.Count, partyRelics.Count);
        utility.DisableGameObjects(changeRelicPageObjects);
        if (relicPage > 0)
        {
            changeRelicPageObjects[0].SetActive(true);
        }
        if (relicPage < maxPage)
        {
            changeRelicPageObjects[1].SetActive(true);
        }
    }
    public ToolTipPopUp relicToolTip;
    public List<RelicButtonTooltip> relicButtons;
    public void ResetRelicButtons()
    {
        for (int i = 0; i < relicButtons.Count; i++)
        {
            relicButtons[i].DisableGO();
        }
    }
    [ContextMenu("Initialize Relic Buttons")]
    protected virtual void InitializeRelicButtons()
    {
        int gridWidth = relicButtons.Count;
        int offsetWidth = 1;
        float xPivot = 0f + (float)offsetWidth/(gridWidth + offsetWidth + offsetWidth - 1);
        float yPivot = 1f;
        for (int i = 0; i < gridWidth; i++)
        {
            relicButtons[i].SetToolTipIndex(i);
            relicButtons[i].anchor.pivot = new Vector2(xPivot, yPivot);
            xPivot += 1f/(gridWidth + offsetWidth + offsetWidth - 1);
        }
    }
    public void ClickRelicButton(int index)
    {
        int relicIndex = utility.GetPageIndex(index, relicPage, relicButtons.Count);
        // Get The Relic Clicked.
        string relicName = partyRelics[relicIndex];
        // Display The Name, Effect and Flavor.
        string[] relicDetails = relicDB.ReturnValue(relicName).Split("|");
        string relicDisplayText = relicName + "\n" + relicDetails[relicDetails.Length - 2] + "\n" + relicDetails[relicDetails.Length - 1];
        relicButtons[index].ShowTooltip(relicDisplayText);
    }
    public int relicPage = 0;
    public void ChangePage(bool right)
    {
        relicToolTip.HideTooltip();
        relicPage = utility.ChangePageV2(relicPage, right, relicButtons.Count, partyRelics.Count);
        UpdatePartyRelics();
    }
    public List<GameObject> changeRelicPageObjects;
    public TMP_Text goldText;
    public void UpdateGoldText()
    {
        goldText.text = partyData.inventory.GetGold().ToString();
    }
    public ArmoryUI armory;
    public GameObject armoryObject;
    public void ActivateArmory()
    {
        armory.SetPartyData(partyData);
        armoryObject.SetActive(true);
    }
    public MedicinePanel medicinePanel;
    public GameObject medicineObject;
    public void UseItems()
    {
        medicinePanel.SetPartyData(partyData);
        medicineObject.SetActive(true);
    }
    public UseSkillBookUI useSkillBook;
    public GameObject useSBObject;
    public void ActivateUseSkillBook()
    {
        useSkillBook.SetPartyData(partyData);
        useSBObject.SetActive(true);
    }
}
