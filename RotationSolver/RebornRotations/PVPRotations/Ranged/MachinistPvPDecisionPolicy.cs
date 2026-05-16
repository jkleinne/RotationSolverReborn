using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

internal readonly record struct MachinistPvPDecisionInput(
	MachinistPvPTargetSnapshot Target,
	bool SafeCloseRange,
	bool FollowUpAvailable,
	bool AlliesCanBurst,
	bool ObjectiveControlNeeded,
	bool TargetCommitted,
	double ExpectedDamageRatio = 0.0);

internal static class MachinistPvPDecisionPolicy
{
	private const float DrillKillHealthRatio = 0.35f;
	private const float BurstHealthRatio = 0.65f;
	private const float MarksmanCleanupHealthRatio = 0.18f;
	private const float MarksmanFocusedCleanupHealthRatio = 0.25f;
	private const double MarksmanSecureSafetyMarginRatio = 0.01;
	private const double MarksmanConversionMaxLeftoverRatio = 0.04;

	internal static bool ShouldUseAnalysisDrill(MachinistPvPDecisionInput input)
	{
		if (!input.Target.IsInNormalRange || input.Target.HasInvulnerability)
		{
			return false;
		}

		if (CanDirectSecure(input, ignoresGuard: true))
		{
			return true;
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
		if (CanDirectSecure(input, ignoresGuard: false))
		{
			return true;
		}

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
		if (CanDirectSecure(input, ignoresGuard: false))
		{
			return true;
		}

		if (HasBlockedDamage(input))
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
			&& !HasBlockedDamage(input)
			&& (input.ObjectiveControlNeeded || input.Target.HasAllyFocus || input.Target.IsVulnerable);
	}

	internal static bool ShouldUseScattergun(MachinistPvPDecisionInput input)
	{
		if (!input.SafeCloseRange || !input.Target.IsInCloseRange)
		{
			return false;
		}

		if (input.Target.HasGuard || input.Target.HasInvulnerability)
		{
			return false;
		}

		if (input.Target.HasResilience && input.ObjectiveControlNeeded)
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
		if (!input.TargetCommitted || !input.FollowUpAvailable || HasBlockedDamage(input))
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
		if (!input.Target.IsInNormalRange)
		{
			return false;
		}

		if (!input.ObjectiveControlNeeded && IsLikelyAlreadyDying(input))
		{
			return false;
		}

		var gateDecision = PvPDamageGate.Evaluate(new PvPDamageGateInput(
			Intent: PvPBurstIntent.Burst,
			EffectiveHpRatio: input.Target.EffectiveHealthRatio,
			ExpectedDamageRatio: input.ExpectedDamageRatio,
			ActiveDamageReduction: input.Target.ActiveDamageReduction,
			HasInvulnerability: input.Target.HasGuard || input.Target.HasInvulnerability,
			HasPrioritySignal: HasDamagePriority(input)));
		if (gateDecision == PvPBurstRecommendation.Hold)
		{
			return false;
		}

		if (gateDecision == PvPBurstRecommendation.Secure)
		{
			return HasMarksmanSecureDamage(input) || HasMarksmanConversionSignal(input);
		}

		return HasMarksmanConversionSignal(input);
	}

	internal static bool ShouldUseFullMetalField(MachinistPvPDecisionInput input)
	{
		if (CanDirectSecure(input, ignoresGuard: false))
		{
			return true;
		}

		if (HasBlockedDamage(input))
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
		if (CanDirectSecure(input, ignoresGuard: false))
		{
			return true;
		}

		if (HasBlockedDamage(input))
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
			&& !input.Target.HasInvulnerability
			&& !input.Target.HasResilience;
	}

	private static bool HasBlockedDamage(MachinistPvPDecisionInput input)
	{
		return !input.Target.IsInNormalRange || input.Target.HasGuard || input.Target.HasInvulnerability;
	}

	private static bool HasDamagePriority(MachinistPvPDecisionInput input)
	{
		return input.FollowUpAvailable
			|| input.AlliesCanBurst
			|| input.ObjectiveControlNeeded
			|| input.TargetCommitted
			|| input.Target.HasAllyFocus
			|| input.Target.IsVulnerable;
	}

	private static bool HasMarksmanConversionSignal(MachinistPvPDecisionInput input)
	{
		if (!HasMarksmanConvertibleLeftover(input))
		{
			return false;
		}

		if (!input.Target.HasAllyFocus)
		{
			return false;
		}

		return input.FollowUpAvailable
			|| input.AlliesCanBurst
			|| input.ObjectiveControlNeeded
			|| input.Target.IsVulnerable;
	}

	private static bool HasMarksmanSecureDamage(MachinistPvPDecisionInput input)
	{
		return input.Target.EffectiveHealthRatio + MarksmanSecureSafetyMarginRatio <= input.ExpectedDamageRatio;
	}

	private static bool HasMarksmanConvertibleLeftover(MachinistPvPDecisionInput input)
	{
		return input.Target.EffectiveHealthRatio - input.ExpectedDamageRatio <= MarksmanConversionMaxLeftoverRatio;
	}

	private static bool CanDirectSecure(MachinistPvPDecisionInput input, bool ignoresGuard)
	{
		if (!input.Target.IsInNormalRange || input.Target.ExpectedDamageRatio <= 0.0)
		{
			return false;
		}

		var blocksDamage = input.Target.HasInvulnerability || (input.Target.HasGuard && !ignoresGuard);
		var effectiveHealthRatio = input.Target.HasGuard && ignoresGuard
			? input.Target.HealthRatio
			: input.Target.EffectiveHealthRatio;
		var gateDecision = PvPDamageGate.Evaluate(new PvPDamageGateInput(
			Intent: PvPBurstIntent.Secure,
			EffectiveHpRatio: effectiveHealthRatio,
			ExpectedDamageRatio: input.Target.ExpectedDamageRatio,
			ActiveDamageReduction: input.Target.ActiveDamageReduction,
			HasInvulnerability: blocksDamage,
			HasPrioritySignal: HasDamagePriority(input)));

		return gateDecision == PvPBurstRecommendation.Secure;
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
