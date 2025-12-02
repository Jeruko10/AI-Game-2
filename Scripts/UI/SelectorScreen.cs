using Game;
using Godot;
using System;


namespace UI;
public partial class SelectorScreen : Control
{
    [Export] Button displayButton;
    [Export] FlowContainer troopsContainer;
    [Export] ColorRect backgroundRect;

    [Export] PackedScene troopCardScene;
    [Export] Sprite2D fireIcon;
    [Export] Sprite2D waterIcon;
    [Export] Sprite2D plantIcon;




    bool isDisplayed = false;
    
    public override void _Ready()
    {
        displayButton.Pressed += OnDisplayButtonPressed;
        GenerateCards();
    }

    private void GenerateCards()
    {
        foreach (var troopType in Minions.AllMinionDatas)
        {
            var troopCardInstance = troopCardScene.Instantiate() as Button;
            if (troopCardInstance == null) GD.PushError("CardScene is not provided in SelectorScreen");

            Sprite2D iconElementType = troopCardInstance.GetNode<Sprite2D>("IconElementType");
            Sprite2D silhouette = troopCardInstance.GetNode<Sprite2D>("ElemnentSilhouette");
            Label nameLabel = troopCardInstance.GetNode<Label>("name");
            Label priceLabel = troopCardInstance.GetNode<Label>("PriceLabel");

            silhouette.Texture = troopType.Texture;
            nameLabel.Text = troopType.Name;
            priceLabel.Text = troopType.Cost.ToString();
            iconElementType.Texture = GetElementIconTexture(troopType.Element.Tag);


            troopsContainer.AddChild(troopCardInstance);
        }
    }

    private Texture2D GetElementIconTexture(Element.Types tag)
    {
        return tag switch
        {
            Element.Types.Fire  => fireIcon.Texture,
            Element.Types.Water => waterIcon.Texture,
            Element.Types.Plant => plantIcon.Texture,
            _                   => null
        };
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
