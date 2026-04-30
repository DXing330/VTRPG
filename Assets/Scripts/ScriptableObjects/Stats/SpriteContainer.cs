using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
    using UnityEditor;
#endif


[CreateAssetMenu(fileName = "SpriteContainer", menuName = "ScriptableObjects/DataContainers/SpriteContainer", order = 1)]
public class SpriteContainer : ScriptableObject
{
    public bool copySprites = false;
    public SpriteContainer copiedSprites;
    public bool searchLinkedSpriteContainers = false;
    public List<SpriteContainer> linkedSpriteContainers;
    public GeneralUtility utility;
    public ColorDictionary colors;
    public List<Sprite> sprites;
    public bool defaultsEnabled;
    public string defaultKey;
    public Sprite defaultSprite;
    public string allKeysAndValues;
    public virtual void SetAllData(string newInfo)
    {
        allKeysAndValues = newInfo;
    }
    public string delimiter;
    public string delimiterTwo;
    public virtual void Initialize()
    {
        string[] blocks = allKeysAndValues.Split(delimiter);
        keys = blocks[0].Split(delimiterTwo).ToList();
        values = blocks[1].Split(delimiterTwo).ToList();
        if (blocks.Length > 2)
        {
            colorNames = blocks[2].Split(delimiterTwo).ToList();
        }
        else
        {
            colorNames = new List<string>();
        }
        if (blocks.Length > 3)
        {
            sizes = blocks[3].Split(delimiterTwo).ToList();
        }
        else
        {
            sizes = new List<string>();
        }
        while (colorNames.Count < keys.Count)
        {
            colorNames.Add("None");
        }
        while (sizes.Count < keys.Count)
        {
            sizes.Add("1");
        }
        if (colorNames.Count > keys.Count)
        {
            colorNames = colorNames.GetRange(0, keys.Count);
        }
        if (sizes.Count > keys.Count)
        {
            sizes = sizes.GetRange(0, keys.Count);
        }
        if (copySprites)
        {
            keys = new List<string>(copiedSprites.keys);
            values = new List<string>(copiedSprites.values);
            colorNames = new List<string>(copiedSprites.colorNames);
            sizes = new List<string>(copiedSprites.sizes);
            sprites = copiedSprites.sprites;
        }
        #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
        #endif
    }
    public List<string> keys;
    public List<string> colorNames;
    public List<string> sizes;
    public string RandomSpriteName()
    {
        return sprites[Random.Range(0, sprites.Count)].name;
    }
    public List<string> values;

    public string SpriteNameByIndex(int index)
    {
        if (index < 0 || index >= sprites.Count){return "";}
        return sprites[index].name;
    }

    bool SearchLinkedContainers()
    {
        return searchLinkedSpriteContainers && linkedSpriteContainers != null;
    }

    Sprite SpriteByNameLocal(string spriteName)
    {
        if (sprites == null){return null;}
        for (int i = 0; i < sprites.Count; i++)
        {
            if (sprites[i] != null && sprites[i].name == spriteName){return sprites[i];}
        }
        return null;
    }

    string LocalSpriteName(string spriteName)
    {
        if (keys != null && values != null)
        {
            int indexOf = keys.IndexOf(spriteName);
            if (indexOf >= 0 && indexOf < values.Count)
            {
                return values[indexOf];
            }
        }
        return spriteName;
    }

    Sprite SpriteDictionaryFromLinkedContainers(string spriteName)
    {
        if (!SearchLinkedContainers()){return null;}
        for (int i = 0; i < linkedSpriteContainers.Count; i++)
        {
            if (linkedSpriteContainers[i] == null){continue;}
            Sprite linkedSprite = linkedSpriteContainers[i].SpriteByNameLocal(spriteName);
            if (linkedSprite != null){return linkedSprite;}
        }
        return null;
    }

    public Sprite SpriteDictionary(string spriteName)
    {
        spriteName = LocalSpriteName(spriteName);
        Sprite sprite = SpriteByNameLocal(spriteName);
        if (sprite != null){return sprite;}
        sprite = SpriteDictionaryFromLinkedContainers(spriteName);
        if (sprite != null){return sprite;}
        if (defaultsEnabled)
        {
            return defaultSprite;
        }
        return null;
    }

    public Sprite SpriteByIndex(int index)
    {
        if (index < 0 || index >= sprites.Count)
        {
            if (defaultsEnabled)
            {
                return defaultSprite;
            }
            return null;
        }
        return sprites[index];
    }

    public Sprite SpriteByKey(string nKey)
    {
        int indexOf = keys == null ? -1 : keys.IndexOf(nKey);
        if (values == null || indexOf < 0 || indexOf >= values.Count)
        {
            if (defaultsEnabled)
            {
                return SpriteDictionary(defaultKey);
            }
            return null;
        }
        return SpriteDictionary(values[indexOf]);
    }

    public Sprite GetSprite(string spriteName)
    {
        return SpriteDictionary(spriteName);
    }

