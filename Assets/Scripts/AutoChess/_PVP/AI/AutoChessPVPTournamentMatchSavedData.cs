using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SavedMatches", menuName = "ScriptableObjects/PVPAI/SavedMatches", order = 1)]
public class AutoChessPVPTournamentMatchSavedData : SavedData
{
    public List<string> matches = new();
    public override void NewGame()
    {
        matches.Clear();
        dataList = matches;
        Save();
    }
    public void AddMatch(List<GenomeEntry> rankedChampions)
    {
        string match = string.Join(",", rankedChampions.Select(c =>
            $"{c.generation}:{c.elo:F1}"
        ));
        matches.Add(match);
        dataList = matches;
        Save();
    }
    public override void LoadStat(string data)
    {
        if (string.IsNullOrEmpty(data)){return;}
        matches.Add(data);
    }
    public void LoadHistory()
    {
        matches.Clear();
        Load();
        dataList = matches;
    }
    public void ClearHistory()
    {
        matches.Clear();
        dataList = matches;
        Save();
    }
}
