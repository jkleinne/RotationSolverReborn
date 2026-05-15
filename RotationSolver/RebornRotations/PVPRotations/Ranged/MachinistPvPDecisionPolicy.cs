using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

internal readonly record struct MachinistPvPDecisionInput(
	MachinistPvPTargetSnapshot Target,
	bool SafeCloseRange,
	bool FollowUpAvailable,
	bool AlliesCanBurst,
	bool ObjectiveControlNeeded,
	bool TargetCommitted);

internal static class MachinistPvPDecisionPolicy
{
	private const float DrillKillHealthRatio = 0.35f;
	private const float BurstHealthRatio = 0.65f;
	private const float MarksmanHealthRatio = 0.70f;
	private const float MarksmanCleanupHealthRatio = 0.18f;
	private const float MarksmanFocusedCleanupHealthRatio = 0.25f;

	internal static bool ShouldUseAnalysisDrill(MachinistPvPDecisionInput input)
	{
		if (!input.Target.IsInNormalRange)
		{
			return false;
		}

		if (input.Target.HasGuard)
		{
			return input.Target.HealthRatio <= DrillKillHealthRatio
				|| input.ObjectiveControlNeeded;
		}

		return IsKillWindow(input)
			|| input.Target.IsVulnerable
			|| input.ObjectiveControlNeeded;
	}

	internal static bool ShouldUseAnalysisAirAnchor(MachinistPvPDecisionInput input)
	{
		if (!CanApplyControl(input))
		{
			return false;
		}

		return IsKillWindow(input)
			|| input.ObjectiveControlNeeded
			|| (input.FollowUpAvailable && (input.AlliesCanBurst || input.Target.IsVulnerable));
	}

	internal static bool ShouldUseAnalysisChainSaw(MachinistPvPDecisionInput input)
	{
		if (!input.Target.IsInNormalRange || input.Target.HasGuard)
		{
			return false;
		}

		return input.AlliesCanBurst
			|| IsKillWindow(input)
			|| (input.FollowUpAvailable && input.Target.IsVulnerable)
			|| input.Target.HasAllyFocus
			|| input.Target.IsVulnerable
			|| input.ObjectiveControlNeeded;
	}

	internal static bool ShouldUseAnalysisBioblaster(MachinistPvPDecisionInput input)
	{
		return input.SafeCloseRange
			&& input.Target.IsInCloseRange
			&& !input.Target.HasGuard
			&& (input.ObjectiveControlNeeded || input.Target.HasAllyFocus || input.Target.IsVulnerable);
	}

	internal static bool ShouldUseScattergun(MachinistPvPDecisionInput input)
	{
		if (!input.SafeCloseRange || !input.Target.IsInCloseRange)
		{
			return false;
		}

		if ((input.Target.HasGuard || input.Target.HasResilience) && input.ObjectiveControlNeeded)
		{
			return false;
		}

		return IsKillWindow(input)
			|| input.ObjectiveControlNeeded
			|| input.Target.HasAllyFocus
			|| (input.FollowUpAvailable && input.TargetCommitted);
	}

	internal static bool ShouldUseWildfire(MachinistPvPDecisionInput input)
	{
		if (!input.TargetCommitted || !input.FollowUpAvailable || input.Target.HasGuard)
		{
			return false;
		}

		return IsKillWindow(input)
			|| input.AlliesCanBurst
			|| input.ObjectiveControlNeeded
			|| input.Target.IsVulnerable;
	}

	internal static bool ShouldUseBishop(MachinistPvPDecisionInput input)
	{
		return input.ObjectiveControlNeeded
			|| (input.Target.IsObjectiveRelevant && (input.AlliesCanBurst || input.TargetCommitted));
	}

	internal static bool ShouldUseMarksmanSpite(MachinistPvPDecisionInput input)
	{
		if (!input.Target.IsInNormalRange || input.Target.HasGuard)
		{
			return false;
		}

		if (!input.ObjectiveControlNeeded && IsLikelyAlreadyDying(input))
		{
			return false;
		}

		if (input.Target.HealthRatio > MarksmanHealthRatio && !input.Target.IsVulnerable)
		{
			return false;
		}

		return input.Target.CurrentMp <= PvPScoringFactors.MediumMp
			|| (!input.Target.HasResilience && input.FollowUpAvailable)
			|| input.Target.HasAllyFocus
			|| input.ObjectiveControlNeeded;
	}

	internal static bool ShouldUseFullMetalField(MachinistPvPDecisionInput input)
	{
		if (!input.Target.IsInNormalRange || input.Target.HasGuard)
		{
			return false;
		}

		return (input.FollowUpAvailable && input.TargetCommitted)
			|| input.AlliesCanBurst
			|| input.ObjectiveControlNeeded
			|| IsKillWindow(input);
	}

	internal static bool ShouldUseBlazingShot(MachinistPvPDecisionInput input)
	{
		if (!input.Target.IsInNormalRange || input.Target.HasGuard)
		{
			return false;
		}

		return IsKillWindow(input)
			|| input.FollowUpAvailable
			|| input.TargetCommitted;
	}

	private static bool CanApplyControl(MachinistPvPDecisionInput input)
	{
		return input.Target.IsInNormalRange
			&& !input.Target.HasGuard
			&& !input.Target.HasResilience;
	}

	private static bool IsKillWindow(MachinistPvPDecisionInput input)
	{
		if (input.Target.HealthRatio > BurstHealthRatio)
		{
			return false;
		}

		return input.Target.CurrentMp <= PvPScoringFactors.MediumMp
			|| input.Target.HasAllyFocus
			|| input.Target.IsVulnerable
			|| input.Target.IsObjectiveRelevant;
	}

	private static bool IsLikelyAlreadyDying(MachinistPvPDecisionInput input)
	{
		if (!input.Target.HasAllyFocus && !input.FollowUpAvailable)
		{
			return false;
		}

		if (input.Target.HealthRatio <= MarksmanCleanupHealthRatio)
		{
			return true;
		}

		return input.Target.HealthRatio <= MarksmanFocusedCleanupHealthRatio
			&& input.Target.HasAllyFocus
			&& input.FollowUpAvailable;
	}
}
