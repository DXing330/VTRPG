using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyDataManager : MonoBehaviour
{
    void Start()
    {
        SetFullParty();
    }
    // For out of combat party changes.
    public GeneralUtility utility;
    public SkillEffect partyEffects;
    // This is the one that the battle will actually read.
    public StatDatabase actorStats;
    public TacticActor dummyActor;
    public StatDatabase standardAttributes;
    public CharacterList fullParty;
    public List<PartyData> allParties;
    // For player + familiar.
    public PartyData permanentPartyData;
    // For hirelings + allies.
    public PartyData mainPartyData;
    // For quest party members (rescue/escort/etc)
    public PartyData tempPartyData;
    public List<SavedData> otherPartyData;
    public Inventory inventory;
    public EquipmentInventory equipmentInventory;
    public DungeonBag dungeonBag;
    public GuildCard guildCard;
    public SpellBook spellBook;

    public void Save()
    {
        for (int i = 0; i < allParties.Count; i++) { allParties[i].Save(); }
        for (int i = 0; i < otherPartyData.Count; i++) { otherPartyData[i].Save(); }
        SetFullParty();
    }

    [ContextMenu("Debug Load")]
    public void Load()
    {
        for (int i = 0; i < allParties.Count; i++) { allParties[i].Load(); }
        for (int i = 0; i < otherPartyData.Count; i++) { otherPartyData[i].Load(); }
        SetFullParty();
    }

    public void NewGame()
    {
        for (int i = 0; i < allParties.Count; i++) { allParties[i].NewGame(); }
        for (int i = 0; i < otherPartyData.Count; i++) { otherPartyData[i].NewGame(); }
    }

    // Since when starting a new run, the partydata is already set but you want to reset equipment and other stuff.
    public void OtherDataNewName()
    {
        for (int i = 0; i < otherPartyData.Count; i++) { otherPartyData[i].NewGame(); }
    }

    public virtual void NewDay(int dayCount)
    {
        for (int i = 0; i < otherPartyData.Count; i++) { otherPartyData[i].NewDay(dayCount); }
        // Pay the failure penalty for any failed quests.
        int penalty = guildCard.GetFailPenalty();
        if (penalty > 0)
        {
            if (inventory.EnoughGold(penalty))
            {
                inventory.SpendGold(penalty);
            }
            else
            {
                inventory.LoseGold();
            }
        }
        // Add exhaustion to all party members.
        for (int i = 0; i < allParties.Count; i++)
        {
            for (int j = allParties[i].PartyCount() - 1; j >= 0; j--)
            {
                allParties[i].Exhaust(j, i != 0);
            }
        }
        // People might die or get injured after a new day.
        // Or they might rest and heal if we're optimistic.
        SetFullParty();
    }

    public virtual void AddHours(int hours)
    {
        for (int i = 0; i < otherPartyData.Count; i++) { otherPartyData[i].AddHours(hours); }
    }

    public void RemoveExhaustion()
    {
        for (int i = 0; i < allParties.Count; i++)
        {
            for (int j = allParties[i].PartyCount() - 1; j >= 0; j--)
            {
                allParties[i].RemoveExhaustion(j);
            }
        }
    }

    public virtual void Rest()
    {
        for (int i = 0; i < otherPartyData.Count; i++) { otherPartyData[i].Rest(); }
    }

    public void NaturalRegeneration(List<string> regenPassives)
    {
        for (int i = 0; i < allParties.Count; i++)
        {
            allParties[i].NaturalRegeneration(regenPassives);
        }
    }

    public bool DungeonHunger()
    {
        bool permStarved = false;
        // Subtract 1 health from everyone.
        for (int i = 0; i < allParties.Count; i++)
        {
            for (int j = allParties[i].PartyCount() - 1; j >= 0; j--)
            {
                permStarved = allParties[i].HungerChipDamage(j, i != 0);
                if (permStarved){return true;}
            }
        }
        // Remove dead party members.
        SetFullParty();
        return false;
    }

    public bool StatusDamage(List<string> damagingStatuses)
    {
        bool permanentPartyDeath = false;
        // Subtract 1 health from everyone with certain statuses.
        for (int i = 0; i < allParties.Count; i++)
        {
            permanentPartyDeath = allParties[i].StatusChipDamage(damagingStatuses, i != 0);
            // This can only be true if i == 0
            if (permanentPartyDeath)
            {
                return true;
            }
        }
        // Remove dead party members.
        SetFullParty();
        return false;
    }

    public bool PartyMemberClassExists(string spriteName)
    {
        if (permanentPartyData.MemberExists(spriteName) || mainPartyData.MemberExists(spriteName)){ return true; }
        return false;
    }

    public void AddTempPartyMember(string name)
    {
        int nextID = guildCard.GetNextID();
        guildCard.IncrementNextID();
        // Don't need stats, just grab base stats.
        tempPartyData.AddMember(actorStats.ReturnValue(name), name, nextID.ToString());
        SetFullParty();
    }

    public bool TempPartyMemberExists(string name)
    {
        return tempPartyData.MemberExists(name);
    }

    public void RemoveTempPartyMember(string name)
    {
        tempPartyData.RemoveMember(name);
        SetFullParty();
    }

    public void RemoveAllTempPartyMember(string name)
    {
        for (int i = 0; i < tempPartyData.PartyCount(); i++)
        {
            tempPartyData.RemoveMember(name);
        }
        SetFullParty();
    }

    public bool OpenSlots()
    {
        // Default is 2 party members, plus 2 permanent for the classic 4?
        return mainPartyData.PartyCount() < guildCard.GetGuildRank() + guildCard.GetPartyLimit();
    }

    public void ForceNewGameData(string newData)
    {
        NewGame();
        string[] blocks = newData.Split("#");
        string[] personalNames = blocks[0].Split("|");
        string[] spriteNames = blocks[1].Split("|");
        for (int i = 0; i < personalNames.Length; i++)
        {
            HireMember(actorStats.ReturnValue(spriteNames[i]), personalNames[i]);
        }
    }

    public void HireMemberBySpriteName(string spriteName)
    {
        HireMember(actorStats.ReturnValue(spriteName), spriteName);
    }

    // Add Random Attributes Here.
    public void HireMember(string stats, string personalName)
    {
        int nextID = guildCard.GetNextID();
        guildCard.IncrementNextID();
        // Dummy Actor Loads Stats And Checks If Attributes Are Already Assigned.
        dummyActor.SetInitialStatsFromString(stats);
        dummyActor.InitializeAttributes(standardAttributes);
        stats = dummyActor.GetInitialStats();
        mainPartyData.AddMember(stats, personalName, nextID.ToString());
        SetFullParty();
    }

    public string EquipToPartyMember(string equip, int index, Equipment dummy)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return "";}
        return section.EquipToMember(equip, localIndex, dummy);
    }

    public string UnequipFromPartyMember(int index, string slot, Equipment dummy)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return "";}
        return section.UnequipFromMember(localIndex, slot, dummy);
    }

    public int ReturnPartyMemberIDFromIndex(int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return -1;}
        return section.GetIDAtIndex(localIndex);
    }

    public void SetPartyMemberEquipFromIndex(string equip, int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return;}
        section.SetEquipmentAtIndex(equip, localIndex);
    }

    public string ReturnPartyMemberEquipFromIndex(int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return "";}
        return section.GetEquipmentAtIndex(localIndex);
    }
    public int ReturnPartyMemberCurrentHealthFromIndex(int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return -1;}
        return section.GetCurrentHealthAtIndex(localIndex);
    }
    public string ReturnMainPartyEquipment(int selected)
    {
        return mainPartyData.partyEquipment[selected];
    }
    public int ReturnTotalPartyCount()
    {
        int count = 0;
        count += permanentPartyData.PartyCount();
        count += mainPartyData.PartyCount();
        count += tempPartyData.PartyCount();
        return count;
    }
    public TacticActor ReturnActorFromID(int id)
    {
        TacticActor actor = permanentPartyData.ReturnActorFromID(id);
        if (actor == null)
        {
            actor = mainPartyData.ReturnActorFromID(id);
        }
        if (actor == null)
        {
            actor = tempPartyData.ReturnActorFromID(id);
        }
        return actor;
    }
    public int ReturnIDAtIndex(int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return -1;}
        return section.GetIDAtIndex(localIndex);
    }
    // All Party Functions Should Use This To Determine Which Section Of The Party To Edit And What Index To Edit.
    public (PartyData section, int localIndex) ResolvePartyIndex(int index)
    {
        if (index < 0)
        {
            return (null, -1);
        }
        int permanentCount = permanentPartyData.PartyCount();
        int mainCount = mainPartyData.PartyCount();
        int tempCount = tempPartyData.PartyCount();
        if (index < permanentCount)
        {
            return (permanentPartyData, index);
        }
        else if (index < permanentCount + mainCount)
        {
            return (mainPartyData, index - permanentCount);
        }
        else if (index < permanentCount + mainCount + tempCount)
        {
            return (tempPartyData, index - permanentCount - mainCount);
        }
        return (null, -1);
    }
    public TacticActor ReturnActorAtIndex(int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return null;}
        return section.ReturnActorAtIndex(localIndex);
    }
    public void RenamePartyMember(string newInfo, int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return;}
        section.ChangeName(newInfo, localIndex);
        SetFullParty();
    }
    public void ApplyEffectToPartyMember(string effect, string specifics, string power, int index)
    {
        if (index < 0){return;}
        TacticActor partyMember = ReturnActorAtIndex(index);
        partyEffects.AffectActor(partyMember, effect, specifics, utility.SafeParseInt(power, 1));
        UpdatePartyMember(partyMember, index);
    }
    public void ApplyEffectToParty(string effect, string specifics, string power = "1", bool fullParty = true, int memberIndex = -1)
    {
        if (!fullParty)
        {
            ApplyEffectToPartyMember(effect, specifics, power, memberIndex);
            return;
        }
        int partyCount = ReturnTotalPartyCount();
        for (int i = 0; i < partyCount; i++)
        {
            ApplyEffectToPartyMember(effect, specifics, power, i);
        }
    }
    public void UpdatePartyMember(TacticActor dummyActor, int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return;}
        section.SetMemberStats(dummyActor, localIndex);
        SetFullParty();
    }

    public void RemovePartyMember(int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return;}
        section.RemoveStatsAtIndex(localIndex);
        SetFullParty();
    }

    public void HealParty(bool full = true)
    {
        if (full)
        {
            permanentPartyData.ResetCurrentStats();
            mainPartyData.ResetCurrentStats();
            tempPartyData.ResetCurrentStats();
        }
        else
        {
            permanentPartyData.HalfRestore();
            mainPartyData.HalfRestore();
            tempPartyData.HalfRestore();
        }
        SetFullParty();
    }

    public void UpdatePartyAfterBattle(List<int> IDs, List<string> stats)
    {
        for (int i = 0; i < allParties.Count; i++)
        {
            // Assume everyone dies at the end of every battle.
            allParties[i].ResetDefeatedMemberTracker();
        }
        // Match each code/spritename to an index.
        List<int> allIndices = new List<int>();
        // Remove the index after it have been taken.
        List<int> allPossibleIndices = new List<int>();
        for (int i = 0; i < ReturnTotalPartyCount(); i++)
        {
            allIndices.Add(-1);
            allPossibleIndices.Add(i);
        }
        for (int i = 0; i < IDs.Count; i++)
        {
            for (int j = allPossibleIndices.Count - 1; j >= 0; j--)
            {
                if (MatchID(IDs[i], allPossibleIndices[j]))
                {
                    allIndices[i] = allPossibleIndices[j];
                    allPossibleIndices.RemoveAt(j);
                    break;
                }
            }
        }
        for (int i = 0; i < Mathf.Min(ReturnTotalPartyCount(), IDs.Count); i++)
        {
            Debug.Log(allIndices[i]);
            UpdatePartyMemberAfterBattle(stats[i], allIndices[i]);
        }
        // Permanent Parties Members Survive With 1 HP, Main Character Power.
        permanentPartyData.ReviveDefeatedMembers();
        mainPartyData.RemoveDefeatedMembers();
        tempPartyData.RemoveDefeatedMembers();
        SetFullParty();
    }

    public void RemoveDeadPartyMembers()
    {
        mainPartyData.RemoveDeadMembers();
        tempPartyData.RemoveDeadMembers();
    }

    public string ReturnPartyMemberStatsAtIndex(int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return "";}
        return section.GetMemberStatsAtIndex(localIndex);
    }

    protected string CodeNameAtIndex(int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return "";}
        return section.GetNameAtIndex(localIndex);
    }

    protected string SpriteNameAtIndex(int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return "";}
        return section.GetSpriteNameAtIndex(localIndex);
    }

    protected bool MatchID(int ID, int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return false;}
        return (section.GetIDAtIndex(localIndex) == ID);
    }

    protected void UpdatePartyMemberAfterBattle(string stats, int index)
    {
        (PartyData section, int localIndex) = ResolvePartyIndex(index);
        if (section == null){return;}
        section.SetCurrentStats(stats, localIndex);
    }

    public void PartyDefeated()
    {
        fullParty.ResetLists();
        permanentPartyData.ResetCurrentStats(true);
        mainPartyData.ClearAllStats();
        tempPartyData.ClearAllStats();
        Save();
        SetFullParty();
    }

    public List<string> GetAllPartyNames()
    {
        List<string> names = new List<string>();
        names.AddRange(permanentPartyData.GetNames());
        names.AddRange(mainPartyData.GetNames());
        names.AddRange(tempPartyData.GetNames());
        return names;
    }

    [ContextMenu("SetParty")]
    public void SetFullParty()
    {
        fullParty.ResetCharacters();
        fullParty.AddToParty(permanentPartyData.GetNames(), permanentPartyData.GetStats(), permanentPartyData.GetSpriteNames(), permanentPartyData.GetEquipmentStats(), permanentPartyData.GetPartyIDs());
        fullParty.AddToParty(mainPartyData.GetNames(), mainPartyData.GetStats(), mainPartyData.GetSpriteNames(), mainPartyData.GetEquipmentStats(), mainPartyData.GetPartyIDs());
        fullParty.AddToParty(tempPartyData.GetNames(), tempPartyData.GetStats(), tempPartyData.GetSpriteNames(), tempPartyData.GetEquipmentStats(), tempPartyData.GetPartyIDs());
    }
}
