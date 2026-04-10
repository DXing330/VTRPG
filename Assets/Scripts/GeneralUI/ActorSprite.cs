using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActorSprite : MonoBehaviour
{
    public SpriteContainer actorSprites;
    public Image actorSprite;
    protected Color defaultSpriteColor = Color.white;
    protected Vector3 defaultSpriteScale = Vector3.one;
    protected bool initializedAppearance = false;
    public virtual void SetTextSize(int newSize)
    {
    }

    protected void InitializeAppearance()
    {
        if (initializedAppearance || actorSprite == null){return;}
        defaultSpriteColor = actorSprite.color;
        defaultSpriteScale = actorSprite.rectTransform.localScale;
        initializedAppearance = true;
    }

    protected void UpdateActorSpriteAppearance(string spriteKey)
    {
        InitializeAppearance();
        actorSprite.sprite = actorSprites.GetSprite(spriteKey);
        actorSprite.color = actorSprites.GetColor(spriteKey, defaultSpriteColor);
        float scale = 1f;
        if (!float.TryParse(actorSprites.GetSize(spriteKey), out scale))
        {
            scale = 1f;
        }
        actorSprite.rectTransform.localScale = defaultSpriteScale * scale;
    }

    public void ShowActorSprite(TacticActor actor)
    {
        UpdateActorSpriteAppearance(actor.GetSpriteName());
    }

    public virtual void ShowActorInfo(TacticActor actor)
    {
        Debug.Log(actor.GetPersonalName());
    }

    public virtual void ChangeTextColor(Color newColor){}
}
