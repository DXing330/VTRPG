using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MapTile : MonoBehaviour
{
    public int tileNumber;
    public void SetTileNumber(int newNumber)
    {
        tileNumber = newNumber;
    }
    public ClickTileManager cMap;
    public GameObject mainObject;
    public Image elevationImage;
    public GameObject elevationObject;
    public Image highlightImage;
    public GameObject highlightObject;
    public Color defaultColor;
    public void UpdateElevationSprite(Sprite newSprite, int elevation)
    {
        if (elevation == 0)
        {
            elevationObject.SetActive(false);
            return;
        }
        elevationObject.SetActive(true);
        elevationImage.sprite = newSprite;
    }
    public List<GameObject> layerObjects;
    // Tile, Character, Tile Effect, Highlight
    public List<Image> layers;
    protected List<Color> defaultLayerColors;
    protected List<Vector3> defaultLayerScales;
    protected bool layerAppearanceInitialized = false;
    public List<GameObject> directionObjects;
    public List<GameObject> borderObjects;
    public List<Image> borderImages;
    public void ResetBorders()
    {
        for (int i = 0; i < borderObjects.Count; i++)
        {
            borderObjects[i].SetActive(false);
        }
    }
    public void AddBorder(int direction)
    {
        borderObjects[direction].SetActive(true);
    }
    public void UpdateBorderImage(int direction, Sprite borderSprite)
    {
        if (borderSprite == null)
        {
            borderObjects[direction].SetActive(false);
            return;
        }
        borderObjects[direction].SetActive(true);
        borderImages[direction].sprite = borderSprite;
    }
    public GameObject healthBarObject;
    public void ResetHealthBar()
    {
        healthBarObject.SetActive(false);
    }
    public RectTransform healthBar;
    public void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        healthBarObject.SetActive(true);
        float percent = Mathf.Clamp01((float)currentHealth / maxHealth);
        Vector3 scale = healthBar.localScale;
        scale.x = percent;
        healthBar.localScale = scale;
    }
    public TMP_Text tileText;
    public GameObject textObject;
    public void UpdateText(string newText = "")
    {
        if (newText == "")
        {
            textObject.SetActive(false);
        }
        textObject.SetActive(true);
        tileText.text = newText;
    }
    public void DisableLayers()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            layerObjects[i].SetActive(false);
        }
    }
    public void EnableLayer(int layer = 0)
    {
        layerObjects[layer].SetActive(true);
    }
    public void EnableLayers()
    {
        for (int i = 0; i < layers.Count; i++)
        {
            layerObjects[i].SetActive(true);
        }
    }
    public void UpdateDirectionArrow(string direction)
    {
        ResetDirectionArrows();
        if (direction == "") { return; }
        int dirInt = int.Parse(direction);
        if (dirInt >= 0 && dirInt < directionObjects.Count)
        {
            directionObjects[dirInt].SetActive(true);
        }
    }
    public void ResetDirectionArrows()
    {
        for (int i = 0; i < directionObjects.Count; i++)
        {
            directionObjects[i].SetActive(false);
        }
    }
    public void ActivateAllDirectionArrows()
    {
        for (int i = 0; i < directionObjects.Count; i++)
        {
            directionObjects[i].SetActive(true);
        }
    }
    public void ActivateDirectionArrow(int direction)
    {
        if (direction < 0) { return; }
        directionObjects[direction].SetActive(true);
    }
    public void ActivateLayerSprite(int layer)
    {
        if (layer < 0 || layer > layers.Count)
        {
            return;
        }
        layerObjects[layer].SetActive(true);
    }
    public Image GetLayerSprite(int layer)
    {
        if (layer < 0 || layer > layers.Count)
        {
            return null;
        }
        return layers[layer];
    }
    public void UpdateLayerSprite(Sprite newSprite, int layer = 0)
    {
        if (layer < 0 || layer > layers.Count) { return; }
        if (newSprite == null)
        {
            layerObjects[layer].SetActive(false);
            return;
        }
        layerObjects[layer].SetActive(true);
        layers[layer].sprite = newSprite;
    }
    protected void InitializeLayerAppearance()
    {
        if (layerAppearanceInitialized){return;}
        defaultLayerColors = new List<Color>();
        defaultLayerScales = new List<Vector3>();
        for (int i = 0; i < layers.Count; i++)
        {
            defaultLayerColors.Add(layers[i].color);
            defaultLayerScales.Add(layers[i].rectTransform.localScale);
        }
        layerAppearanceInitialized = true;
    }
    public Color GetDefaultLayerColor(int layer)
    {
        if (layer < 0 || layer >= layers.Count) { return Color.white; }
        InitializeLayerAppearance();
        return defaultLayerColors[layer];
    }
    public void UpdateStyledLayerSprite(Sprite newSprite, Color newColor, float newScale = 1f, int layer = 0)
    {
        if (layer < 0 || layer >= layers.Count) { return; }
        InitializeLayerAppearance();
        if (newSprite == null)
        {
            layerObjects[layer].SetActive(false);
            return;
        }
        layerObjects[layer].SetActive(true);
        layers[layer].sprite = newSprite;
        layers[layer].color = newColor;
        layers[layer].rectTransform.localScale = defaultLayerScales[layer] * newScale;
    }
    public void ResetAllLayers()
    {
        for (int i = 0; i < layerObjects.Count; i++)
        {
            ResetLayerSprite(i);
        }
    }
    public void ResetLayerSprite(int layer)
    {
        InitializeLayerAppearance();
        layerObjects[layer].SetActive(false);
        layers[layer].color = defaultLayerColors[layer];
        layers[layer].rectTransform.localScale = defaultLayerScales[layer];
    }
    public void ResetHighlight()
    {
        highlightObject.SetActive(false);
        highlightImage.color = defaultColor;
    }
    public void HighlightTile(Color newColor)
    {
        highlightObject.SetActive(true);
        highlightImage.color = newColor;
    }
    public void HighlightLayer(int layer, Color newColor)
    {
        layerObjects[layer].SetActive(true);
        layers[layer].color = newColor;
    }
    public void ClickTile()
    {
        if (cMap == null)
        {
            return;
        }
        cMap.ClickOnTile(tileNumber);
    }
    public void ClickDirection(int direction)
    {
        if (cMap == null)
        {
            return;
        }
        cMap.ClickDirection(tileNumber, direction);
    }
    // AUTOCHESS SPECIFIC ITEMS
    public List<GameObject> autoChessEquipmentObjects;
    public List<Image> autoChessEquipmentImages;
    public void ResetAutoChessEquipment()
    {
        for (int i = 0; i < autoChessEquipmentObjects.Count; i++)
        {
            autoChessEquipmentObjects[i].SetActive(false);
        }
    }
    public void EnableEquipSlot(int index)
    {
        if (index >= autoChessEquipmentObjects.Count){return;}
        autoChessEquipmentObjects[index].SetActive(true);
    }
    public Image GetEquipSlotImage(int index)
    {
        if (index >= autoChessEquipmentImages.Count){return null;}
        return autoChessEquipmentImages[index];
    }
}
