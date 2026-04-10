using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "Utility", menuName = "ScriptableObjects/Utility/GeneralUtility", order = 1)]
public class GeneralUtility : ScriptableObject
{
    public void DebugList<T>(List<T> dList)
    {
        for (int i = 0; i < dList.Count; i++)
        {
            Debug.Log(dList[i]);
        }
    }
    public int ChangeIndex(int currentIndex, bool right, int maxIndex, int minIndex = 0)
    {
        if (right)
        {
            if (currentIndex >= maxIndex) { return minIndex; }
            else { return currentIndex + 1; }
        }
        else
        {
            if (currentIndex > minIndex) { return currentIndex - 1; }
            else { return maxIndex; }
        }
    }

    public string GetNextItemInList(List<string> allItems, string currentItem, bool increase = true)
    {
        if (allItems.Count <= 0){ return ""; }
        int indexOf = allItems.IndexOf(currentItem);
        if (indexOf < 0) { return allItems[0]; }
        if (increase)
        {
            indexOf = (indexOf + 1) % allItems.Count;
        }
        else
        {
            indexOf = (indexOf + allItems.Count - 1) % allItems.Count;
        }
        return allItems[indexOf];
    }

    public int ChangePage(int currentPage, bool right, List<GameObject> pageLength, List<string> dataList)
    {
        int maxPage = dataList.Count / pageLength.Count;
        if (dataList.Count % pageLength.Count == 0) { maxPage--; }
        if (right)
        {
            if (currentPage < maxPage) { currentPage++; }
            else { currentPage = 0; }
        }
        else
        {
            if (currentPage > 0) { currentPage--; }
            else { currentPage = maxPage; }
        }
        return currentPage;
    }

    public int ChangePageV2(int currentPage, bool right, int pageLength, int maxLength)
    {
        int maxPage = maxLength / pageLength;
        if (maxLength % pageLength == 0) { maxPage--; }
        if (right)
        {
            if (currentPage < maxPage) { currentPage++; }
            else { currentPage = 0; }
        }
        else
        {
            if (currentPage > 0) { currentPage--; }
            else { currentPage = maxPage; }
        }
        return currentPage;
    }

    public List<string> GetCurrentPageStrings(int currentPage, List<GameObject> pageLength, List<string> dataList)
    {
        List<string> strings = new List<string>();
        if (dataList.Count <= 0)
        {
            return strings;
        }
        int start = currentPage * pageLength.Count;
        for (int i = start; i < Mathf.Min(start + pageLength.Count, dataList.Count); i++)
        {
            strings.Add(dataList[i]);
        }
        return strings;
    }

    public List<int> GetCurrentPageIndices(int currentPage, List<GameObject> pageLength, List<string> dataList)
    {
        List<int> indices = new List<int>();
        int start = currentPage * pageLength.Count;
        for (int i = start; i < Mathf.Min(start + pageLength.Count, dataList.Count); i++)
        {
            indices.Add(i);
        }
        return indices;
    }

    public (bool, int) DecrementBoolDuration(bool newBool, int boolDuration)
    {
        if (!newBool){return (newBool, boolDuration);}
        if (boolDuration > 0)
        {
            boolDuration--;
        }
        if (boolDuration <= 0)
        {
            newBool = false;
        }
        return (newBool, boolDuration);
    }

