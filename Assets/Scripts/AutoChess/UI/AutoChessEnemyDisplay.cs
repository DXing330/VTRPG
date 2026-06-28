using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessEnemyDisplay : MonoBehaviour
{
    public AutoChessEnemyDataManager enemyData;
    public List<AutoChessEnemySlot> enemySlots;
    public List<string> enemies;
    public List<int> enemyCount;
    void Start()
    {
        UpdateDisplay();
    }
    protected void CountEnemy(string enemyName)
    {
        int indexOf = enemies.IndexOf(enemyName);
        if (indexOf < 0)
        {
            enemies.Add(enemyName);
            enemyCount.Add(1);
            return;
        }
        enemyCount[indexOf]++;
    }
    public void UpdateDisplay()
    {
        enemies.Clear();
        enemyCount.Clear();
        for (int i = 0; i < enemySlots.Count; i++)
        {
            enemySlots[i].ResetSlot();
        }
        List<string> allEnemies = enemyData.GetNextRoundEnemies();
        for (int i = 0; i < allEnemies.Count; i++)
        {
            CountEnemy(allEnemies[i]);
        }
        for (int i = 0; i < enemies.Count; i++)
        {
            enemySlots[i].UpdateSlot(enemies[i], enemyCount[i]);
        }
    }
}
