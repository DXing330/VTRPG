using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Skills", menuName = "ScriptableObjects/DataContainers/SkillData", order = 1)]
public class SkillDatabase : StatDatabase
{
    public List<string> timings;
    public List<string> conditions;
    public List<string> conditionSpecifics;
    public List<string> targets;
    public List<string> effects;
    public List<string> effectSpecifics;
}
