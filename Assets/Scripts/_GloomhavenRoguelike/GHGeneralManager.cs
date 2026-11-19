using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles Battle Scene Flow + Coordinates Data Managers.
public class GHGeneralManager : MonoBehaviour
{
    public GHBattleManager battleManager;
    public GHBattleMap map;
    public GHBattleState battleState;
    public GHPlayerState playerState;
    void Start()
    {
        // Deck Loads Itself On Start.
        // Load The Map + Battle.
        battleState.Load();
        playerState.Load();
        // Spawn Player + Enemies.
    }
    
}
