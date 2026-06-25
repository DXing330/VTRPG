using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AutoChessEnemyDataManager", menuName = "ScriptableObjects/AutoChess/AutoChessEnemyDataManager", order = 1)]
public class AutoChessEnemyDataManager : SavedData
{
    public StatDatabase enemyData;
    public StatDatabase enemyGroups;
    public StatDatabase enemyDifficulty;
}
