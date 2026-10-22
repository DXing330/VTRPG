using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MysteryDungeonStatCardList : MonoBehaviour
{
    public GeneralUtility utility;
    public List<MysteryDungeonStatCard> statCards;
    protected void DisableStatCards()
    {
        for (int i = 0; i < statCards.Count; i++)
        {
            statCards[i].DisableStatCard();
        }
    }
    public List<RectTransform> statCardRects;
    public RectTransformAdjustor adjustor;
    [ContextMenu("Initialize Stat Card Placements")]
    public void InitializeStatCardPlaces()
    {
        adjustor.SetRectTilesToColumn(statCardRects);
    }
    public void UpdateTeamActors(List<TacticActor> actors, int team = 0)
    {
        DisableStatCards();
        int index = 0;
        for (int i = 0; i < actors.Count; i++)
        {
            if (index >= statCards.Count){break;}
            if (actors[i].GetTeam() == team)
            {
                statCards[index].UpdateStatCard(actors[i]);
                index++;
            }
        }
    }
}
