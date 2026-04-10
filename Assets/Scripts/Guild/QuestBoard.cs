using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestBoard : MonoBehaviour
{
    public Dungeon dungeon;
    public SceneMover sceneMover;
    public int baseReward = 10;
    public int minQuests = 1;
    public List<string> availableQuests;
    // location | goal(s) | specific(s) | reward(s)
    public List<string> questDataBlocks;
    public SelectStatTextList questSelect;
    public TMP_Text questInfo;
    public List<string> requestOpeners;
    public GuildCard guildCard;
    public StatDatabase questLocations;
    public StatDatabase questGoals;

    void Start()
    {
        availableQuests = guildCard.availableQuests;
        if (guildCard.RefreshQuests())
        {
            GenerateQuests();
        }
        UpdateSelectableQuests();
    }

    protected void GenerateQuests()
    {
        availableQuests.Clear();
        string newQuestString = "";
        int newQuestDifficulty = 1;
        for (int i = 0; i < Mathf.Max(minQuests, guildCard.GetGuildRank()+1); i++)
        {
            newQuestString = "";
            newQuestDifficulty = Random.Range(1, guildCard.GetGuildRank()+1);
            // If the difficulty is too high, then minus 1 difficulty.
            while (!questLocations.KeyExists(newQuestDifficulty.ToString()) && newQuestDifficulty > 0)
            {
                newQuestDifficulty--;
            }
            // Get the goal.
            questDataBlocks = questGoals.ReturnValue(newQuestDifficulty.ToString()).Split("|").ToList();
            newQuestString += questDataBlocks[Random.Range(0, questDataBlocks.Count)]+"|";
            // Get the location.
            questDataBlocks = questLocations.ReturnValue(newQuestDifficulty.ToString()).Split("|").ToList();
            newQuestString += questDataBlocks[Random.Range(0, questDataBlocks.Count)]+"|";
            // Get the reward.
            newQuestString += Random.Range(newQuestDifficulty*baseReward, baseReward*(newQuestDifficulty+1)+1).ToString();
            availableQuests.Add(newQuestString);
        }
    }

    public void UpdateSelectableQuests()
    {
        List<string> goals = new List<string>();
        List<string> rewards = new List<string>();
        for (int i = 0; i < availableQuests.Count; i++)
        {
            string[] questDetails = availableQuests[i].Split("|");
            goals.Add(questDetails[0]);
            rewards.Add(questDetails[2]+" Gold");
        }
        questSelect.SetStatsAndData(goals, rewards);
    }

    public void ShowSelectedQuestDetails()
    {
        if (questSelect.GetSelected() < 0){return;}
        questInfo.text = requestOpeners[Random.Range(0, requestOpeners.Count)]+"\n";
        string[] questDetails = availableQuests[questSelect.GetSelected()].Split("|");
        questInfo.text += GoalDescriptionStart(questDetails[0]);
        questInfo.text += questDetails[1]+", ";
        questInfo.text += GoalDescriptionEnd(questDetails[0]);
    }

    protected string GoalDescriptionStart(string goal)
    {
        switch (goal)
        {
            case "Search":
            return "I lost my treasured item in ";
            case "Escort":
            return "I'm scared to go alone through ";
            case "Defeat":
            return "There are reports of a dangerous group in ";
            case "Rescue":
            return "Someone is missing! They were last seen in ";
        }
        return "";
    }

    protected string GoalDescriptionEnd(string goal)
    {
        switch (goal)
        {
            case "Search":
            return "please find it!";
            case "Escort":
            return "please accompany me.";
            case "Defeat":
            return "please get rid of them!";
            case "Rescue":
            return "please find them!";
        }
        return ".";
    }

    public void BeginQuest()
    {
        if (questSelect.GetSelected() < 0){return;}
        string[] questDetails = availableQuests[questSelect.GetSelected()].Split("|");
        dungeon.SetDungeonName(questDetails[1]);
        dungeon.SetQuestInfo(availableQuests[questSelect.GetSelected()]);
        dungeon.MakeDungeon();
        sceneMover.MoveToDungeon();
    }
}