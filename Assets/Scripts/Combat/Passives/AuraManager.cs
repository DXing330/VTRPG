using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuraManager : MonoBehaviour
{
    public GeneralUtility utility;
    public BattleMap map;
    public List<TacticActor> ReturnActorsInAura(AuraEffect aura)
    {
        List<TacticActor> actorsInAura = new List<TacticActor>();
        for (int i = 0; i < map.battlingActors.Count; i++)
        {
            if (aura.ActorInAura(map.battlingActors[i], map))
            {
                actorsInAura.Add(map.battlingActors[i]);
            }
        }
        return actorsInAura;
    }
    public void ApplyAuraMapEffect(AuraEffect aura)
    {
        Debug.Log(aura.GetEffect());
        Debug.Log(aura.GetEffectSpecifics());
        switch (aura.GetEffect())
        {
            // Tiles/TEffects
            default:
            List<int> auraTiles = aura.GetAuraTiles(map);
            for (int i = 0; i < auraTiles.Count; i++)
            {
                map.ChangeTile(auraTiles[i], aura.GetEffect(), aura.GetEffectSpecifics());
            }
            break;
            case "Time":
            map.SetTime(aura.GetEffectSpecifics());
            break;
            case "Weather":
            map.SetWeather(aura.GetEffectSpecifics());
            break;
        }
    }
    public PassiveSkill passive;
    public string GetAuraSpecifics(AuraEffect aura)
    {
        int eSpecifics = utility.SafeParseInt(passive.GetEffectSpecifics(aura.actor, aura.effectSpecifics), 0);
        if (eSpecifics <= 0)
        {
            return aura.effectSpecifics;
        }
        return eSpecifics.ToString();
    }
    public bool SpecialAuraEffect(TacticActor auraTarget, AuraEffect aura)
    {
        switch (aura.GetTarget())
        {
            default:
            return false;
            case "Attack":
            if (auraTarget == null){return true;}
            // Battle Manager Attack, Don't Consume Actions.
            map.battleManager.PublicAAA(aura.actor, auraTarget, false);
            return true;
            // Affect the map directly.
            case "Map":
            ApplyAuraMapEffect(aura);
            return true;
            case "RitualSummon":
            if (auraTarget == null)
            {
                map.combatLog.UpdateNewestLog("Ritual summoning failed!");
                return true;
            }
            map.combatLog.UpdateNewestLog(auraTarget.GetPersonalName() + " is consumed by the ritual.");
            auraTarget.MarkSacrificed();
            auraTarget.SetCurrentHealth(-1);
            map.battlingActors.Remove(auraTarget);
            map.battleManager.SpawnAndAddActor(auraTarget.GetLocation(), aura.GetEffect(), auraTarget.GetTeam());
            map.combatLog.UpdateNewestLog("By offering " + auraTarget.GetPersonalName() + " a " + aura.GetEffect() + " is summoned.");
            return true;
        }
    }
    protected void TriggerAuraEffect(AuraEffect aura, TacticActor actor, string triggerType)
    {
        if (!aura.TriggerAura(triggerType)){return;}
        if (passive.CheckAuraCondition(aura, actor, map))
        {
            map.combatLog.UpdateNewestLog(actor.GetPersonalName() + " is affected by " + aura.GetAuraName() + ".");
            map.combatLog.AddDetailedLogs(map.detailViewer.ReturnAuraDetails(aura));
            // Check if the effect is special.
            if (SpecialAuraEffect(actor, aura))
            {
                aura.ActorTriggersAura(actor);
                return;
            }
            passive.AffectActor(actor, aura.effect, GetAuraSpecifics(aura));
            aura.ActorTriggersAura(actor);
        }
    }
    protected void TriggerAuraEffects(List<AuraEffect> allAura, TacticActor actor, string triggerType)
    {
        for (int i = 0; i < allAura.Count; i++)
        {
            TriggerAuraEffect(allAura[i], actor, triggerType);
        }
    }
    public void TriggerAllAuraEffects(List<AuraEffect> allAura, TacticActor triggeringActor, string triggerType)
    {
        TriggerAuraEffects(allAura, triggeringActor, triggerType);
    }
    public void TriggerRemoveAuraEffect(AuraEffect aura)
    {
        if (!aura.TriggerAura("RemoveAura")){return;}
        List<TacticActor> triggeringActors = ReturnActorsInAura(aura);
        // Check for special effects, which don't need an actor, they can change the map directly.
        if (aura.GetTarget() == "Map" && SpecialAuraEffect(null, aura))
        {
            return;
        }
        if (triggeringActors.Count <= 0 && aura.GetTarget() == "RitualSummon")
        {
            SpecialAuraEffect(null, aura);
            return;
        }
        // Otherwise iterate through the actors.
        for (int i = 0; i < triggeringActors.Count; i++)
        {
            TriggerAuraEffect(aura, triggeringActors[i], "RemoveAura");
        }
    }
}
