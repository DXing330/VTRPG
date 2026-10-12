using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EffectSpecificsCalculator", menuName = "ScriptableObjects/BattleLogic/EffectSpecificsCalculator", order = 1)]
public class EffectSpecificsCalculator : ScriptableObject
{
    public StatDatabase allPassiveNames;
    public string GetEffectSpecificsFromPassiveName(TacticActor actor, string passiveName, int input = 0, BattleMap map = null)
    {
        // Check If The Actor Has The Passive.
        //if (!actor.PassiveExists(passiveName)){return "0";}
        // Get The Data Of The Passive Somehow?
        string[] passiveData = allPassiveNames.ReturnValue(passiveName).Split("|");
        if (passiveData.Length < 6){return "0";}
        int totalSpecifics = 0;
        string[] effects = passiveData[4].Split(",");
        string[] effectSpecifics = passiveData[5].Split(",");
        if (effects.Length != effectSpecifics.Length){return "0";}
        for (int i = 0; i < effectSpecifics.Length; i++)
        {
            string specificDetails = GetEffectSpecifics(actor, effectSpecifics[i], input, map);
            // Any Non-Int Value Is Ignored.
            if (!int.TryParse(specificDetails, out int specificValue))
            {
                continue;
            }
            totalSpecifics += specificValue;
        }
        return totalSpecifics.ToString();
    }
    public string GetEffectSpecifics(TacticActor actor, string specifics, int input = 0, BattleMap map = null, TacticActor otherActor = null)
    {
        // Some are scaling.
        if (specifics.Contains("ScalingEquals"))
        {
            TacticActor scalingActor = actor;
            string[] scaling = specifics.Split("Equals");
            int amount = 0;
            string scalingSpecifics = scaling[1];
            if (scalingSpecifics.EndsWith("A") || scalingSpecifics.EndsWith("D"))
            {
                scalingSpecifics = scalingSpecifics[..^1];
                if (otherActor == null)
                {
                    return "0";
                }
                scalingActor = otherActor;
            }
            if (scalingSpecifics.StartsWith("Meta"))
            {
                string passiveName = scalingSpecifics.Replace("Meta", "");
                amount = int.Parse(GetEffectSpecificsFromPassiveName(scalingActor, passiveName, input, map));
            }
            else
            {
                switch (scalingSpecifics)
                {
                    // Scaled based on the passive level. 
                    default:
                    amount = scalingActor.GetLevelFromPassive(scalingSpecifics);
                    break;
                    case "DamageDealt":
                    case "DamageTaken":
                    amount = input;
                    break;
                    case "Defense":
                    amount = scalingActor.GetDefense();
                    break;
                    case "Attack":
                    amount = scalingActor.GetAttack();
                    break;
                    case "Actions":
                    amount = scalingActor.GetActions();
                    break;
                    case "RemainingActions":
                    amount = scalingActor.ReturnPreviousRoundRemainingActions();
                    break;
                    case "SkillsUsed":
                    amount = scalingActor.ReturnTotalRoundSkills();
                    break;
                    case "TempSkillCount":
                    amount = scalingActor.GetTempActives().Count;
                    break;
                    case "PassiveSkillCount":
                    amount = scalingActor.GetPassiveSkills().Count;
                    break;
                    case "Attacks":
                    amount = scalingActor.ReturnTotalRoundAttacks();
                    break;
                    case "Defends":
                    amount = scalingActor.ReturnTotalRoundDefends();
                    break;
                    case "Moves":
                    amount = scalingActor.ReturnTotalRoundMoves();
                    break;
                    case "RemainingMovement":
                    amount = scalingActor.ReturnPreviousRoundRemainingMovement();
                    break;
                    case "RoundAttacks":
                    amount = scalingActor.ReturnCurrentRoundAttacks();
                    break;
                    case "MissingHealthPercent":
                    amount = scalingActor.GetMissingHealthPercent();
                    break;
                    case "AdjacentEnemies":
                    if (map == null){return "0";}
                    amount = map.GetAdjacentEnemies(scalingActor).Count;
                    break;
                    case "AdjacentAllies":
                    if (map == null){return "0";}
                    amount = map.GetAdjacentAllies(scalingActor).Count;
                    break;
                    case "TotalAllyDeploys":
                    if (map == null){return "0";}
                    List<TacticActor> allies = map.AllAllies(scalingActor);
                    for (int i = 0; i < allies.Count; i++)
                    {
                        amount += allies[i].GetDeployCount();
                    }
                    break;
                    case "TotalAllySkillsUsed":
                    if (map == null){return "0";}
                    List<TacticActor> skillAllies = map.AllAllies(scalingActor);
                    for (int i = 0; i < skillAllies.Count; i++)
                    {
                        amount += skillAllies[i].ReturnTotalRoundSkills();
                    }
                    break;
                    case "Round":
                    amount = map.GetRound();
                    break;
                }
            }
            // Some scaling includes a multiplier.
            string operation = "";
            int coefficent = 1;
            if (scaling.Length > 3)
            {
                operation = scaling[2];
                string scalingCoefficent = scaling[3];
                if (scalingCoefficent.StartsWith("Meta"))
                {
                    string scalingPassiveName = scalingCoefficent.Replace("Meta", "");
                    coefficent = int.Parse(GetEffectSpecificsFromPassiveName(scalingActor, scalingPassiveName, input, map));
                }
                else if (scalingCoefficent.StartsWith("Base"))
                {
                    string basePassiveName = scalingCoefficent.Replace("Base", "");
                    coefficent = scalingActor.GetLevelFromPassive(basePassiveName);
                }
                else
                {
                    coefficent = int.Parse(scalingCoefficent);
                }
            }
            else
            {
                return amount.ToString();
            }
            switch (operation)
            {
                default:
                return amount.ToString();
                case "MultiBy":
                return (amount * coefficent).ToString();
                case "DivideBy":
                if (coefficent == 0){return amount.ToString();}
                return (Mathf.Max(1, amount / coefficent)).ToString();
                case "AddTo":
                return (amount + coefficent).ToString();
                case "RationalFunc":
                if (amount + coefficent <= 0){return "-1";}
                return ((100 * amount) / (amount + coefficent)).ToString();
                case "UpTo":
                return (Mathf.Min(coefficent, amount)).ToString();
            }
        }
        return specifics;
    }
}
