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

		return HasPitchPerfectSpendValue(input);
	}

	internal static bool ShouldUseApexArrow(BardPvPOffensiveDecisionInput input)
	{
		if (input.HasBlastArrowReady || HasHardBlockedDamage(input))
		{
			return false;
		}

		if (input.Target.HasGuard)
		{
			return HasGuardedApexPressureValue(input);
		}

		if (CanDirectSecure(input))
		{
			return true;
		}

		return HasApexPressureValue(input);
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

		if (CanReactWithGuard(input) && !HasEncorePrioritySignal(input))
		{
			return false;
		}

		return HasEncoreSpendValue(input);
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

	private static bool HasHardBlockedDamage(BardPvPOffensiveDecisionInput input)
	{
		return !input.Target.IsInNormalRange || input.Target.HasInvulnerability;
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

	private static bool HasPitchPerfectSpendValue(BardPvPOffensiveDecisionInput input)
	{
		return HasSplashValue(input)
			|| HasCommittedFollowUpValue(input)
			|| input.AlliesCanBurst
			|| HasLowMpConversionValue(input)
			|| input.Target.IsObjectiveRelevant
			|| input.Target.HasAllyFocus
			|| input.ForcedExpiryWindow
			|| IsKillWindow(input);
	}

	private static bool HasApexPressureValue(BardPvPOffensiveDecisionInput input)
	{
		return HasLineValue(input)
			|| HasObjectivePressureValue(input)
			|| input.AlliesCanBurst
			|| HasCommittedFollowUpValue(input)
			|| input.ForcedExpiryWindow
			|| IsKillWindow(input);
	}

	private static bool HasGuardedApexPressureValue(BardPvPOffensiveDecisionInput input)
	{
		return HasObjectivePressureValue(input) || input.ForcedExpiryWindow;
	}

	private static bool HasEncorePrioritySignal(BardPvPOffensiveDecisionInput input)
	{
		return HasDamagePriority(input) || input.HasFinalFantasia;
	}

	private static bool HasEncoreSpendValue(BardPvPOffensiveDecisionInput input)
	{
		return HasLowMpConversionValue(input)
			|| HasSplashValue(input)
			|| HasCommittedFollowUpValue(input)
			|| input.AlliesCanBurst
			|| input.HasFinalFantasia
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

	private static bool HasObjectivePressureValue(BardPvPOffensiveDecisionInput input)
	{
		return input.ObjectiveControlNeeded || input.Target.IsObjectiveRelevant;
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
