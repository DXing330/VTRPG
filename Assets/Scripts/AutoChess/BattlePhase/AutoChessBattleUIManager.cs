using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Later Add Another Helper For Meta End Game Stuff.
public class AutoChessBattleUIManager : MonoBehaviour
{
    public bool PVP = false;
    public AutoChessPVPMatchDirector PVPMatchDirector;
    public AutoChessPrepManager prepManager;
    public AutoChessEnemyDisplay enemyDisplay;
    public GeneralUtility utility;
    public List<GameObject> nonBattleUI;
    public List<GameObject> defaultNonBattleUI;
    public GameObject endGameUIObject;
    public TMP_Text endGameText;
    public RectTransform mapRect;
    public Vector3 startBattleSize;
    public Vector3 endBattleSize;
    // Show The Combat Log At The End Of Battle
    public CombatLogLarge combatLog;
    public void StartBattle()
    {
        utility.DisableGameObjects(nonBattleUI);
        mapRect.localScale = startBattleSize;
    }
    public void EndBattle()
    {
        utility.EnableGameObjects(defaultNonBattleUI);
        mapRect.localScale = endBattleSize;
        prepManager.UpdateAllUI();
        if (enemyDisplay != null)
        {
            enemyDisplay.UpdateDisplay();
        }
        CheckEndGame();
        combatLog.ActivateCombatLogLarge();
    }
    // If Too Much, Move These To A Separate Helper Later.
    public Inventory inventory;
    public void WinGameReward()
    {
        int round = prepManager.dataManager.GetRound();
        inventory.Load();
        inventory.GainGold(round * 2);
        inventory.Save();
    }
    public void LoseGameReward()
    {
        int round = prepManager.dataManager.GetRound();
        inventory.Load();
        inventory.GainGold(round / 2);
        inventory.Save();
    }
    public void PVPPlacementReward(int placement)
    {
        inventory.Load();
        inventory.GainGold((8 - placement) * 4);
        inventory.Save();
    }
    public void CheckEndGame()
    {
        endGameUIObject.SetActive(false);
        int health = prepManager.dataManager.GetHealth();
        if (PVP)
        {
            int placement = PVPMatchDirector.GetPlayerPlacement();
            if (health <= 0)
            {
                PVPPlacementReward(placement);
                endGameUIObject.SetActive(true);
                endGameText.text = $"Defeat...";
                endGameText.color = Color.red;
            }
            if (PVPMatchDirector.matchOver)
            {
                PVPPlacementReward(placement);
                endGameUIObject.SetActive(true);
                endGameText.text = "Victory!";
                endGameText.color = Color.green;
            }
            return;
        }
        bool finalRound = prepManager.dataManager.FinalRound();
        if (health <= 0)
        {
            endGameUIObject.SetActive(true);
            endGameText.text = "Defeat...";
            endGameText.color = Color.red;
            LoseGameReward();
        }
        if (finalRound)
        {
            endGameUIObject.SetActive(true);
            endGameText.text = "Victory!";
            endGameText.color = Color.green;
            WinGameReward();
        }
    }
}
