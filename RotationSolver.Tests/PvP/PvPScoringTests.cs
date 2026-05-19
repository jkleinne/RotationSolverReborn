using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	static void PvPSmartDefaultPresetIsRanked()
	{
		AssertEqual(ScoringPreset.Ranked, ScoringWeights.DefaultPreset, "PvPSmart should default to Ranked for Ranked CC Bard tuning");
	}

	static void PvPSmartDefaultWeightsMatchRanked()
	{
		var expected = ScoringWeights.ForPreset(ScoringPreset.Ranked);

		AssertEqual(expected, ScoringWeights.DefaultWeights, "PvPSmart default weights should match Ranked weights");
	}

	static void LegacyCustomPvPWeightsFillNewControlDefaults()
	{
		var legacyCustomWeights = LegacyTunedCustomWeights();
		var migrated = ScoringWeights.MigrateLegacyCustomWeights(legacyCustomWeights);
		var expected = new ScoringWeights(
			RoleWeight: 1.25,
			FinishWeight: 1.40,
			MitigationPenaltyWeight: 1.30,
			DistancePenaltyWeight: 0.20,
			StickyBonus: 0.08,
			CarrierWeight: 0.75,
			LBWeight: 1.20,
			IsolationWeight: 0.35,
			ThreatWeight: 0.55,
			MpPressureWeight: 0.40,
			ResiliencePenaltyWeight: 0.50,
			ObjectiveWeight: 0.50);

		AssertEqual(expected, migrated, "legacy custom migration should preserve old weights and seed new control weights");
	}

	static void LegacyCasualPvPWeightsAreDetected()
	{
		var legacyDefault = LegacyCasualWeights();

		AssertTrue(ScoringWeights.IsLegacyCasualDefault(legacyDefault), "legacy Casual default should be detected for config migration");
		AssertFalse(ScoringWeights.IsLegacyCasualDefault(ScoringWeights.ForPreset(ScoringPreset.Casual)), "current Casual default should not be treated as legacy");
	}

	static void LegacyDefaultPvPWeightsMigrateToRankedDefaults()
	{
		var migrated = ScoringWeights.MigrateLegacyDefaultWeights(LegacyCasualWeights());

		AssertEqual(ScoringWeights.DefaultWeights, migrated, "legacy default PvP weights should migrate to Ranked defaults");
	}

	static void LegacyPvPScoringConfigMigratesDefaultPresetAndWeights()
	{
		var migrated = ScoringWeights.MigrateLegacyConfig(ScoringPreset.Casual, LegacyCasualWeights());

		AssertEqual(ScoringPreset.Ranked, migrated.Preset, "legacy Casual preset should migrate to Ranked");
		AssertEqual(ScoringWeights.DefaultWeights, migrated.Weights, "legacy Casual backing weights should migrate to Ranked defaults");
	}

	static ScoringWeights LegacyCasualWeights()
	{
		return new ScoringWeights(
			RoleWeight: 1.00,
			FinishWeight: 1.00,
			MitigationPenaltyWeight: 1.00,
			DistancePenaltyWeight: 0.10,
			StickyBonus: 0.05,
			CarrierWeight: 0.50,
			LBWeight: 1.00,
			IsolationWeight: 0.25,
			ThreatWeight: 0.40,
			MpPressureWeight: 0.0,
			ResiliencePenaltyWeight: 0.0,
			ObjectiveWeight: 0.0);
	}

	static ScoringWeights LegacyTunedCustomWeights()
	{
		return new ScoringWeights(
			RoleWeight: 1.25,
			FinishWeight: 1.40,
			MitigationPenaltyWeight: 1.30,
			DistancePenaltyWeight: 0.20,
			StickyBonus: 0.08,
			CarrierWeight: 0.75,
			LBWeight: 1.20,
			IsolationWeight: 0.35,
			ThreatWeight: 0.55,
			MpPressureWeight: 0.0,
			ResiliencePenaltyWeight: 0.0,
			ObjectiveWeight: 0.0);
	}

	static void MpPressureScoresLowAndMediumMp()
	{
		AssertEqual(1.0, PvPScoringFactors.ComputeMpPressure(2_000), "low MP should be highest pressure");
		AssertEqual(0.5, PvPScoringFactors.ComputeMpPressure(4_000), "medium MP should be partial pressure");
		AssertEqual(0.0, PvPScoringFactors.ComputeMpPressure(6_000), "high MP should not add pressure");
	}

	static void ObjectivePressureScoresKnownObjectiveTarget()
	{
		var targetId = 42UL;
		var ids = new HashSet<ulong> { targetId };

		AssertEqual(1.0, PvPScoringFactors.ComputeObjectivePressure(targetId, ids), "objective target should score");
		AssertEqual(0.0, PvPScoringFactors.ComputeObjectivePressure(99UL, ids), "unlisted target should not score");
	}

	static void ResiliencePenaltyScoresBooleanSignal()
	{
		AssertEqual(1.0, PvPScoringFactors.ComputeResiliencePenalty(true), "resilience should score as a penalty");
		AssertEqual(0.0, PvPScoringFactors.ComputeResiliencePenalty(false), "no resilience should not penalize");
	}

	static void PvpDamageGateRejectsInvulnerability()
	{
		var decision = PvPDamageGate.Evaluate(new PvPDamageGateInput(
			Intent: PvPBurstIntent.Secure,
			EffectiveHpRatio: double.PositiveInfinity,
			ExpectedDamageRatio: 1.00,
			ActiveDamageReduction: 0.99,
			HasInvulnerability: true,
			HasPrioritySignal: true));

		AssertEqual(PvPBurstRecommendation.Hold, decision, "damage gate should never spend into active invulnerability");
	}

	static void PvpDamageGateAllowsMitigatedSecureKill()
	{
		var decision = PvPDamageGate.Evaluate(new PvPDamageGateInput(
			Intent: PvPBurstIntent.Secure,
			EffectiveHpRatio: 0.62,
			ExpectedDamageRatio: 0.67,
			ActiveDamageReduction: 0.25,
			HasInvulnerability: false,
			HasPrioritySignal: false));

		AssertEqual(PvPBurstRecommendation.Secure, decision, "damage gate should allow mitigation when expected damage still kills");
	}

	static void PvpFinalGuardGateBlocksStaleGuardedTarget()
	{
		var input = new PvPActionUseGuardInput(
			IsPvP: true,
			IsHostileAction: true,
			IgnoresGuard: false,
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: false);

		AssertTrue(PvPActionUseGuard.ShouldBlock(input), "final action use should recheck Guard after target selection");
	}

	static void PvpFinalGuardGateAllowsGuardPiercingAction()
	{
		var input = new PvPActionUseGuardInput(
			IsPvP: true,
			IsHostileAction: true,
			IgnoresGuard: true,
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: false);

		AssertFalse(PvPActionUseGuard.ShouldBlock(input), "final action use should not block actions that ignore Guard");
	}

	static void PvpFinalGuardGateAllowsExpiringGuard()
	{
		var input = new PvPActionUseGuardInput(
			IsPvP: true,
			IsHostileAction: true,
			IgnoresGuard: false,
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: true);

		AssertFalse(PvPActionUseGuard.ShouldBlock(input), "final action use should allow targets whose Guard expires before resolution");
	}

	static void PvpFinalGuardGateAllowsNonhostileAction()
	{
		var input = new PvPActionUseGuardInput(
			IsPvP: true,
			IsHostileAction: false,
			IgnoresGuard: false,
			TargetHasGuard: true,
			GuardWillExpireBeforeAction: false);

		AssertFalse(PvPActionUseGuard.ShouldBlock(input), "final action use should not block self or friendly actions because the target has Guard");
	}

	static void PvpGuardCooldownTrackerBackdatesObservedGuard()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.FromSeconds(10),
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(2.5)));

		AssertEqual(
			PvPGuardAvailability.CoolingDown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(38), TimeSpan.Zero),
			"observed Guard should backdate use time from remaining duration");
		AssertEqual(
			PvPGuardAvailability.Ready,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(38.6), TimeSpan.Zero),
			"Guard should become ready 30 seconds after inferred activation");
	}

	static void PvpGuardCooldownTrackerKeepsCooldownAfterEarlyCancel()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.Zero,
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(4)));
		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.FromSeconds(1),
			HasGuard: false,
			GuardRemaining: TimeSpan.Zero));

		AssertEqual(
			PvPGuardAvailability.CoolingDown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(10), TimeSpan.Zero),
			"canceling Guard early should not make Guard available before the recast finishes");
		AssertEqual(
			PvPGuardAvailability.Ready,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(30.1), TimeSpan.Zero),
			"Guard should be ready after its recast from activation");
	}

	static void PvpGuardCooldownTrackerRequiresSafeUnavailableWindow()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.Zero,
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(4)));

		AssertEqual(
			PvPGuardAvailability.CoolingDown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(28.5), TimeSpan.FromSeconds(1)),
			"Guard should count as unavailable when it remains down through the required commit window");
		AssertEqual(
			PvPGuardAvailability.Ready,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(29.2), TimeSpan.FromSeconds(1)),
			"Guard should count as ready when it returns during the required commit window");
	}

	static void PvpGuardCooldownTrackerForgetsStaleUnseenTargets()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.Zero,
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(4)));
		tracker.ForgetUnseen(TimeSpan.FromSeconds(8), new HashSet<ulong> { 20 }, TimeSpan.FromSeconds(5));

		AssertEqual(
			PvPGuardAvailability.Unknown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(8), TimeSpan.Zero),
			"stale unseen targets should become unknown because they may have used Guard out of sight");
	}

	static void PvpGuardCooldownTrackerForgetsTarget()
	{
		var tracker = new PvPGuardCooldownTracker();

		tracker.Observe(new PvPGuardCooldownObservation(
			TargetId: 10,
			ObservedAt: TimeSpan.Zero,
			HasGuard: true,
			GuardRemaining: TimeSpan.FromSeconds(4)));
		tracker.Forget(10);

		AssertEqual(
			PvPGuardAvailability.Unknown,
			tracker.GetAvailability(10, TimeSpan.FromSeconds(1), TimeSpan.Zero),
			"death or match reset should clear a target's inferred Guard cooldown");
	}
}
