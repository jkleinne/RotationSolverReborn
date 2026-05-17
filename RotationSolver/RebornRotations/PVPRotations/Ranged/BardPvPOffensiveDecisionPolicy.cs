using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

internal readonly record struct BardPvPOffensiveDecisionInput(
	BardPvPTargetSnapshot Target,
	bool FollowUpAvailable,
	bool AlliesCanBurst,
	bool ObjectiveControlNeeded,
	bool TargetCommitted,
	bool HasFinalFantasia,
	bool HasFrontlinersMarch,
	bool HasRepertoire,
	bool HasBlastArrowReady,
	bool HarmonicWouldOvercap,
	bool ForcedExpiryWindow = false,
	bool PeelValueNeeded = false,
	double ExpectedDamageRatio = 0.0,
	bool HasGuardCooldownKnowledge = false);

internal static class BardPvPOffensiveDecisionPolicy
{
	private const float KillWindowHealthRatio = 0.65f;
	private const int MultiTargetValueCount = 2;

	internal static bool ShouldUseHarmonicArrow(BardPvPOffensiveDecisionInput input)
	{
		if (HasBlockedDamage(input))
		{
			return false;
		}

		if (CanDirectSecure(input))
		{
			return true;
		}

		return input.HarmonicWouldOvercap
			|| input.ForcedExpiryWindow
			|| HasCommittedFollowUpValue(input)
			|| HasDamagePriority(input)
			|| IsKillWindow(input);
	}

	internal static bool ShouldUsePitchPerfect(BardPvPOffensiveDecisionInput input)
	{
		if (!input.HasRepertoire || HasBlockedDamage(input))
		{
			return false;
		}

		if (CanDirectSecure(input))
		{
			return true;
		}

		return HasSplashValue(input)
			|| HasCommittedFollowUpValue(input)
			|| (input.FollowUpAvailable && input.Target.HasAllyFocus)
			|| (input.FollowUpAvailable && IsKillWindow(input))
			|| input.ForcedExpiryWindow;
	}

	internal static bool ShouldUseApexArrow(BardPvPOffensiveDecisionInput input)
	{
		if (input.HasBlastArrowReady || HasBlockedDamage(input))
		{
			return false;
		}

		if (CanDirectSecure(input))
		{
			return true;
		}

		return HasObjectiveLineValue(input)
			|| HasCommittedFollowUpValue(input)
			|| input.ForcedExpiryWindow
			|| (HasLineValue(input) && input.AlliesCanBurst)
			|| IsKillWindow(input);
	}

	internal static bool ShouldUseBlastArrow(BardPvPOffensiveDecisionInput input)
	{
		if (!input.HasBlastArrowReady || HasBlockedDamage(input))
		{
			return false;
		}

		if (CanDirectSecure(input))
		{
			return true;
		}

		var hasDisplacementValue = HasObjectiveDisplacementValue(input) || input.PeelValueNeeded;
		var hasNonDisplacementValue = HasLineValue(input) || HasCommittedFollowUpValue(input);
		if (input.Target.HasResilience && hasDisplacementValue && !hasNonDisplacementValue)
		{
			return false;
		}

		return hasDisplacementValue || hasNonDisplacementValue;
	}

	internal static bool ShouldUseEncoreOfLight(BardPvPOffensiveDecisionInput input)
	{
		if (!HasEncoreWindow(input) || HasBlockedDamage(input))
		{
			return false;
		}

		if (CanDirectSecure(input))
		{
			return true;
		}

		if (CanReactWithGuard(input) && !HasDamagePriority(input))
		{
			return false;
		}

		return HasLowMpConversionValue(input)
			|| HasSplashValue(input)
			|| HasCommittedFollowUpValue(input)
			|| input.ForcedExpiryWindow;
	}

