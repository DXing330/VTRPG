using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuneGridManager : MonoBehaviour
{
    public StatDatabase runeLetters;
    public StatDatabase runeWords;
    public List<EquipSlots> runeSlotOrder = new List<EquipSlots>()
    {
        EquipSlots.Weapon,
        EquipSlots.Armor,
        EquipSlots.Charm,
        EquipSlots.Helmet,
        EquipSlots.Boots,
        EquipSlots.Gloves
    };
    public Equipment ReturnEquipmentOfSlot(List<Equipment> equipment, EquipSlots slot)
    {
        for (int i = 0; i < equipment.Count; i++)
        {
            if (equipment[i].slot == slot)
            {
                return equipment[i];
            }
        }
        return null;
    }
    public string[,] CreateEmptyRuneGrid()
    {
        string[,] runeGrid = new string[6, 6];
        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 6; col++)
            {
                runeGrid[row, col] = "";
            }
        }
        return runeGrid;
    }
    public void AddEquipmentRunesToGrid(string[,] grid, Equipment equipment, int row)
    {
        if (equipment == null){return;}
        List<string> runes = equipment.GetRunes();
        for (int col = 0; col < runes.Count && col < 6; col++)
        {
            grid[row, col] = runes[col];
        }
    }
    public string[,] ReturnRuneGrid(List<Equipment> equipment)
    {
        string[,] grid = CreateEmptyRuneGrid();
        for (int row = 0; row < runeSlotOrder.Count; row++)
        {
            Equipment slotEquipment = ReturnEquipmentOfSlot(equipment, runeSlotOrder[row]);
            AddEquipmentRunesToGrid(grid, slotEquipment, row);
        }
        return grid;
    }
    public string ReturnRuneDiagonal(string[,] grid)
    {
        string line = "";
        for (int i = 0; i < 6; i++)
        {
            line += runeLetters.ReturnValue(grid[i, i]);
        }
        return line;
    }
    public List<string> ReturnRuneRows(string[,] grid)
    {
        List<string> rows = new List<string>();
        for (int row = 0; row < 6; row++)
        {
            string line = "";
            for (int col = 0; col < 6; col++)
            {
                line += runeLetters.ReturnValue(grid[row, col]);
            }
            rows.Add(line);
        }
        return rows;
    }
    public List<string> ReturnRuneColumns(string[,] grid)
    {
        List<string> cols = new List<string>();
        for (int col = 0; col < 6; col++)
        {
            string line = "";
            for (int row = 0; row < 6; row++)
            {
                line += runeLetters.ReturnValue(grid[row, col]);
            }
            cols.Add(line);
        }
        return cols;
    }
    public void ApplyRuneGridToActor(TacticActor actor)
    {
        string[,] runeGrid = ReturnRuneGrid(actor.GetBaseEquipment());
        List<string> runePassives = new List<string>();
        List<string> rowWords = ReturnRuneRows(runeGrid);
        List<string> colWords = ReturnRuneColumns(runeGrid);
        string diagonal = ReturnRuneDiagonal(runeGrid);
        // Add The Regular Rune Passives.
        for (int row = 0; row < 6; row++)
        {
            // If the row is a word, then skip the letter passives and add the row passives.
            if (runeWords.ReturnValue(rowWords[row]).Length > 1)
            {
                runePassives.Add(runeWords.ReturnValue(rowWords[row]));
                continue;
            }
            for (int col = 0; col < 6; col++)
            {
                runePassives.Add(runeGrid[row, col]);
            }
        }
        // Add Column Word Passives.
        for (int i = 0; i < colWords.Count; i++)
        {
            runePassives.Add(runeWords.ReturnValue(colWords[i]));
        }
        // Add Diagonal Word Passives.
        runePassives.Add(runeWords.ReturnValue(diagonal));
        for (int i = 0; i < runePassives.Count; i++)
        {
            actor.AddRunePassive(runePassives[i]);
        }
    }
}
