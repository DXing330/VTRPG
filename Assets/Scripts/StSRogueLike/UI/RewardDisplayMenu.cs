using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Shows event/battle/shop rewards.
public class RewardDisplayMenu : MonoBehaviour
{
    public GeneralUtility utility;
    public StSStateManager stsManager;
    public PartyDataManager partyData;
    public StSRewardSaveData rewardData;
    // Some are called inside other scenes, but mainly it is from the battle rewards.
    public bool battleRewards = true;
    void Start()
    {
        if (battleRewards)
        {
            rewardData.Load();
            LoadFromData();
        }
        UpdateRewardDisplay();
        selectMenu.SetPartyData(partyData);
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
        int displayCount = Mathf.Min(rewards.Count, rewardButtons.Count, rewardPanels.Count);
        for (int i = 0; i < displayCount; i++)
        {
            rewardButtons[i].SetActive(true);
            rewardPanels[i].SetRewardAndSpecifics(rewards[i], rewardSpecifics[i]);
        }
    }
    public List<string> rewards; // Gold/Relic/Item/Equipment/Skillbook
    // Some things will manually set the rewards.
    public void SetRewards(List<string> newRewards)
    {
        rewards = newRewards;
    }
    public List<string> rewardSpecifics; // Amount/Specifics/Specifics/Specifics/Specifics[,]
    public void SetRewardSpecifics(List<string> newRewards)
    {
        rewardSpecifics = newRewards;
    }
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
            // TODO Implement Gaining Relics.
            // Picking a relic from this menu shouldn't popup another relic to select?
            // Only relics from shops/bosses should have a popup for another reward menu.
            case "Relic":
            string relicName = rewardSpecifics[index];
            rewardData.GainRelic(relicName, partyData, stsManager);
            ClaimRewardAtIndex();
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
    public void ClaimSelectedSkillBook(string skillBookName)
    {
        partyData.spellBook.GainBook(skillBookName);
        ClaimRewardAtIndex();
        UpdateRewardDisplay();
    }
}
