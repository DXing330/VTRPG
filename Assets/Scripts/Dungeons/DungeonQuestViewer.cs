using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DungeonQuestViewer : MonoBehaviour
{
    public bool outsideDungeon = false;
    public GeneralUtility utility;
    public GuildCard guildCard;
    public Dungeon dungeon;
    public List<int> questIndices;
    public TMP_Text questLocation;
    public TMP_Text questGoal;
    public TMP_Text questFloor;
    public TMP_Text questReward;
    public TMP_Text questCounter;
    public int selectedQuest = 0;
    void Start()
    {
        RefreshData();
    }
    public void RefreshData()
    {
        selectedQuest = 0;
        if (outsideDungeon)
        {
            questIndices.Clear();
            for (int i = 0; i < guildCard.GetQuestLocations().Count; i++)
            {
                questIndices.Add(i);
            }
        }
        else
        {
            questIndices = guildCard.GetQuestIndicesAtLocation(dungeon.GetDungeonName());
        }
        UpdateSelectedQuest();
    }
    protected void ResetText()
    {
        questLocation.text = "";
        questGoal.text = "";
        questFloor.text = "";
        questReward.text = "";
        questCounter.text = "0/0";
    }
    protected void UpdateSelectedQuest()
    {
        ResetText();
        if (questIndices.Count <= 0){return;}
        questLocation.text = guildCard.GetQuestLocations()[questIndices[selectedQuest]];
        questReward.text = guildCard.GetQuestRewards()[questIndices[selectedQuest]].ToString();
        questGoal.text = guildCard.GetQuestGoals()[questIndices[selectedQuest]];
        questFloor.text = guildCard.GetQuestFloors()[questIndices[selectedQuest]].ToString();
        questCounter.text = (selectedQuest + 1) + "/" + questIndices.Count;
    }
    public void ChangeIndex(bool right)
    {
        selectedQuest = utility.ChangeIndex(selectedQuest, right, questIndices.Count - 1);
        UpdateSelectedQuest();
    }
}
