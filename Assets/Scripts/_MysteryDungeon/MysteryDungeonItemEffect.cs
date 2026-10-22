using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Applys Effects + Targeting For Items.
[CreateAssetMenu(fileName = "MysteryDungeonItemEffect", menuName = "ScriptableObjects/MysteryDungeon/MysteryDungeonItemEffect", order = 1)]
public class MysteryDungeonItemEffect : PassiveSkill
{
    public StatDatabase MDItemData;
    public void UseItem(TacticActor user, MysteryDungeonBattleMap map, string itemName, PartyDataManager partyData)
    {
        string[] itemData = MDItemData.ReturnValue(itemName).Split("|");
        if (itemData.Length < 3){return;}
        string target = itemData[0];
        string effect = itemData[1];
        string specifics = itemData[2];
        if (target.StartsWith("User"))
        {
            UserItems(user, map, target, effect, specifics, partyData);
            return;
        }
        else if (target.StartsWith("Enemy"))
        {
            EnemyItems(user, map, target, effect, specifics, partyData);
            return;
        }
        else if (target.StartsWith("Move"))
        {
            MovementItems(user, map, target, effect, specifics, partyData);
            return;
        }
        switch (target)
        {
            case "MapReveal":
            map.Reveal(specifics);
            break;
        }
    }
    protected void UserItems(TacticActor user, MysteryDungeonBattleMap map, string target, string effect, string specifics, PartyDataManager partyData)
    {
        switch (target)
        {
            default:
            AffectActor(user, effect, specifics);
            break;
            case "UserParty":
            AffectActor(user, effect, specifics);
            // Apply The Permanent Change To The Party Stats.
            partyData.ApplyEffectToPartyID(effect, specifics, "1", user.GetID());
            break;
        }
    }
    protected void EnemyItems(TacticActor user, MysteryDungeonBattleMap map, string target, string effect, string specifics, PartyDataManager partyData)
    {
        List<TacticActor> targets = new List<TacticActor>();
        List<int> tiles = new List<int>();
        switch (target)
        {
            case "EnemyClosest":
            AffectActor(map.MDMapUtility.GetClosestEnemy(user, map), effect, specifics);
            return;
            case "EnemyFront":
            tiles.Add(map.ReturnTileInRelativeDirection(user, 0));
            targets = map.ReturnEnemiesInTiles(user, tiles, true);
            break;
            case "LineEnemy":
            TacticActor firstLineTarget = null;
            tiles = map.mapUtility.GetTilesInLineDirection(user.GetLocation(), user.GetDirection(), map.mapSize, map.mapSize);
            for (int i = 0; i < tiles.Count; i++)
            {
                firstLineTarget = map.GetActorOnTile(tiles[i]);
                if (firstLineTarget != null && firstLineTarget.GetTeam() != user.GetTeam())
                {
                    AffectActor(firstLineTarget, effect, specifics);
                    return;
                }
            }
            return;
        }
        if (targets.Count > 0)
        {
            ApplyEffectToTargets(targets, map, effect, specifics);
        }
    }
    protected void MovementItems(TacticActor user, MysteryDungeonBattleMap map, string target, string effect, string specifics, PartyDataManager partyData)
    {
        switch (target)
        {
            default:
            map.MoveEffect(user, effect, specifics);
            break;
        }
    }
}
