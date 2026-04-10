using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shows event/battle/shop rewards.
public class RewardDisplayMenu : MonoBehaviour
{
    public GeneralUtility utility;
    public PartyDataManager partyData;
    public StSRewardSaveData rewardData;
    void Start()
    {
        rewardData.Load();
        LoadFromData();
        UpdateRewardDisplay();
    }
    protected void LoadFromData()
    {
        rewards = rewardData.rewards;
        rewardSpecifics = rewardData.rewardSpecifics;
    }
    public List<GameObject> rewardButtons;
    public List<RewardDisplayPanel> rewardPanels;
    [ContextMenu("Test Reward Display + Generation")]
    public void TestGenerateRewards()
    {
        rewardData.GenerateRewards();
        LoadFromData();
        UpdateRewardDisplay();
    }
    public void UpdateRewardDisplay()
    {
        utility.DisableGameObjects(rewardButtons);
        for (int i = 0; i < rewards.Count; i++)
        {
            rewardButtons[i].SetActive(true);
            rewardPanels[i].SetRewardAndSpecifics(rewards[i], rewardSpecifics[i]);
        }
    }
    public List<string> rewards; // Gold/Relic/Item/Equipment/Skillbook
    public List<string> rewardSpecifics; // Amount/Specifics/Specifics/Specifics/Specifics[,]
    public GameObject selectMenuObject;
    public RewardSelectMenu selectMenu;
    public int selectedIndex;
    public void SelectIndex(int index)
    {
        selectedIndex = index;
        switch (rewards[selectedIndex])
        {
            case "Gold":
            // Gain The Gold.
            partyData.inventory.GainGold(int.Parse(rewardSpecifics[index]));
            ClaimRewardAtIndex();
            break;
            case "Item":
            partyData.inventory.AddItemQuantity(rewardSpecifics[index]);
            ClaimRewardAtIndex();
            break;
            case "Skillbook":
            // Show The Skillbook Select.
            selectMenuObject.SetActive(true);
            selectMenu.SetRewardOptions(rewardSpecifics[index]);
            break;
            case "Equipment":
            break;
            case "Relic":
            break;
        }
    }
    public void ClaimRewardAtIndex()
    {
        if (selectedIndex < 0 || selectedIndex >= rewards.Count){return;}
        selectMenuObject.SetActive(false);
        rewards.RemoveAt(selectedIndex);
        rewardSpecifics.RemoveAt(selectedIndex);
        UpdateRewardDisplay();
        selectedIndex = -1;
    }
}
