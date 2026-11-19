using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Applys Effects + Targeting For Items.
// Probably Reuse This For Traps As Well.
[CreateAssetMenu(fileName = "MysteryDungeonItemEffect", menuName = "ScriptableObjects/MysteryDungeon/MysteryDungeonItemEffect", order = 1)]
public class MysteryDungeonItemEffect : PassiveSkill
{
    public StatDatabase MDItemData;
    public void ThrowItem(TacticActor thrownTo, MysteryDungeonBattleMap map, string itemName, int itemIndex, PartyDataManager partyData)
    {
        // Check If The Item Is Throwable (Usable When Thrown).
        string[] itemData = MDItemData.ReturnValue(itemName).Split("|");
        if (itemData.Length < 5){return;}
        bool throwable = int.Parse(itemData[4]) == 1;
        // Remove The Item From Begin Equipped If Applicable.
        int oldHolderID = partyData.dungeonBag.UnequipItemAtIndex(itemIndex);
        if (oldHolderID >= 0)
        {
            UnequipItem(map.GetActorByID(oldHolderID), map, itemName, partyData);
        }
        // If Throwable, The Actor Thrown To AutoUses.
        if (throwable)
        {
            // TESTED 10/26/26
            UseItem(thrownTo, map, itemName, itemIndex, partyData);
        }
        // Else If Possible The Actor Picks It Up.
        else if (thrownTo.GetDungeonHeldItem() == "")
        {
            // TESTED 10/26/26
            thrownTo.SetDungeonHeldItem(itemName);
            if (thrownTo.GetTeam() == 0)
            {
                partyData.dungeonBag.EquipItemAtIndex(itemIndex, thrownTo);
            }
            else
            {
                partyData.dungeonBag.RemoveItemAt(itemIndex);
            }
        }
        // Else It Drops To The Tile
        else
        {
            map.AddItemToLocation(itemName, thrownTo.GetLocation());
            partyData.dungeonBag.RemoveItemAt(itemIndex);
        }
    }
    protected void ApplyTargetEffectSpecifics(TacticActor user, MysteryDungeonBattleMap map, PartyDataManager partyData, string target, string effect, string specifics)
    {
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
            case "Map":
            MapItems(user, map, target, effect, specifics, partyData);
            break;
        }
    }
    public void UseItem(TacticActor user, MysteryDungeonBattleMap map, string itemName, int itemIndex, PartyDataManager partyData)
    {
        int oldHolderID = partyData.dungeonBag.UnequipItemAtIndex(itemIndex);
        if (oldHolderID >= 0)
        {
            UnequipItem(map.GetActorByID(oldHolderID), map, itemName, partyData);
        }
        if (itemName.StartsWith("TM"))
        {
            UseTM(user, map, itemName, partyData);
            partyData.dungeonBag.RemoveItemAt(itemIndex);
            return;
        }
        string[] itemData = MDItemData.ReturnValue(itemName).Split("|");
        if (itemData.Length < 5){return;}
        string target = itemData[0];
        string effect = itemData[1];
        string specifics = itemData[2];
        bool consumable = int.Parse(itemData[3]) == 1;
        // If Edible Then Increase Fullness.
        if (consumable && effect != "Stomach")
        {
            user.Eat(10);
        }
        if (target == "Held")
        {
            // Equipped Items Are Not Removed From Inventory.
            EquipItem(user, map, itemName, itemIndex, effect, specifics, partyData, true);
            return;
        }
        ApplyTargetEffectSpecifics(user, map, partyData, target, effect, specifics);
        partyData.dungeonBag.RemoveItemAt(itemIndex);
    }
    protected void UnequipItem(TacticActor user, MysteryDungeonBattleMap map, string itemName, PartyDataManager partyData)
    {
        if (string.IsNullOrEmpty(itemName) || user == null){return;}
        // Put The Item Back In The Bag.
        partyData.dungeonBag.UnequipItem(itemName, user);
        user.ResetDungeonHeldItem();
        string[] itemData = MDItemData.ReturnValue(itemName).Split("|");
        if (itemData.Length < 5){return;}
        string target = itemData[0];
        if (target != "Held"){return;}
        string effect = itemData[1];
        string specifics = itemData[2];
        // Undo The Effect/Specifics Somehow.
        AntiAffectActor(user, effect, specifics);
    }
    public void ApplyHeldItemEffect(TacticActor user, MysteryDungeonBattleMap map)
    {
        string currentHeld = user.GetDungeonHeldItem();
        if (string.IsNullOrEmpty(currentHeld)){return;}
        string[] itemData = MDItemData.ReturnValue(currentHeld).Split("|");
        if (itemData.Length < 5){return;}
        AffectActor(user, itemData[1], itemData[2]);
    }
    // Previously Bug Out With Multiple Items Of Same Name Equipped To Multiple Users, TOTEST.
    public void EquipItem(TacticActor user, MysteryDungeonBattleMap map, string itemName, int itemIndex, PartyDataManager partyData)
    {
        string[] itemData = MDItemData.ReturnValue(itemName).Split("|");
        if (itemData.Length < 5){return;}
        string target = itemData[0];
        string effect = itemData[1];
        string specifics = itemData[2];
        bool held = false;
        if (target == "Held"){held = true;}
        EquipItem(user, map, itemName, itemIndex, effect, specifics, partyData, held);
    }
    protected void EquipItem(TacticActor user, MysteryDungeonBattleMap map, string itemName, int itemIndex, string effect, string specifics, PartyDataManager partyData, bool held = true)
    {
        // Add The Current Held Item Back To Inventory + Lose Its Effects.
        string currentHeld = user.GetDungeonHeldItem();
        UnequipItem(user, map, currentHeld, partyData);
        // Equip The New Held Item + Apply Effects.
        user.SetDungeonHeldItem(itemName);
        if (held)
        {
            AffectActor(user, effect, specifics);
        }
        int oldHolderID = partyData.dungeonBag.EquipItemAtIndex(itemIndex, user);
        if (oldHolderID < 0 || oldHolderID == user.GetID()){return;}
        // Unequip The Item From The Old Holder.
        UnequipItem(map.GetActorByID(oldHolderID), map, itemName, partyData);
    }
    protected void UseTM(TacticActor user, MysteryDungeonBattleMap map, string TMName, PartyDataManager partyData)
    {
        string[] TMData = TMName.Split("-");
        if (TMData.Length < 2){return;}
        UserItems(user, map, "UserParty", "Skill", TMData[1], partyData);
    }
    protected void UserItems(TacticActor user, MysteryDungeonBattleMap map, string target, string effect, string specifics, PartyDataManager partyData)
    {
        List<TacticActor> targets = new List<TacticActor>();
        List<int> tiles = new List<int>();
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
            case "UserTeam":
            targets = map.AllAllies(user);
            break;
            case "UserRoom":
            tiles = map.GetRoomTiles(user.GetLocation());
            targets = map.GetActorsOnTiles(tiles);
            break;
        }
        if (targets.Count > 0)
        {
            ApplyEffectToTargets(targets, map, effect, specifics);
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
            case "EnemyLine":
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
            case "EnemyRoom":
            tiles = map.GetRoomTiles(user.GetLocation());
            targets = map.ReturnEnemiesInTiles(user, tiles, true);
            break;
            case "EnemyFloor":
            targets = map.AllEnemies(user);
            break;
        }
        if (targets.Count > 0)
        {
            ApplyEffectToTargets(targets, map, effect, specifics);
        }
    }
    protected void MovementItems(TacticActor user, MysteryDungeonBattleMap map, string target, string effect, string specifics, PartyDataManager partyData)
    {
        List<TacticActor> targets = new List<TacticActor>();
        List<int> tiles = new List<int>();
        switch (target)
        {
            default:
            map.MoveEffect(user, effect, specifics);
            break;
            case "MoveFront":
            map.MoveEffect(map.GetActorOnTile(map.mapUtility.PointInDirection(user.GetLocation(), user.GetDirection(), map.mapSize)), effect, specifics, user);
            break;
            case "MoveTeam":
            targets = map.AllAllies(user);
            targets.Remove(user);
            break;
            case "MoveRoom":
            tiles = map.GetRoomTiles(user.GetLocation());
            targets = map.GetActorsOnTiles(tiles);
            break;
        }
        if (targets.Count > 0)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                map.MoveEffect(targets[i], effect, specifics, user);
            }
        }
    }
    protected void MapItems(TacticActor user, MysteryDungeonBattleMap map, string target, string effect, string specifics, PartyDataManager partyData)
    {
        map.MDChangeMap(user, effect, specifics);
    }
    public StatDatabase MDTrapData;
    public void TriggerTrap(TacticActor triggerer, MysteryDungeonBattleMap map, PartyDataManager partyData, string trapName)
    {
        string[] trapData = MDTrapData.ReturnValue(trapName).Split("|");
        if (trapData.Length < 3){return;}
        string target = trapData[0];
        string effect = trapData[1];
        string specifics = trapData[2];
        ApplyTargetEffectSpecifics(triggerer, map, partyData, target, effect, specifics);
    }
}