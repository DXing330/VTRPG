using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

// Later you can capture enemies and train against them.
public class EnemyListTester : MonoBehaviour
{
    public GeneralUtility utility;
    public List<string> trainingEnemies;
    public int index;
    public TMP_Text selectedEnemy;
    public CharacterList enemyList;

    public void ChangeSelectedEnemy(bool right)
    {
        index = utility.ChangeIndex(index, right, trainingEnemies.Count - 1);
        string sEnemy = trainingEnemies[index];
        selectedEnemy.text = sEnemy;
        enemyList.ResetLists();
        enemyList.AddCharacter(sEnemy);
    }
}
