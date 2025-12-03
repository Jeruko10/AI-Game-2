using Game;
using Godot;
using System;

public partial class MinionCard : Button
{

    [Export] Sprite2D iconElementType;
    [Export] Sprite2D silhouette; 
    [Export] Label nameLabel; 
    [Export] Label priceLabel;
    [Export] Control controlSelected;
    MinionData minionData;


    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    public override void _Process(double delta)
    {
        if(Board.State.SelectedDeployTroop == minionData)
        {
            controlSelected.Visible = true;
        }
        else
        {
            controlSelected.Visible = false;
        }
    }


    public void SetUpButton(MinionData minionData)
    {
        this.minionData = minionData;

        CustomMinimumSize = new Vector2(Size.X, Size.Y);

        silhouette.Texture = minionData.Texture;
        nameLabel.Text = minionData.Name;
        priceLabel.Text = GetTroopCost(minionData);
        iconElementType.Texture = minionData.Element.Symbol;
    }


    void OnPressed()
    {
        GD.Print($"minion type selected: {minionData}");
        Board.State.SelectedDeployTroop = minionData;
    }   

    string GetTroopCost(MinionData troopType)
    {
        return troopType.Element.Tag switch
        {
            Element.Types.Fire => troopType.Cost.FireMana.ToString(),
            Element.Types.Water => troopType.Cost.WaterMana.ToString(),
            Element.Types.Plant => troopType.Cost.PlantMana.ToString(),
            _ => "0",
        };
    }
}
