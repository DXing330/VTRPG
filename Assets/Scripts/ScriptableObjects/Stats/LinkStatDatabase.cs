using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Linking 2+ StatDatabase, can return value from any of them.
[CreateAssetMenu(fileName = "LinkStatDatabase", menuName = "ScriptableObjects/DataContainers/LinkStatDatabase", order = 1)]
public class LinkStatDatabase : StatDatabase
{
    public List<StatDatabase> linkedDBs;
    public override string ReturnValue(string key)
    {
        string returnedValue = "";
        for (int i = 0; i < linkedDBs.Count; i++)
        {
            returnedValue = linkedDBs[i].ReturnValue(key);
            // Stop after first value found.
            if (returnedValue != ""){break;}
        }
        return returnedValue;
    }
}
