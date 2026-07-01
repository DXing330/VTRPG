using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBattleManager : MonoBehaviour
{
    public AutoChessPrepManager prepManager;
    public AutoChessEnemyDataManager enemyData;
    public BattleMap map;
    public ActorMaker actorMaker;
    public EffectManager effectManager;
    public AttackManager attackManager;
    public ActiveManager activeManager;
    public MoveCostManager moveManager;
    // Turn Lifecycle.
        // Attacking + Moving.
    // End Battle Management.
    public List<string> enemyPool;
    public void StartBattle()
    {
        // Make Actors For Each Player Unit.
        // Apply Faction Effects (Maybe Handled By Other Manager)?
        // Spawn The First Wave Of Enemies.
        // Start The First Round.
    }
    public void SpawnPhase()
    {
        // for each spawn zone, spawn a random enemy from the pool
    }
    public void StartRound()
    {
        // 
    }
    public void StartTurn()
    {
        // Split Between Player Turn And Enemy Turn.
    }
    public void EndTurn()
    {
        // Apply EndTurn Effects.
    }
    public void EndRound()
    {
        // Spawn Next Wave.
    }
    public void EnemyTurn()
    {
        // Move/Attack/Skill.
    }
    public void PlayerTurn()
    {
        // Attack/Skill.
    }
}
