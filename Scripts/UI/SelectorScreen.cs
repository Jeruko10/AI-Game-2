using Godot;
using System;


namespace UI;
public partial class SelectorScreen : Control
{
    [Export] Button displayButton;
    [Export] FlowContainer troopsContainer;
    [Export] ColorRect backgroundRect;

    bool isDisplayed = false;
    
    public override void _Ready()
    {
        displayButton.Pressed += OnDisplayButtonPressed;
    }

    void OnDisplayButtonPressed()
    {
        isDisplayed = !isDisplayed;
        Tween tween = CreateDefaultTween();
        
        Vector2 originalPosition = backgroundRect.Position;
        if (!isDisplayed)
            tween.TweenProperty(backgroundRect, "position", originalPosition + new Vector2(490.0f, 0f), 0.5f);
        else
            tween.TweenProperty(backgroundRect, "position", originalPosition - new Vector2(490.0f, 0f), 0.5f);
    }

    Tween CreateDefaultTween()
    {
        Tween tween = GetTree().CreateTween();

        tween.SetTrans(Tween.TransitionType.Quint);
        tween.SetEase(Tween.EaseType.Out);

        return tween;
    }
}
