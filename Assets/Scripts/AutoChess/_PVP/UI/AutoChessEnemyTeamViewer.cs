using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AutoChessEnemyTeamViewer : MonoBehaviour
{
    public GeneralUtility utility;
    public AutoChessPVPDataManager enemyTeams;
    public AutoChessPrepUIManager UI;
    public List<GameObject> enemyTeamTextObjects;
    public List<ButtonText> enemyTeamTexts;
    public void UpdateViewDetails()
    {
        utility.DisableGameObjects(enemyTeamTextObjects);
        int index = 0;
        for (int i = 1; i < enemyTeams.dataManagers.Count; i++)
        {
            int health = enemyTeams.dataManagers[i].GetHealth();
            if (health <= 0)
            {
                continue;
            }
            enemyTeamTextObjects[index].SetActive(true);
            enemyTeamTexts[index].UpdateText($"{enemyTeams.dataManagers[i].tactician.GetTactician()} ({enemyTeams.dataManagers[i].GetHealth()})");
            index++;
        }
    }
    public void ViewEnemyTeam(int index)
    {
        // Only 8 Teams, Team 0 Is The Player.
        if (index == 0 || index >= 8){return;}
        UI.UpdateMapWithDataManager(enemyTeams.dataManagers[index]);
    }
}
