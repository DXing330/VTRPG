using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DamageStatDisplayManager : MonoBehaviour
{
    public GeneralUtility utility;
    public BattleSimulatorState simulatorState;
    public int currentPage = 0;
    public void ChangePage(bool right = true)
    {
        currentPage = utility.ChangePage(currentPage, right, actorDisplayObjects, actorNames);
        UpdateCurrentPageDisplay();
    }
    public GameObject displayObject;
    // Bar graph sort of thing.
    // Y-axis is scaling measure of damage dealt and taken.
    // X-axis is actor sprites and names.
    public SpriteContainer actorSprites;
    public List<GameObject> actorDisplayObjects;
    public List<DamageStatDisplay> actorDisplays;
    public TMP_Text winRateText;
    public GameObject winRatioBar;
    public float winRatio;
    public int leftSideWins;
    public int totalWins;
    public void SetWinningTeam(List<int> winners)
    {
        // Change the ratio based on how much team 0 won.
        leftSideWins = 0;
        totalWins = winners.Count;
        for (int i = 0; i < winners.Count; i++)
        {
            if (winners[i] == 0)
            {
                leftSideWins++;
            }
        }
        if (totalWins <= 0)
        {
            winRatio = 0f;
            winRateText.text = "0%";
            winRatioBar.transform.localScale = new Vector3(winRatio, 1f, 0f);
            return;
        }
        winRatio = (float) leftSideWins / (float) totalWins;
        if (winRatio <= 0f)
        {
            winRateText.text = "0%";
        }
        else if (winRatio >= 1f)
        {
            winRateText.text = "100%";
        }
        else
        {
            winRateText.text = (winRatio * 100).ToString().Substring(0, 2)+"%";
        }
        winRatioBar.transform.localScale = new Vector3(winRatio, 1f, 0f);
    }
    public List<string> actorNames;
    public List<string> actorSpriteNames;
    public List<int> damageDealt;
    public int maxDamageDealt;
    public float ReturnDamageDealtProportion(int damage)
    {
        return (float)damage / (float)maxDamageDealt;
    }
    public List<int> damageTaken;
    public int maxDamageTaken;
    public float ReturnDamageTakenProportion(int damage)
    {
        return (float)damage / (float)maxDamageTaken;
    }
    public void ResetStatDisplay()
    {
        currentPage = 0;
        actorNames = new List<string>();
        actorSpriteNames = new List<string>();
        damageDealt = new List<int>();
        damageTaken = new List<int>();
        maxDamageDealt = 0;
        maxDamageTaken = 0;
    }

    public void InitializeDisplay(BattleStatsTracker damageTracker)
    {
        displayObject.SetActive(true);
        ResetStatDisplay();
        SetWinningTeam(damageTracker.winningTeams);
        actorNames = damageTracker.GetActorNames();
        actorSpriteNames = damageTracker.GetActorSprites();
        damageDealt = damageTracker.GetDamageDealt();
        damageTaken = damageTracker.GetDamageTaken();
        UpdateCurrentPageDisplay();
    }

    public void UpdateCurrentPageDisplay()
    {
        utility.DisableGameObjects(actorDisplayObjects);
        if (damageDealt.Count <= 0 || damageTaken.Count <= 0)
        {
            maxDamageDealt = 0;
            maxDamageTaken = 0;
        }
        else
        {
            maxDamageDealt = damageDealt.Max();
            maxDamageTaken = damageTaken.Max();
        }
        List<int> currentIndices = utility.GetCurrentPageIndices(currentPage, actorDisplayObjects, actorNames);
        for (int i = 0; i < currentIndices.Count; i++)
        {
            int index = currentIndices[i];
            actorDisplayObjects[i].SetActive(true);
            actorDisplays[i].UpdateDisplay(actorSprites.SpriteDictionary(actorSpriteNames[index]), actorNames[index], damageDealt[index], damageTaken[index], ReturnDamageDealtProportion(damageDealt[index]), ReturnDamageTakenProportion(damageTaken[index]));
        }
    }

    public int testStatCount = 6;
    [ContextMenu("Test Display Damage")]
    public void TestDisplay()
    {
        ResetStatDisplay();
        // Generate random names and sprite names.
        for (int i = 0; i < testStatCount; i++)
        {
            string randomName = actorSprites.RandomSpriteName();
            actorNames.Add(randomName);
            actorSpriteNames.Add(randomName);
            damageDealt.Add(Random.Range(1, 100));
            damageTaken.Add(Random.Range(1, 100));
        }
        UpdateCurrentPageDisplay();
    }
}