	internal static bool ShouldUsePowerfulShot(BardPvPOffensiveDecisionInput input)
	{
		if (HasBlockedDamage(input))
		{
			return false;
		}

		if (CanDirectSecure(input))
		{
			return true;
		}

		return IsKillWindow(input)
			|| input.Target.HasAllyFocus
			|| input.Target.IsVulnerable
			|| input.ObjectiveControlNeeded
			|| input.TargetCommitted
			|| input.ForcedExpiryWindow;
	}

	private static bool HasBlockedDamage(BardPvPOffensiveDecisionInput input)
	{
		return !input.Target.IsInNormalRange || input.Target.HasGuard || input.Target.HasInvulnerability;
	}

	private static bool CanDirectSecure(BardPvPOffensiveDecisionInput input)
	{
		var expectedDamageRatio = GetExpectedDamageRatio(input);
		if (expectedDamageRatio <= 0.0)
		{
			return false;
		}

		var gateDecision = PvPDamageGate.Evaluate(new PvPDamageGateInput(
			Intent: PvPBurstIntent.Secure,
			EffectiveHpRatio: input.Target.EffectiveHealthRatio,
			ExpectedDamageRatio: expectedDamageRatio,
			ActiveDamageReduction: input.Target.ActiveDamageReduction,
			HasInvulnerability: input.Target.HasInvulnerability,
			HasPrioritySignal: true));

		return gateDecision == PvPBurstRecommendation.Secure;
	}

	private static double GetExpectedDamageRatio(BardPvPOffensiveDecisionInput input)
	{
		return input.ExpectedDamageRatio > 0.0
			? input.ExpectedDamageRatio
			: input.Target.ExpectedDamageRatio;
	}

	private static bool IsKillWindow(BardPvPOffensiveDecisionInput input)
	{
		if (input.Target.HealthRatio > KillWindowHealthRatio)
		{
			return false;
		}

		return input.Target.CurrentMp <= PvPScoringFactors.MediumMp
			|| input.Target.HasAllyFocus
			|| input.Target.IsVulnerable
			|| input.Target.IsObjectiveRelevant;
	}

	private static bool HasDamagePriority(BardPvPOffensiveDecisionInput input)
	{
		return input.AlliesCanBurst
			|| input.ObjectiveControlNeeded
			|| input.TargetCommitted
			|| input.Target.HasAllyFocus
			|| input.Target.IsVulnerable
			|| input.Target.IsObjectiveRelevant
			|| input.PeelValueNeeded
			|| input.ForcedExpiryWindow;
	}

	private static bool HasCommittedFollowUpValue(BardPvPOffensiveDecisionInput input)
	{
		return input.FollowUpAvailable && input.TargetCommitted;
	}

	private static bool HasLineValue(BardPvPOffensiveDecisionInput input)
	{
		return input.Target.LineTargetCount >= MultiTargetValueCount;
	}

	private static bool HasSplashValue(BardPvPOffensiveDecisionInput input)
	{
		return input.Target.SplashTargetCount >= MultiTargetValueCount;
	}

	private static bool HasObjectiveLineValue(BardPvPOffensiveDecisionInput input)
	{
		return HasLineValue(input) && (input.ObjectiveControlNeeded || input.Target.IsObjectiveRelevant);
	}

	private static bool HasObjectiveDisplacementValue(BardPvPOffensiveDecisionInput input)
	{
		return input.ObjectiveControlNeeded && input.Target.IsObjectiveRelevant;
	}

	private static bool HasEncoreWindow(BardPvPOffensiveDecisionInput input)
	{
		return input.HasFinalFantasia || input.HasFrontlinersMarch;
	}

	private static bool HasLowMpConversionValue(BardPvPOffensiveDecisionInput input)
	{
		return input.Target.CurrentMp <= PvPScoringFactors.LowMp;
	}

	private static bool CanReactWithGuard(BardPvPOffensiveDecisionInput input)
	{
		return input.HasGuardCooldownKnowledge
			&& input.Target.GuardAvailability is PvPGuardAvailability.Ready or PvPGuardAvailability.Unknown;
	}
}
