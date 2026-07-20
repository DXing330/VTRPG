using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AutoChessBattleUIManager : MonoBehaviour
{
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
        enemyDisplay.UpdateDisplay();
        CheckEndGame();
        combatLog.ActivateCombatLogLarge();
    }
    public void CheckEndGame()
    {
        endGameUIObject.SetActive(false);
        bool finalRound = prepManager.dataManager.FinalRound();
        int health = prepManager.dataManager.GetHealth();
        if (health <= 0)
        {
            endGameUIObject.SetActive(true);
            endGameText.text = "Defeat...";
            endGameText.color = Color.red;
        }
        if (finalRound)
        {
            endGameUIObject.SetActive(true);
            endGameText.text = "Victory!";
            endGameText.color = Color.green;
        }
    }
}