    string GetColorNameLocal(string nKey)
    {
        int indexOf = keys == null ? -1 : keys.IndexOf(nKey);
        if (colorNames == null || indexOf < 0 || indexOf >= colorNames.Count)
        {
            return "None";
        }
        if (colorNames[indexOf] == "")
        {
            return "None";
        }
        return colorNames[indexOf];
    }

    public string GetColorName(string nKey)
    {
        return GetColorNameLocal(nKey);
    }

    Color GetColorLocal(string nKey, Color defaultColor)
    {
        string colorName = GetColorNameLocal(nKey);
        if (colorName == "None" || colors == null)
        {
            return defaultColor;
        }
        return colors.GetColorByName(colorName);
    }

    public Color GetColor(string nKey, Color defaultColor)
    {
        return GetColorLocal(nKey, defaultColor);
    }

    string GetSizeLocal(string nKey)
    {
        int indexOf = keys == null ? -1 : keys.IndexOf(nKey);
        if (sizes == null || indexOf < 0 || indexOf >= sizes.Count)
        {
            return "1";
        }
        if (sizes[indexOf] == "")
        {
            return "1";
        }
        return sizes[indexOf];
    }

    public string GetSize(string nKey)
    {
        return GetSizeLocal(nKey);
    }

    [ContextMenu("Log Linked Sprite Duplicate Names")]
    public void LogLinkedSpriteDuplicateNames()
    {
        List<string> reportLines = new List<string>();
        Dictionary<string, int> localNames = SpriteNameCounts(this);
        AddInternalDuplicateReport(name, localNames, reportLines);

        if (linkedSpriteContainers == null || linkedSpriteContainers.Count == 0)
        {
            Debug.Log(name+" has no linked sprite containers.", this);
            return;
        }

        for (int i = 0; i < linkedSpriteContainers.Count; i++)
        {
            SpriteContainer linkedContainer = linkedSpriteContainers[i];
            if (linkedContainer == null)
            {
                reportLines.Add("Linked container at index "+i+" is null.");
                continue;
            }
            if (linkedContainer == this)
            {
                reportLines.Add("Linked container at index "+i+" references itself.");
            }

            Dictionary<string, int> linkedNames = SpriteNameCounts(linkedContainer);
            AddInternalDuplicateReport(linkedContainer.name, linkedNames, reportLines);
            AddOverlapReport(name, localNames, linkedContainer.name, linkedNames, reportLines);
        }

        for (int i = 0; i < linkedSpriteContainers.Count; i++)
        {
            SpriteContainer leftContainer = linkedSpriteContainers[i];
            if (leftContainer == null){continue;}
            Dictionary<string, int> leftNames = SpriteNameCounts(leftContainer);
            for (int j = i + 1; j < linkedSpriteContainers.Count; j++)
            {
                SpriteContainer rightContainer = linkedSpriteContainers[j];
                if (rightContainer == null){continue;}
                AddOverlapReport(leftContainer.name, leftNames, rightContainer.name, SpriteNameCounts(rightContainer), reportLines);
            }
        }

        if (reportLines.Count == 0)
        {
            Debug.Log("No duplicate sprite names found for "+name+" and its linked sprite containers.", this);
            return;
        }

        Debug.LogWarning("Sprite duplicate check for "+name+":\n"+string.Join("\n", reportLines), this);
    }

    static Dictionary<string, int> SpriteNameCounts(SpriteContainer spriteContainer)
    {
        Dictionary<string, int> counts = new Dictionary<string, int>();
        if (spriteContainer == null || spriteContainer.sprites == null){return counts;}
        for (int i = 0; i < spriteContainer.sprites.Count; i++)
        {
            Sprite sprite = spriteContainer.sprites[i];
            if (sprite == null){continue;}
            if (!counts.ContainsKey(sprite.name))
            {
                counts.Add(sprite.name, 0);
            }
            counts[sprite.name]++;
        }
        return counts;
    }

    static void AddInternalDuplicateReport(string containerName, Dictionary<string, int> names, List<string> reportLines)
    {
        List<string> duplicates = names.Where(x => x.Value > 1).Select(x => x.Key).OrderBy(x => x).ToList();
        if (duplicates.Count == 0){return;}
        reportLines.Add(containerName+" has duplicate sprite references: "+string.Join(", ", duplicates));
    }

    static void AddOverlapReport(string leftName, Dictionary<string, int> leftNames, string rightName, Dictionary<string, int> rightNames, List<string> reportLines)
    {
        List<string> duplicates = leftNames.Keys.Where(x => rightNames.ContainsKey(x)).OrderBy(x => x).ToList();
        if (duplicates.Count == 0){return;}
        reportLines.Add(leftName+" overlaps "+rightName+": "+string.Join(", ", duplicates));
    }

    public void ApplyToImage(Image image, string nKey, Color defaultColor, Vector3 defaultScale)
    {
        if (image == null){return;}
        image.sprite = GetSprite(nKey);
        image.color = GetColor(nKey, defaultColor);
        float scale = 1f;
        if (!float.TryParse(GetSize(nKey), out scale))
        {
            scale = 1f;
        }
        image.rectTransform.localScale = defaultScale * scale;
    }
}
