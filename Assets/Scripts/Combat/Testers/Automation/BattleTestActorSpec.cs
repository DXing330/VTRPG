using System;

[Serializable]
public class BattleTestActorSpec
{
    public string spriteName;
    public string personalName;
    public string statsOverride;
    public string equipment;
    public string id;

    public string DisplayName(int index)
    {
        if (!string.IsNullOrEmpty(personalName))
        {
            return personalName;
        }
        if (!string.IsNullOrEmpty(spriteName))
        {
            return spriteName + " " + (index + 1);
        }
        return "Actor " + (index + 1);
    }

    public string ActorId(int index)
    {
        if (!string.IsNullOrEmpty(id))
        {
            return id;
        }
        return (index + 1).ToString();
    }
}
