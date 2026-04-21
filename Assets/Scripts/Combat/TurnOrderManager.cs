using System.Collections.Generic;
using UnityEngine;

public class TurnOrderManager : MonoBehaviour
{
    // Snapshot of actors eligible to act this round; BattleMap.battlingActors remains the live roster.
    public List<TacticActor> roundTurnOrder = new List<TacticActor>();

    public List<TacticActor> GetRoundTurnOrder(){return roundTurnOrder;}

    public void ClearRoundOrder()
    {
        roundTurnOrder.Clear();
    }

    public void StartRound(List<TacticActor> battlingActors, InitiativeTracker initiativeTracker)
    {
        if (battlingActors == null)
        {
            roundTurnOrder = new List<TacticActor>();
            return;
        }

        roundTurnOrder = new List<TacticActor>(battlingActors);
        if (initiativeTracker != null)
        {
            roundTurnOrder = initiativeTracker.SortActors(roundTurnOrder);
        }
    }

    public bool ValidTurnActor(TacticActor actor, BattleMap map)
    {
        return actor != null
            && map != null
            && actor.GetHealth() > 0
            && map.battlingActors.Contains(actor);
    }

    public TacticActor GetActor(int turnNumber)
    {
        if (turnNumber < 0 || turnNumber >= roundTurnOrder.Count){return null;}
        return roundTurnOrder[turnNumber];
    }

    public bool TryFindNextValidTurn(BattleMap map, ref int turnNumber)
    {
        for (int i = turnNumber; i < roundTurnOrder.Count; i++)
        {
            if (!ValidTurnActor(roundTurnOrder[i], map)){continue;}
            turnNumber = i;
            return true;
        }
        return false;
    }

    protected int FirstFutureTurnIndex(TacticActor actor, int currentTurn)
    {
        for (int i = currentTurn + 1; i < roundTurnOrder.Count; i++)
        {
            if (roundTurnOrder[i] == actor){return i;}
        }
        return -1;
    }

    public bool CanGrantExtraTurn(TacticActor actor, BattleMap map, int currentTurn)
    {
        return ValidTurnActor(actor, map)
            && FirstFutureTurnIndex(actor, currentTurn) < 0;
    }

    public bool GrantExtraTurnNext(TacticActor actor, BattleMap map, int currentTurn)
    {
        if (!CanGrantExtraTurn(actor, map, currentTurn)){return false;}
        int insertIndex = Mathf.Clamp(currentTurn + 1, 0, roundTurnOrder.Count);
        roundTurnOrder.Insert(insertIndex, actor);
        return true;
    }

    public bool GrantExtraTurnAtEnd(TacticActor actor, BattleMap map, int currentTurn)
    {
        if (!CanGrantExtraTurn(actor, map, currentTurn)){return false;}
        roundTurnOrder.Add(actor);
        return true;
    }

    public bool PullFutureTurnNext(TacticActor actor, BattleMap map, int currentTurn)
    {
        if (!ValidTurnActor(actor, map)){return false;}
        int index = FirstFutureTurnIndex(actor, currentTurn);
        if (index < 0){return false;}
        if (index == currentTurn + 1){return true;}

        roundTurnOrder.RemoveAt(index);
        roundTurnOrder.Insert(currentTurn + 1, actor);
        return true;
    }

    public bool PushFutureTurnToEnd(TacticActor actor, BattleMap map, int currentTurn)
    {
        if (!ValidTurnActor(actor, map)){return false;}
        int index = FirstFutureTurnIndex(actor, currentTurn);
        if (index < 0){return false;}
        if (index == roundTurnOrder.Count - 1){return true;}

        roundTurnOrder.RemoveAt(index);
        roundTurnOrder.Add(actor);
        return true;
    }
}
