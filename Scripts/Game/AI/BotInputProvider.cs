using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Utility;

namespace Game;

[GlobalClass]
public partial class BotInputProvider : VirtualInputProvider
{
	[Export] Board.Players self;
	[Export] float playSpeed = 1f;
	[Export] float courtesyDelay = 1f;

    public override void _Ready() => Board.State.TurnStarted += OnTurnStarted;

    public async Task SimulateHumanClick(Vector2I cell, bool rightClick = false, float hoverTime = 0.2f, float afterClickTime = 0.2f)
	{
		SimulateHover(cell);
		await SimulateDelay(hoverTime);

		if (rightClick) SimulateRightClick(cell);
		else SimulateLeftClick(cell);

		await SimulateDelay(afterClickTime);
	}

	async Task SimulateDelay(float seconds) => await Task.Delay((int)Mathf.Round(seconds * 1000 / playSpeed));

	List<Minion> GetEnemyMinions() => GetMinionsOwnedBy(Board.GetRival(self));

	List<Minion> GetFriendlyMinions() => GetMinionsOwnedBy(self);

	static List<Minion> GetMinionsOwnedBy(Board.Players player)
	{
		List<Minion> minions = [];

		foreach (Minion minion in Board.State.Minions)
			if (minion.Owner == player)
				minions.Add(minion);

		return minions;
	}

	async void OnTurnStarted(Board.Players newTurnOwner)
	{
		if (newTurnOwner != self) return;

		await GetTree().DelayUntil(() => InputHandler.InteractionEnabled);
		await PlayTurn();
	}
}
