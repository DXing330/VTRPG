using UnityEngine;
// Generic fixed shop slot for consumables/relics/services.
public class ShopTextSlotDisplay : MonoBehaviour
{
    public StSStore store;
    public string section;
    public int index;
    public IconDisplay iconDisplay;

    public void SetSlot(string itemName, SpriteContainer sprites, string price, bool sold)
    {
        iconDisplay.SetIcon(itemName, sprites, price);
        SetHighlighted(false);
    }

    public void ResetSlot()
    {
        if (iconDisplay != null)
        {
            iconDisplay.ResetDisplay();
        }
        SetHighlighted(false);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (iconDisplay != null)
        {
            iconDisplay.SetHighlighted(highlighted);
        }
    }

    public void SelectSlot()
    {
        if (store != null)
        {
            store.SelectSlot(section, index);
        }
    }
}
