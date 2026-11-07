using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MysteryDungeonItemDetailSwitch", menuName = "ScriptableObjects/MysteryDungeon/MysteryDungeonItemDetailSwitch", order = 1)]
public class MysteryDungeonItemDetailSwitch : ScriptableObject
{
    public PassiveDetailViewerSwitch passiveDetails;
    public StatDatabase itemData;
    public string ReturnItemDescriptionFromName(string itemName)
    {
        string[] itemStats = itemData.ReturnValue(itemName).Split("|");
        if (itemStats.Length < 3){return "";}
        string target = itemStats[0];
        string effect = itemStats[1];
        string specifics = itemStats[2];
        return ReturnItemDescription(target, effect, specifics);
    }
    public string ReturnItemDescription(string target, string effect, string specifics)
    {
        if (target.StartsWith("Move"))
        {
            return MoveItemDescription(target, effect, specifics);
        }
        switch (target)
        {
            // User/UserTeam
            default:
            return passiveDetails.PassiveEffect(effect, specifics, target);
            case "Held":
            return passiveDetails.PassiveEffect(effect, specifics, "User") + " while held";
            case "UserParty":
            return passiveDetails.PassiveEffect(effect, specifics, "User") + " for the remainder of the dungeon";
            case "UserTeam":
            return passiveDetails.PassiveEffect(effect, specifics, "User's Team");
            case "UserRoom":
            return passiveDetails.PassiveEffect(effect, specifics, "Units In The User's Room");
            case "EnemyClosest":
            return passiveDetails.PassiveEffect(effect, specifics, "Closest Enemy");
            case "EnemyLine":
            return passiveDetails.PassiveEffect(effect, specifics, "Closest Enemy In A Straight Line");
            case "EnemyFront":
            return passiveDetails.PassiveEffect(effect, specifics, "Enemy In Front");
            case "EnemyRoom":
            return passiveDetails.PassiveEffect(effect, specifics, "Enemies In The Same Room");
            case "MapReveal":
            switch (specifics)
            {
                case "All":
                    return "Reveal the entire map.";
                case "Enemy":
                    return "Reveal enemies on the map.";
                case "Items":
                    return "Reveal items on the map.";
                case "Traps":
                    return "Reveal traps on the map.";
                case "Stairs":
                    return "Reveal the stairs on the map.";
            }
            break;
            case "Map":
            return MapItemDescription(effect, specifics);
        }
        return "???";
    }
    public string MoveItemDescription(string target, string effect, string specifics)
    {
        string targetDescription = MoveItemTargetDescription(target);
        switch (effect)
        {
            case "Warp":
                return "Warp " + targetDescription + " to a random location.";
            case "WarpStairs":
                return "Warp " + targetDescription + " to the stairs.";
            case "Push":
                return "Push " + targetDescription + "up to " + specifics + " tiles away ";
            case "Forward":
                return "Move " + targetDescription + "up to " + specifics + " tiles forward ";
            case "WarpLeader":
                return "Warp " + targetDescription + " to the leader.";
            case "Hurl":
                return "Hurl " + targetDescription + " towards the nearest ally or wall, dealing damage.";
        }
        return "???";
    }
    protected string MoveItemTargetDescription(string target)
    {
        switch (target)
        {
            case "Move":
            return "the User";
            case "MoveFront":
            return "the Unit In Front";
            case "MoveTeam":
            return "Teammates";
            case "MoveEnemyRoom":
            return "Enemies in the Room";
        }
        return "???";
    }
    public string MapItemDescription(string effect, string specifics)
    {
        switch (effect)
        {
            case "Remove":
                return "Remove all " + specifics + " tiles from the map.";
            case "RemoveFront":
                return "Remove " + specifics + " tiles in front of the user.";
            case "Escape":
                return "Escape from the dungeon.";
            case "Weather":
                return "Change the weather to " + specifics + ".";
            case "Itemize":
                return "Turn the closest enemy into an item.";
            case "OneRoom":
                return "Turn the entire floor into one room.";
            case "RemoveTraps":
                switch (specifics)
                {
                    case "All":
                        return "Remove all traps from the map.";
                    default:
                        return "Remove " + specifics + " traps from the map.";
                }
            case "AddTrap":
                switch (specifics)
                {
                    case "Random":
                        return "Place a random trap on the map.";
                    default:
                        return "Place " + specifics + " on the map.";
                }
            case "Trawl":
                return "Pull items on the map toward the user.";
            case "MonsterHouse":
                return "Create a Monster House on the floor.";
        }
        return "???";
    }
}
