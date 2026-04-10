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
    public List<string> rewardOptions;
    public List<SkillDisplay> skillDisplays;
    public void SetRewardOptions(string newOptions)
    {
        rewardOptions = newOptions.Split("+").ToList();
        UpdateRewardOptions();
    }
    public void UpdateRewardOptions()
    {
        for (int i = 0; i < rewardOptions.Count; i++)
        {
            string[] blocks = rewardOptions[i].Split("_");
            string rarity = rewardData.skillBookRarity.ReturnValue(blocks[1]);
            if (rarity == ""){continue;}
            skillDisplays[i].SetSkill(blocks[1], blocks[0], int.Parse(rewardData.skillBookRarity.ReturnValue(blocks[1])));
        }
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
