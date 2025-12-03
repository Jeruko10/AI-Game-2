using Game;
using Godot;
using System;
using System.Diagnostics;


namespace UI;
public partial class SelectorScreen : Control
{
    [Export] Button displayButton;
    [Export] FlowContainer troopsContainer;
    [Export] ColorRect backgroundRect;
    [Export] ColorRect overlayRect;

    [Export] PackedScene troopCardScene;


    bool isDisplayed = false;
    
    public override void _Ready()
    {
        displayButton.Pressed += OnDisplayButtonPressed;
        GenerateCards();
    }

    void GenerateCards()
    {
        foreach (var troopType in Minions.AllMinionDatas)
        {
            var troopCardInstance = troopCardScene.Instantiate() as MinionCard;
            if (troopCardInstance == null) GD.PushError("CardScene is not provided in SelectorScreen");

            troopCardInstance.SetUpButton(troopType);

            troopsContainer.AddChild(troopCardInstance);
        }
    }

    

    void OnDisplayButtonPressed()
    {
        isDisplayed = !isDisplayed;
        Tween tween = CreateDefaultTween();
        
        Vector2 originalPosition = backgroundRect.Position;
        if (!isDisplayed)
        {
            tween.TweenProperty(backgroundRect, "position", originalPosition + new Vector2(490.0f, 0f), 0.5f);
            tween.Parallel().TweenProperty(overlayRect, "modulate:a", 0.0f, 0.5f);
        }
        else
        {
            tween.TweenProperty(backgroundRect, "position", originalPosition - new Vector2(490.0f, 0f), 0.5f);
            tween.Parallel().TweenProperty(overlayRect, "modulate:a", 0.5f, 0.5f);
        }
    }

    Tween CreateDefaultTween()
    {
        Tween tween = GetTree().CreateTween();

        tween.SetTrans(Tween.TransitionType.Quint);
        tween.SetEase(Tween.EaseType.Out);

        return tween;
    }
}
