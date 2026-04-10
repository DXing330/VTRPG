using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardDisplayPanel : MonoBehaviour
{
    public SpriteContainer rewardSprites;
    public string reward; // Gold/Relic/Item/Equipment/Skillbook
    public string rewardSpecifics;
    public void SetRewardAndSpecifics(string newReward, string newSpecifics)
    {
        reward = newReward;
        rewardSpecifics = newSpecifics;
        UpdateRewardDisplay();
    }
    public Image rewardImage;
    public TMP_Text rewardText;
    public void UpdateRewardDisplay()
    {
        rewardImage.sprite = rewardSprites.SpriteDictionary(reward);
        switch (reward)
        {
            // Gold Shows Gold Amount;
            default:
            rewardText.text = rewardSpecifics;
            break;
            case "Gold":
            rewardText.text = reward + "(" + rewardSpecifics + ")";
            break;
            case "Skillbook":
            rewardText.text = "Choose Skillbook";
            break;
        }
    }
}
