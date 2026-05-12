using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.5")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/MCH_Default.PvP.cs")]

public sealed class MCH_DefaultPvP : MachinistRotation
{
	#region Configurations
	#endregion

	#region oGCDs
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
	{
		if (InCombat && BraveryPvP.CanUse(out action))
		{
			return true;
		}

		if (InCombat && DervishPvP.CanUse(out action))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out action);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? action)
	{
		if (nextGCD.IsTheSameTo(false, ActionID.DrillPvP, ActionID.BioblasterPvP, ActionID.AirAnchorPvP, ActionID.ChainSawPvP))
		{
			if (AnalysisPvP.CanUse(out action, usedUp: true)
				&& nextGCD is IBaseAction nextGcdAction
				&& PvPBurstGate.ShouldUse(nextGcdAction))
			{
				return true;
			}
		}

		if (WildfirePvP.CanUse(out action))
		{
			if (StatusHelper.PlayerHasStatus(true, StatusID.Overheated_3149) && PvPBurstGate.ShouldUse(WildfirePvP))
			{
				return true;
			}
		}

		if (BishopAutoturretPvP.CanUse(out action) && PvPBurstGate.ShouldUse(BishopAutoturretPvP))
		{
			return true;
		}

		if (EagleEyeShotPvP.CanUse(out action) && PvPBurstGate.ShouldUse(EagleEyeShotPvP))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out action);
	}

	#endregion

	#region GCDs
	protected override bool GeneralGCD(out IAction? action)
	{
		if (FullMetalFieldPvP.CanUse(out action) && PvPBurstGate.ShouldUse(FullMetalFieldPvP))
		{
			return true;
		}

		if (BlazingShotPvP.CanUse(out action))
		{
			if (StatusHelper.PlayerHasStatus(true, StatusID.Overheated_3149)
				&& !StatusHelper.PlayerHasStatus(true, StatusID.Analysis)
				&& PvPBurstGate.ShouldUse(BlazingShotPvP))
			{
				return true;
			}
		}

		if (DrillPvP.CanUse(out action, usedUp: true) && PvPBurstGate.ShouldUse(DrillPvP))
		{
			return true;
		}

		if (BioblasterPvP.CanUse(out action, usedUp: true) && PvPBurstGate.ShouldUse(BioblasterPvP))
		{
			return true;
		}

		if (AirAnchorPvP.CanUse(out action, usedUp: true) && PvPBurstGate.ShouldUse(AirAnchorPvP))
		{
			return true;
		}

		if (ChainSawPvP.CanUse(out action, usedUp: true) && PvPBurstGate.ShouldUse(ChainSawPvP))
		{
			return true;
		}

		if (ScattergunPvP.CanUse(out action))
		{
			if (!StatusHelper.PlayerHasStatus(true, StatusID.Overheated_3149))
			{
				return true;
			}
		}

		if (BlastChargePvP.CanUse(out action))
		{
			return true;
		}

		return base.GeneralGCD(out action);
	}
	#endregion
}
