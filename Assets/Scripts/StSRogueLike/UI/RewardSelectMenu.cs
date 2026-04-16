using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// For selecting a skillbook.
public class RewardSelectMenu : MonoBehaviour
{
    public PartyDataManager partyData;
    public StSRewardSaveData rewardData;
    public RewardDisplayMenu rewardMenu;
    public int selectedIndex = -1;
    public List<string> rewardOptions;
    public List<GameObject> skillDisplayObjects;
    public List<SkillDisplay> skillDisplays;
    public void SetRewardOptions(string newOptions)
    {
        rewardOptions = newOptions.Split("+").ToList();
        UpdateRewardOptions();
    }
    public void UpdateRewardOptions()
    {
        ResetSelection();
        for (int i = 0; i < Mathf.Min(rewardOptions.Count, skillDisplayObjects.Count); i++)
        {
            skillDisplayObjects[i].SetActive(true);
            string[] blocks = rewardOptions[i].Split("_");
            string rarity = rewardData.skillBookRarity.ReturnValue(blocks[1]);
            if (rarity == ""){continue;}
            skillDisplays[i].SetSkill(blocks[1], blocks[0], int.Parse(rewardData.skillBookRarity.ReturnValue(blocks[1])));
        }
    }
    protected void ResetSelection()
    {
        selectedIndex = -1;
        for (int i = 0; i < skillDisplayObjects.Count; i++)
        {
            skillDisplayObjects[i].SetActive(false);
        }
        UpdateSelectionDisplay();
    }
    protected void UpdateSelectionDisplay()
    {
        for (int i = 0; i < skillDisplays.Count; i++)
        {
            skillDisplays[i].SetHighlighted(i == selectedIndex);
        }
    }
    public void SelectReward(int index)
    {
        if (index < 0 || index >= rewardOptions.Count)
        {
            return;
        }
        selectedIndex = index;
        UpdateSelectionDisplay();
    }

    public void ConfirmSelectedReward()
    {
        if (selectedIndex < 0 || selectedIndex >= rewardOptions.Count)
        {
            return;
        }
        string[] blocks = rewardOptions[selectedIndex].Split("_");
        if (blocks.Length < 2)
        {
            return;
        }
        if (rewardMenu == null)
        {
            partyData.spellBook.GainBook(blocks[1]);
            return;
        }
        rewardMenu.ClaimSelectedSkillBook(blocks[1]);
        ResetSelection();
    }
    public void ClaimReward(int index)
    {
        string[] blocks = rewardOptions[index].Split("_");
        // Add the book to the party books.
        partyData.spellBook.GainBook(blocks[1]);
    }
    [ContextMenu("Test Reward Display")]
    public void TestDisplays()
    {
        rewardOptions = rewardData.GenerateSkillBookChoices();
        UpdateRewardOptions();
    }
    [ContextMenu("Test Rare Reward")]
    public void TestRareDisplays()
    {
        rewardOptions = rewardData.GenerateSkillBookChoices(3, true);
        UpdateRewardOptions();
    }
}
