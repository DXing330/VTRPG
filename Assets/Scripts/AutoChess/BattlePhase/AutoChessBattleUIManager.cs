using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoChessBattleUIManager : MonoBehaviour
{
    public AutoChessPrepManager prepManager;
    public GeneralUtility utility;
    public List<GameObject> nonBattleUI;
    public List<GameObject> defaultNonBattleUI;
    public RectTransform mapRect;
    public Vector3 startBattleSize;
    public Vector3 endBattleSize;
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
    }
}
