using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// You Control The Party Leader.
// Other Party Members Are Adjustable AI (Select AI + Usable Skills/Spells).
// Enemy Are Nonadjustable AI.
public class MysteryDungeonBattleManager : MonoBehaviour
{
    public PartyDataManager partyData;
    public CharacterList playerParty;
    public MysteryDungeonBattleMap map;
    public AttackManager attackManager;
    public ActiveManager activeManager;
    // Add The Party To The Map Battling Actors.
    public void Initialize()
    {
        SpawnParty();
        // Need To Set Location Of Leader, Then Set Location Of Other Party Members Close To Leader On Valid Tiles.
    }
    protected void SpawnParty()
    {
        int partySizeCap = map.MapMaxPartyCapacity();
        // Spawn actors in patterns based on teams.
        List<TacticActor> actors = new List<TacticActor>();
        actors = map.actorMaker.SpawnTeam(0, playerParty.characters, playerParty.stats, playerParty.characterNames, playerParty.equipment, playerParty.characterIDs);
        map.actorMaker.ApplyBattleModifiers(actors, playerParty.GetBattleModifiers());
        for (int i = 0; i < Mathf.Min(partySizeCap, actors.Count); i++)
        {
            map.AddActorToBattle(actors[i]);
        }
    }
    protected void InitializeLocations()
    {
        
    }
    // TAKE PLAYER INPUT (END/MOVE/ATTACK/SKILL/ITEM/SPELL)
    // DO ALLY TURNS BASED ON SET AI (Stick Together/Go After Foes/Go Other Way/Explore)
    // DO ENEMY TURNS.
}
