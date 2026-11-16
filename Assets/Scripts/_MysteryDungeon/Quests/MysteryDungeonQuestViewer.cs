using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class MysteryDungeonQuestViewer : MonoBehaviour
{
    public TMP_Text locationText;
    public TMP_Text questText;
    public TMP_Text rewardText;
    public void ResetView()
    {
        locationText.text = "";
        questText.text = "";
        rewardText.text = "";
    }
    public void UpdateView(MysteryDungeonQuest newQuest)
    {
        locationText.text = "Location: " + newQuest.questLocation;
        questText.text = "Quest: " + newQuest.questGoal + " " + newQuest.goalSpecifics;
        rewardText.text = "Reward: " + newQuest.reward + " X " + newQuest.rewardQuantity;
    }
}
