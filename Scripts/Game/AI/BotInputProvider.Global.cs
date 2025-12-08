using Components;
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Game;

public partial class BotInputProvider : VirtualInputProvider
{
    [Export] public GlobalRootState RootState;
    
    async Task PlayTurn()
    {
        IGlobalState globalState = ChangeGlobalState(); // BE AWARE OF POSSIBLE INFINITE LOOPS CRASHING THE EDITOR: We iterate over state transitions until a state demands no transition.
        List<Waypoint> waypoints = globalState.GenerateWaypoints();

        Board.State.ClearWaypoints();
        foreach (var wp in waypoints)
        {
            Board.State.AddWaypoint(wp);
        }

        await SimulateDeploy(waypoints);

        await SimulateDelay(courtesyDelay);

        foreach(Minion minion in GetFriendlyMinions())
            await PlayMinionStrategy(minion, waypoints);

        SimulatePassTurn();
    }

    private async Task SimulateDeploy(List<Waypoint> waypoints)
    {
        var deployWaypoints = waypoints
            .Where(wp => wp.Type == Waypoint.Types.Deploy)
            .OrderByDescending(wp => wp.Priority)
            .ToList();

        foreach (var waypoint in deployWaypoints)
        {
            MinionData minionToDeploy = GetMinionWithManaLogic(waypoint);
            if (minionToDeploy == null)
                continue; // No hay minion que podamos pagar

            Board.State.SelectedDeployTroopPlayer2 = minionToDeploy;
            await SimulateHumanClick(waypoint.Cell, true);
        }
    }

    private MinionData GetMinionWithManaLogic(Waypoint waypoint)
    {
        var preferred = GetMinionToDeploy(waypoint.ElementAffinity);
        if (preferred != null && CanPay(preferred))
            return preferred;

        var disadvantage = GetMinionToDeploy(Element.GetDisadvantage(waypoint.ElementAffinity));
        if (disadvantage != null && CanPay(disadvantage))
            return disadvantage;

        var advantage = GetMinionToDeploy(Element.GetAdvantage(waypoint.ElementAffinity));
        if (advantage != null && CanPay(advantage))
            return advantage;

        return null;
    }

    private bool CanPay(MinionData data)
    {
        return Board.State.Player2Mana >= data.Cost;
    }

    private static MinionData GetMinionToDeploy(Element.Types elementAffinity)
    {
        return Minions.AllMinionDatas
            .FirstOrDefault(m => m.Element.Tag == elementAffinity);
    }


    IGlobalState ChangeGlobalState()
    {
        IGlobalState globalState;
        // BE AWARE OF POSSIBLE INFINITE LOOPS CRASHING THE EDITOR: We iterate over state transitions until a state demands no transition.
        do
        {
            State activeLeafState = RootState.GetDeepestActiveState();
            
            if (activeLeafState is IGlobalState) globalState = activeLeafState as IGlobalState;
            else
            {
                GD.PushError($"Active leaf state '{activeLeafState.Name}' does not implement IGlobalState interface.");
                return null;
            }
        }
        while (globalState.TryChangeState());

        return globalState;
    }
}
