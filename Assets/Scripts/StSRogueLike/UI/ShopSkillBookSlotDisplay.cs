using UnityEngine;
using TMPro;

// Fixed shop slot wrapper around SkillDisplay with price/sold support.
public class ShopSkillBookSlotDisplay : MonoBehaviour
{
    public StSStore store;
    public int index;
    public SkillDisplay skillDisplay;
    public TMP_Text priceText;
    public GameObject soldObject;

    public void SetSlot(string bookName, string price, bool sold, bool colorless = false)
    {
        if (skillDisplay != null)
        {
            skillDisplay.SetSkillBook(bookName, colorless);
            skillDisplay.SetHighlighted(false);
        }
        if (priceText != null){priceText.text = sold ? "Sold" : price;}
        SetSold(sold);
    }

    public void ResetSlot()
    {
        if (priceText != null){priceText.text = "";}
        if (skillDisplay != null)
        {
            skillDisplay.SetHighlighted(false);
        }
        SetSold(false);
    }

    public void SetSold(bool sold)
    {
        if (soldObject != null)
        {
            soldObject.SetActive(sold);
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (skillDisplay != null)
        {
            skillDisplay.SetHighlighted(highlighted);
        }
    }

    public void SelectSlot()
    {
        if (store != null)
        {
            store.SelectSlot("Book", index);
        }
    }
}
