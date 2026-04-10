using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ActorSpriteAndName : ActorSprite
{
    public TMP_Text actorName;

    public override void SetTextSize(int newSize)
    {
        actorName.fontSize = newSize;
    }

    public override void ShowActorInfo(TacticActor actor)
    {
        ShowActorSprite(actor);
        actorName.text = actor.GetPersonalName();
    }

    public override void ChangeTextColor(Color newColor)
    {
        actorName.color = newColor;
    }
}