    public void DisableGameObjects(List<GameObject> objects)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            objects[i].SetActive(false);
        }
    }

    public void EnableGameObjects(List<GameObject> objects)
    {
        for (int i = 0; i < objects.Count; i++)
        {
            objects[i].SetActive(true);
        }
    }

    public void SetTextSizes(List<TMP_Text> texts, int size)
    {
        for (int i = 0; i < texts.Count; i++)
        {
            SetTextSize(texts[i], size);
        }
    }

    public void SetTextSize(TMP_Text text, int size)
    {
        text.fontSize = size;
    }

    public List<string> RemoveEmptyListItems(List<string> stringList, int minLength = 0)
    {
        for (int i = stringList.Count - 1; i >= 0; i--)
        {
            if (stringList[i].Length <= minLength)
            {
                stringList.RemoveAt(i);
            }
        }
        return stringList;
    }

    public List<int> RemoveEmptyValues(List<int> intList, int emptyValue = 0)
    {
        for (int i = intList.Count - 1; i >= 0; i--)
        {
            if (intList[i] == emptyValue)
            {
                intList.RemoveAt(i);
            }
        }
        return intList;
    }

    public List<string> ReturnFilteredList(List<string> oList, string fValue)
    {
        List<string> filtered = new List<string>();
        for (int i = 0; i < oList.Count; i++)
        {
            if (oList[i].Contains(fValue))
            {
                filtered.Add(oList[i]);
            }
        }
        return filtered;
    }

    public List<string> QuickSortIntStringList(List<string> intStrings, int left, int right)
    {
        int i = left;
        int j = right;
        int pivot = int.Parse(intStrings[left]);
        while (i <= j)
        {
            while (int.Parse(intStrings[i]) > pivot)
            {
                i++;
            }
            while (int.Parse(intStrings[j]) < pivot)
            {
                j--;
            }
            if (i <= j)
            {
                string temp = (intStrings[i]);
                intStrings[i] = intStrings[j];
                intStrings[j] = temp;
                i++;
                j--;
            }
        }
        if (left < j)
        {
            QuickSortIntStringList(intStrings, left, j);
        }
        if (i < right)
        {
            QuickSortIntStringList(intStrings, i, right);
        }
        return intStrings;
    }

    public List<string> QuickSortByIntStringList(List<string> toSort, List<string> intStrings, int left, int right)
    {
        int i = left;
        int j = right;
        int pivot = int.Parse(intStrings[left]);
        while (i <= j)
        {
            while (int.Parse(intStrings[i]) > pivot)
            {
                i++;
            }
            while (int.Parse(intStrings[j]) < pivot)
            {
                j--;
            }
            if (i <= j)
            {
                string temp = (intStrings[i]);
                intStrings[i] = intStrings[j];
                intStrings[j] = temp;
                temp = toSort[i];
                toSort[i] = toSort[j];
                toSort[j] = temp;
                i++;
                j--;
            }
        }
        if (left < j)
        {
            QuickSortByIntStringList(toSort, intStrings, left, j);
        }
        if (i < right)
        {
            QuickSortByIntStringList(toSort, intStrings, i, right);
        }
        return toSort;
    }

    public List<Sprite> SortSpritesByNames(List<Sprite> sprites)
    {
        List<string> spriteNames = new List<string>();
        List<Sprite> newOrder = new List<Sprite>();
        for (int i = 0; i < sprites.Count; i++)
        {
            spriteNames.Add(sprites[i].name);
        }
        spriteNames.Sort();
        for (int i = 0; i < spriteNames.Count; i++)
        {
            for (int j = 0; j < sprites.Count; j++)
            {
                if (spriteNames[i] == sprites[j].name)
                {
                    newOrder.Add(sprites[j]);
                    break;
                }
            }
        }
        return newOrder;
    }

    public string ConvertIntListToString(List<int> int_list, string delimiter = "|")
    {
        List<string> string_list = ConvertIntListToStringList(int_list);
        return ConvertListToString(string_list, delimiter);
    }

    public List<string> ConvertIntListToStringList(List<int> int_list)
    {
        List<string> string_list = new List<string>();
        for (int i = 0; i < int_list.Count; i++)
        {
            string_list.Add(int_list[i].ToString());
        }
        return string_list;
    }

    public List<int> ConvertStringListToIntList(List<string> string_list, int safeParseValue = 0)
    {
        List<int> int_list = new List<int>();
        for (int i = 0; i < string_list.Count; i++)
        {
            int_list.Add(SafeParseInt(string_list[i], safeParseValue));
        }
        return int_list;
    }

    public string ConvertListToString(List<string> string_list, string delimiter = "|")
    {
        return String.Join(delimiter, string_list);
    }

    public string ConvertArrayToString(string[] string_array, string delimiter = "|")
    {
        List<string> string_list = string_array.ToList();
        return ConvertListToString(string_list, delimiter);
    }

    public int CountStringsInList(List<string> list, string specifics)
    {
        int count = 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == specifics) { count++; }
        }
        return count;
    }

    public int CountStringsInArray(string[] array, string s)
    {
        int count = 0;
        for (int i = 0; i < array.Length; i++)
        {
            if (array[i] == s){count++;}
        }
        return count;
    }

    public int CountCharactersInString(string dummyString, char specifics = ' ')
    {
        int count = 0;
        for (int i = 0; i < dummyString.Length; i++)
        {
            if (dummyString[i] == specifics)
            {
                count++;
            }
        }
        return count;
    }

    public bool IntListContainsIntList(List<int> fullList, List<int> partialList)
    {
        for (int i = 0; i < partialList.Count; i++)
        {
            if (!fullList.Contains(partialList[i]))
            {
                return false;
            }
        }
        return true;
    }

    public bool ListContainsList(List<string> fList, List<string> pList)
    {
        for (int i = 0; i < pList.Count; i++)
        {
            if (!fList.Contains(pList[i]))
            {
                return false;
            }
        }
        return true;
    }

    public int SafeParseInt(string intString, int defaultValue = 0)
    {
        try
        {
            return int.Parse(intString);
        }
        catch
        {
            return defaultValue;
        }
    }

    public int SumDescending(int lastValue)
    {
        return (lastValue) * (lastValue + 1) / 2;
    }

    public int Exponent(int eBase, int exponent = 2)
    {
        int value = 1;
        for (int i = 0; i < exponent; i++)
        {
            value *= eBase;
        }
        return value;
    }

    public int Root(int rBase, int root = 2)
    {
        // The most retarded, hard coded and slow method possible.
        if (root <= 0){return 1;}
        else if (root == 1){return rBase;}
        if (rBase <= 0){return 0;}
        else if (rBase == 1){return 1;}
        for (int i = 2; i < rBase + 1; i++)
        {
            // Rounds down.
            if (Exponent(i, root) > rBase){return i - 1;}
        }
        // Not sure how to get here.
        return -1;
    }

    public int Roll(int modifier, int diceSize = 100)
    {
        if (diceSize <= 1){return 0;}
        return UnityEngine.Random.Range(0, diceSize) + modifier;
    }

    public int RollRarity(int maxRarity, int rollBonus = 0)
    {
        int rarity = 0;
        for (int i = 0; i < maxRarity; i++)
        {
            int roll = UnityEngine.Random.Range(0, 2 + i + rollBonus);
            if (roll == 0){rarity++;}
        }
        return rarity;
    }

    public string RandomStringBasedOnWeight(List<string> strings, List<int> weights)
    {
        int totalWeight = 0;
        for (int i = 0; i < weights.Count; i++)
        {
            totalWeight += weights[i];
        }
        int roll = UnityEngine.Random.Range(0, totalWeight);
        for (int i = 0; i < weights.Count; i++)
        {
            if (roll < weights[i])
            {
                return strings[i];
            }
            else
            {
                roll -= weights[i];
            }
        }
        return strings[0];
    }

    public string ReturnValueBasedOnWeight(List<string> values, List<int> valueWeights, int weight)
    {
        if (values.Count <= 0){return "";}
        if (weight < 0 || weight >= valueWeights.Sum()){return values[0];}
        for (int i = 0; i < valueWeights.Count; i++)
        {
            if (weight < valueWeights[i]){return values[i];}
            weight -= valueWeights[i];
        }
        return "";
    }

    public int ReturnIndexBasedOnWeight(List<int> valueWeights, int weight)
    {
        if (valueWeights.Count <= 0){return -1;}
        if (weight < 0 || weight >= valueWeights.Sum()){return 0;}
        for (int i = 0; i < valueWeights.Count; i++)
        {
            if (weight < valueWeights[i]){return i;}
            weight -= valueWeights[i];
        }
        return 0;
    }

    public List<int> ShuffleIntList(List<int> intList)
    {
        for (int i = 0; i < intList.Count; i++)
        {
            int rng = UnityEngine.Random.Range(0, intList.Count);
            int value = intList[i];
            intList[i] = intList[rng];
            intList[rng] = value;
        }
        return intList;
    }
}
