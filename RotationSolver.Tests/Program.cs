using System.Text.Json;
using RotationSolver.Commands;
using RotationSolver.Basic.Actions.PvPTargetSelection;
using RotationSolver.RebornRotations.PVPRotations.Ranged;

var tests = new (string Name, Action Test)[]
{
	("after-combat cancel is cleared by territory change", AfterCombatCancelIsClearedByTerritoryChange),
	("after-combat cancel does not fire after combat restarts", AfterCombatCancelDoesNotFireAfterCombatRestarts),
	("after-combat cancel fires only after expiry in same context", AfterCombatCancelFiresOnlyAfterExpiryInSameContext),
	("countdown-owned state continues into combat", CountdownOwnedStateContinuesIntoCombat),
	("countdown cleanup does not cancel user-owned state", CountdownCleanupDoesNotCancelUserOwnedState),
	("countdown cleanup cancels owned state without combat", CountdownCleanupCancelsOwnedStateWithoutCombat),
	("disabled countdown clears ownership", DisabledCountdownClearsOwnership),
	("pvp smart default preset is ranked", PvPSmartDefaultPresetIsRanked),
	("pvp smart default weights match ranked", PvPSmartDefaultWeightsMatchRanked),
	("legacy custom pvp weights fill new control defaults", LegacyCustomPvPWeightsFillNewControlDefaults),
	("legacy casual pvp weights are detected", LegacyCasualPvPWeightsAreDetected),
	("legacy default pvp weights migrate to ranked defaults", LegacyDefaultPvPWeightsMigrateToRankedDefaults),
	("legacy pvp scoring config migrates default preset and weights", LegacyPvPScoringConfigMigratesDefaultPresetAndWeights),
	("mp pressure scores low and medium mp", MpPressureScoresLowAndMediumMp),
	("objective pressure scores known objective target", ObjectivePressureScoresKnownObjectiveTarget),
	("resilience penalty scores boolean signal", ResiliencePenaltyScoresBooleanSignal),
	("silent nocturne rejects filler use", SilentNocturneRejectsFillerUse),
	("silent nocturne rejects resilient target", SilentNocturneRejectsResilientTarget),
	("silent nocturne accepts casting shutdown", SilentNocturneAcceptsCastingShutdown),
	("repelling rejects unsafe backstep", RepellingRejectsUnsafeBackstep),
	("repelling rejects resilient target", RepellingRejectsResilientTarget),
	("repelling accepts safe peel", RepellingAcceptsSafePeel),
	("protective paean rejects healthy unfocused ally", ProtectivePaeanRejectsHealthyUnfocusedAlly),
	("protective paean allows focused ally", ProtectivePaeanAllowsFocusedAlly),
	("machinist target policy prefers killable low resource target", MachinistTargetPolicyPrefersKillableLowResourceTarget),
	("machinist target policy allows guarded drill punish", MachinistTargetPolicyAllowsGuardedDrillPunish),
	("machinist analysis drill rejects full resource target", MachinistAnalysisDrillRejectsFullResourceTarget),
	("machinist analysis air anchor rejects resilient target", MachinistAnalysisAirAnchorRejectsResilientTarget),
	("machinist analysis air anchor rejects isolated setup", MachinistAnalysisAirAnchorRejectsIsolatedSetup),
	("machinist analysis chain saw requires follow up", MachinistAnalysisChainSawRequiresFollowUp),
	("machinist scattergun rejects unsafe close range", MachinistScattergunRejectsUnsafeCloseRange),
	("machinist wildfire requires committed target and follow up", MachinistWildfireRequiresCommittedTargetAndFollowUp),
	("machinist bishop accepts objective teamfight", MachinistBishopAcceptsObjectiveTeamfight),
	("machinist bishop rejects out of range targets", MachinistBishopRejectsOutOfRangeTargets),
	("machinist marksmans spite rejects guard", MachinistMarksmanSpiteRejectsGuard),
	("machinist marksmans spite accepts low mp kill window", MachinistMarksmanSpiteAcceptsLowMpKillWindow),
	("machinist marksmans spite accepts vulnerable target", MachinistMarksmanSpiteAcceptsVulnerableTarget),
	("machinist full metal rejects uncommitted follow up", MachinistFullMetalRejectsUncommittedFollowUp),
	("pvp lb json contains verified entries", PvpLbJsonContainsVerifiedEntries),
	("pvp mitigation json contains resilience", PvpMitigationJsonContainsResilience),
};

var failures = new List<string>();
foreach (var (name, test) in tests)
{
	try
	{
		test();
	}
	catch (Exception ex)
	{
		failures.Add($"{name}: {ex.Message}");
	}
}

if (failures.Count > 0)
{
	foreach (var failure in failures)
	{
		Console.Error.WriteLine(failure);
	}

	Environment.Exit(1);
}

Console.WriteLine($"Passed {tests.Length} tests.");

static void AfterCombatCancelIsClearedByTerritoryChange()
{
	var now = new DateTime(2026, 5, 13, 12, 0, 0, DateTimeKind.Utc);
	var pendingCancelTime = now.AddSeconds(30);

	var shouldClear = AutoOffPolicy.ShouldClearPendingAfterCombatCancel(
		pendingCancelTime,
		isStateEnabled: true,
		isInCombat: false,
		didTerritoryChange: true);

	AssertTrue(shouldClear, "territory changes must invalidate pending after-combat cancels");
}

static void AfterCombatCancelDoesNotFireAfterCombatRestarts()
{
	var now = new DateTime(2026, 5, 13, 12, 0, 0, DateTimeKind.Utc);
	var expiredCancelTime = now.AddSeconds(-1);

	var shouldCancel = AutoOffPolicy.ShouldCancelForPendingAfterCombat(
		expiredCancelTime,
		now,
		isStateEnabled: true,
		isInCombat: true);

	AssertFalse(shouldCancel, "combat restart must prevent stale after-combat cancellation");
}

static void AfterCombatCancelFiresOnlyAfterExpiryInSameContext()
{
	var now = new DateTime(2026, 5, 13, 12, 0, 0, DateTimeKind.Utc);
	var futureCancelTime = now.AddSeconds(1);
	var expiredCancelTime = now.AddSeconds(-1);

	AssertFalse(
		AutoOffPolicy.ShouldCancelForPendingAfterCombat(
			futureCancelTime,
			now,
			isStateEnabled: true,
			isInCombat: false),
		"pending after-combat cancel must wait until its expiry");

	AssertTrue(
		AutoOffPolicy.ShouldCancelForPendingAfterCombat(
			expiredCancelTime,
			now,
			isStateEnabled: true,
			isInCombat: false),
		"expired after-combat cancel should fire when context still matches");
}

static void CountdownOwnedStateContinuesIntoCombat()
{
	var state = AutoOffPolicy.CountdownAutoState.None;
	var started = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: true,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 10f,
		isStateEnabled: false,
		isInCombat: false,
		countdownStartsManualMode: false,
		state);

	AssertTrue(started.ShouldStartState, "countdown should start rotation when state is off");
	AssertTrue(started.NextState.OwnsActiveState, "countdown should own state it started");

	var completed = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: true,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 0f,
		isStateEnabled: true,
		isInCombat: true,
		countdownStartsManualMode: false,
		started.NextState);

	AssertFalse(completed.ShouldCancelState, "countdown completion must not cancel state after combat starts");
	AssertFalse(completed.NextState.OwnsActiveState, "countdown ownership should be released after pull starts");
}

static void CountdownCleanupDoesNotCancelUserOwnedState()
{
	var userOwnedState = new AutoOffPolicy.CountdownAutoState(
		LastCountdownTime: 10f,
		OwnsActiveState: false);

	var decision = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: true,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 0f,
		isStateEnabled: true,
		isInCombat: false,
		countdownStartsManualMode: false,
		userOwnedState);

	AssertFalse(decision.ShouldCancelState, "countdown cleanup must not cancel user-owned Auto mode");
}

static void CountdownCleanupCancelsOwnedStateWithoutCombat()
{
	var countdownOwnedState = new AutoOffPolicy.CountdownAutoState(
		LastCountdownTime: 10f,
		OwnsActiveState: true);

	var decision = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: true,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 0f,
		isStateEnabled: true,
		isInCombat: false,
		countdownStartsManualMode: false,
		countdownOwnedState);

	AssertTrue(decision.ShouldCancelState, "countdown cleanup should cancel state it started if combat never begins");
}

static void DisabledCountdownClearsOwnership()
{
	var countdownOwnedState = new AutoOffPolicy.CountdownAutoState(
		LastCountdownTime: 10f,
		OwnsActiveState: true);

	var decision = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: false,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 0f,
		isStateEnabled: true,
		isInCombat: false,
		countdownStartsManualMode: false,
		countdownOwnedState);

	AssertFalse(decision.ShouldCancelState, "disabled countdown handling must not cancel state");
	AssertFalse(decision.NextState.OwnsActiveState, "disabled countdown handling should clear ownership");
}

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

static void SilentNocturneRejectsFillerUse()
{
	var input = new BardPvPShutdownInput(
		TargetHasResilience: false,
		TargetIsCasting: false,
		TargetThreatensFragileAlly: false,
		TargetIsBurstWorthy: false,
		TargetHasLowMp: false,
		TargetHealthRatio: 1f,
		TargetDistance: 20f,
		SafeBackstepExists: true,
		ObjectiveControlNeeded: false);

	AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input), "Nocturne should not be filler");
}

static void SilentNocturneAcceptsCastingShutdown()
{
	var input = new BardPvPShutdownInput(
		TargetHasResilience: false,
		TargetIsCasting: true,
		TargetThreatensFragileAlly: false,
		TargetIsBurstWorthy: false,
		TargetHasLowMp: false,
		TargetHealthRatio: 1f,
		TargetDistance: 20f,
		SafeBackstepExists: true,
		ObjectiveControlNeeded: false);

	AssertTrue(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input), "Nocturne should interrupt high-value casts");
}

static void SilentNocturneRejectsResilientTarget()
{
	var input = new BardPvPShutdownInput(
		TargetHasResilience: true,
		TargetIsCasting: true,
		TargetThreatensFragileAlly: true,
		TargetIsBurstWorthy: true,
		TargetHasLowMp: true,
		TargetHealthRatio: 0.20f,
		TargetDistance: 8f,
		SafeBackstepExists: true,
		ObjectiveControlNeeded: true);

	AssertFalse(BardPvPDecisionPolicy.ShouldUseSilentNocturne(input), "Nocturne should reject Resilience");
}

static void RepellingRejectsUnsafeBackstep()
{
	var input = new BardPvPShutdownInput(
		TargetHasResilience: false,
		TargetIsCasting: false,
		TargetThreatensFragileAlly: true,
		TargetIsBurstWorthy: false,
		TargetHasLowMp: false,
		TargetHealthRatio: 1f,
		TargetDistance: 8f,
		SafeBackstepExists: false,
		ObjectiveControlNeeded: false);

	AssertFalse(BardPvPDecisionPolicy.ShouldUseRepellingShot(input), "Repelling should reject unsafe backsteps");
}

static void RepellingRejectsResilientTarget()
{
	var input = new BardPvPShutdownInput(
		TargetHasResilience: true,
		TargetIsCasting: false,
		TargetThreatensFragileAlly: true,
		TargetIsBurstWorthy: true,
		TargetHasLowMp: true,
		TargetHealthRatio: 0.20f,
		TargetDistance: 8f,
		SafeBackstepExists: true,
		ObjectiveControlNeeded: true);

	AssertFalse(BardPvPDecisionPolicy.ShouldUseRepellingShot(input), "Repelling should reject Resilience");
}

static void RepellingAcceptsSafePeel()
{
	var input = new BardPvPShutdownInput(
		TargetHasResilience: false,
		TargetIsCasting: false,
		TargetThreatensFragileAlly: true,
		TargetIsBurstWorthy: false,
		TargetHasLowMp: false,
		TargetHealthRatio: 1f,
		TargetDistance: 8f,
		SafeBackstepExists: true,
		ObjectiveControlNeeded: false);

	AssertTrue(BardPvPDecisionPolicy.ShouldUseRepellingShot(input), "Repelling should peel safe short-range divers");
}

static void ProtectivePaeanRejectsHealthyUnfocusedAlly()
{
	AssertFalse(
		BardPvPDecisionPolicy.ShouldUseProtectivePaean(0.90f, 0),
		"healthy unfocused ally should not receive fake-shield Paean");
}

static void ProtectivePaeanAllowsFocusedAlly()
{
	AssertTrue(
		BardPvPDecisionPolicy.ShouldUseProtectivePaean(0.60f, 1),
		"focused ally near pressure threshold should receive fake-shield Paean");
}

static void MachinistTargetPolicyPrefersKillableLowResourceTarget()
{
	var highResourceTarget = MachinistTarget(1, healthRatio: 0.40f, currentMp: 10_000);
	var lowResourceTarget = MachinistTarget(2, healthRatio: 0.40f, currentMp: 2_000);

	var selected = MachinistPvPTargetPolicy.SelectBest(
		[highResourceTarget, lowResourceTarget],
		MachinistPvPActionIntent.MarksmanSpite);

	AssertEqual(2UL, selected?.TargetId, "MCH should prefer the target that cannot answer with repeated Recuperates");
}

static void MachinistTargetPolicyAllowsGuardedDrillPunish()
{
	var guardedLowTarget = MachinistTarget(
		1,
		healthRatio: 0.25f,
		currentMp: 2_000,
		hasGuard: true);
	var exposedHealthyTarget = MachinistTarget(2, healthRatio: 0.70f, currentMp: 2_000);

	var selected = MachinistPvPTargetPolicy.SelectBest(
		[guardedLowTarget, exposedHealthyTarget],
		MachinistPvPActionIntent.AnalysisDrill);

	AssertEqual(1UL, selected?.TargetId, "Analysis Drill should be allowed to punish low HP Guard");
}

static void MachinistAnalysisDrillRejectsFullResourceTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.90f, currentMp: 10_000),
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: false);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseAnalysisDrill(input), "Analysis Drill should not pad into full-resource targets");
}

static void MachinistAnalysisAirAnchorRejectsResilientTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.30f, currentMp: 2_000, hasResilience: true),
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: true,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseAnalysisAirAnchor(input), "Analysis Air Anchor should reject Resilience when stun value matters");
}

static void MachinistAnalysisAirAnchorRejectsIsolatedSetup()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.90f, currentMp: 10_000),
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseAnalysisAirAnchor(input), "Analysis Air Anchor should not spend stun on an isolated durable target");
}

static void MachinistAnalysisChainSawRequiresFollowUp()
{
	var withoutFollowUp = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.50f, currentMp: 2_000),
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);
	var withFollowUp = withoutFollowUp with
	{
		FollowUpAvailable = true,
		AlliesCanBurst = true,
	};

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseAnalysisChainSaw(withoutFollowUp), "Analysis Chain Saw should not mark targets without follow-up");
	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseAnalysisChainSaw(withFollowUp), "Analysis Chain Saw should set up burst when allies can hit");
}

static void MachinistScattergunRejectsUnsafeCloseRange()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.25f, currentMp: 2_000, isInCloseRange: true),
		SafeCloseRange: false,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: true,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseScattergun(input), "Scattergun should reject unsafe 12y commits");
}

static void MachinistWildfireRequiresCommittedTargetAndFollowUp()
{
	var looseTarget = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.45f, currentMp: 2_000),
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: false);
	var committedTarget = looseTarget with { TargetCommitted = true };

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseWildfire(looseTarget), "Wildfire should reject targets that can leave before detonation");
	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseWildfire(committedTarget), "Wildfire should accept committed targets with follow-up");
}

static void MachinistBishopAcceptsObjectiveTeamfight()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.80f, currentMp: 10_000, isObjectiveRelevant: true),
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: true,
		TargetCommitted: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseBishop(input), "Bishop should be used for objective teamfights");
}

static void MachinistBishopRejectsOutOfRangeTargets()
{
	var inRange = MachinistTarget(1, healthRatio: 0.80f, currentMp: 10_000, isInNormalRange: true);
	var outOfRange = MachinistTarget(2, healthRatio: 0.10f, currentMp: 0, isInNormalRange: false);
	var rankedTargets = MachinistPvPTargetPolicy.Rank(
		[inRange, outOfRange],
		MachinistPvPActionIntent.Bishop);

	AssertEqual(1, rankedTargets.Count, "Bishop targeting should keep only reachable targets");
	AssertEqual(1UL, rankedTargets[0].TargetId, "Bishop targeting should choose the reachable target");
}

static void MachinistMarksmanSpiteRejectsGuard()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.10f, currentMp: 0, hasGuard: true),
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: true,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not be modeled as Guard piercing");
}

static void MachinistMarksmanSpiteAcceptsLowMpKillWindow()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.55f, currentMp: 2_000),
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should convert low MP kill windows");
}

static void MachinistMarksmanSpiteAcceptsVulnerableTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.80f, currentMp: 10_000, isVulnerable: true),
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should accept vulnerable targets even above normal HP cutoff");
}

static void MachinistFullMetalRejectsUncommittedFollowUp()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.90f, currentMp: 10_000),
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: false);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseFullMetalField(input), "Full Metal Field should not spend burst on an uncommitted durable target");
}

static MachinistPvPTargetSnapshot MachinistTarget(
	ulong targetId,
	float healthRatio,
	uint currentMp,
	bool hasGuard = false,
	bool hasResilience = false,
	bool isObjectiveRelevant = false,
	bool hasAllyFocus = false,
	bool isVulnerable = false,
	bool isExposed = true,
	bool isInNormalRange = true,
	bool isInCloseRange = false)
{
	return new MachinistPvPTargetSnapshot(
		TargetId: targetId,
		HealthRatio: healthRatio,
		CurrentMp: currentMp,
		HasGuard: hasGuard,
		HasResilience: hasResilience,
		IsObjectiveRelevant: isObjectiveRelevant,
		HasAllyFocus: hasAllyFocus,
		IsVulnerable: isVulnerable,
		IsExposed: isExposed,
		IsInNormalRange: isInNormalRange,
		IsInCloseRange: isInCloseRange);
}

static void PvpLbJsonContainsVerifiedEntries()
{
	using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("RotationSolver.Basic", "Data", "PvPLBs.json")));
	var root = document.RootElement;
	var expectedEntries = new Dictionary<uint, (string Category, string Description)>
	{
		[29069] = ("Utility", "PLD Phalanx"),
		[29083] = ("Utility", "WAR Primal Scream"),
		[29097] = ("Offensive", "DRK Eventide"),
		[29130] = ("Offensive", "GNB Relentless Rush"),
		[29485] = ("Offensive", "MNK Meteodrive"),
		[29497] = ("Offensive", "DRG Sky High"),
		[29515] = ("Offensive", "NIN Seiton Tenchu"),
		[29537] = ("Offensive", "SAM Zantetsuken"),
		[29553] = ("Utility", "RPR Tenebrae Lemurum"),
		[39190] = ("Offensive", "VPR World-swallower"),
		[29401] = ("Utility", "BRD Final Fantasia"),
		[29415] = ("Offensive", "MCH Marksman's Spite"),
		[29432] = ("Utility", "DNC Contradance"),
		[29662] = ("Utility", "BLM Soul Resonance"),
		[29673] = ("Offensive", "SMN Summon Bahamut"),
		[41498] = ("Offensive", "RDM Southern Cross"),
		[39215] = ("Utility", "PCT Advent of Chocobastion"),
		[29230] = ("Healing", "WHM Afflatus Purgation"),
		[41502] = ("Healing", "SCH Seraphism"),
		[29255] = ("Healing", "AST Celestial River"),
		[29266] = ("Healing", "SGE Mesotes"),
	};
	var seenActionIds = new HashSet<uint>();

	AssertEqual(JsonValueKind.Array, root.ValueKind, "PvPLBs.json should be an array");
	AssertEqual(expectedEntries.Count, root.GetArrayLength(), "PvPLBs.json should contain the verified PvP LB entries");

	foreach (var entry in root.EnumerateArray())
	{
		var actionId = GetRequiredUInt(entry, "ActionId");
		AssertTrue(seenActionIds.Add(actionId), $"PvP LB action id {actionId} should be unique");
		AssertTrue(expectedEntries.TryGetValue(actionId, out var expected), $"PvP LB action id {actionId} should be verified");
		AssertEqual(expected.Category, GetRequiredString(entry, "Category"), $"PvP LB action id {actionId} should have verified category");
		AssertEqual(expected.Description, GetRequiredString(entry, "Description"), $"PvP LB action id {actionId} should have verified description");
	}
}

static void PvpMitigationJsonContainsResilience()
{
	using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("RotationSolver.Basic", "Data", "PvPMitigations.json")));
	var root = document.RootElement;
	var seenIds = new HashSet<string>();

	AssertEqual(JsonValueKind.Array, root.ValueKind, "PvPMitigations.json should be an array");

	foreach (var entry in root.EnumerateArray())
	{
		var id = GetRequiredString(entry, "Id");
		AssertTrue(seenIds.Add(id), $"PvP mitigation id {id} should be unique");

		if (id != "Resilience")
		{
			continue;
		}

		AssertEqual("HeavyDR", GetRequiredString(entry, "Kind"), "Resilience should be modeled as control protection");
		AssertEqual(0.0, GetRequiredDouble(entry, "DamageReductionPercent"), "Resilience should not add damage reduction");
		return;
	}

	throw new InvalidOperationException("PvPMitigations.json should represent Resilience as non-invulnerability control protection");
}

static string RepositoryPath(params string[] parts)
{
	var root = FindRepositoryRoot();
	var segments = new string[parts.Length + 1];
	segments[0] = root;
	Array.Copy(parts, 0, segments, 1, parts.Length);
	return Path.Combine(segments);
}

static string FindRepositoryRoot()
{
	var directory = new DirectoryInfo(AppContext.BaseDirectory);
	while (directory != null)
	{
		var gitPath = Path.Combine(directory.FullName, ".git");
		if (Directory.Exists(gitPath) || File.Exists(gitPath))
		{
			return directory.FullName;
		}

		directory = directory.Parent;
	}

	throw new InvalidOperationException("Could not locate repository root");
}

static string GetRequiredString(JsonElement element, string propertyName)
{
	if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
	{
		throw new InvalidOperationException($"JSON entry should contain string property {propertyName}");
	}

	return property.GetString() ?? string.Empty;
}

static uint GetRequiredUInt(JsonElement element, string propertyName)
{
	if (!element.TryGetProperty(propertyName, out var property) || !property.TryGetUInt32(out var value))
	{
		throw new InvalidOperationException($"JSON entry should contain unsigned integer property {propertyName}");
	}

	return value;
}

static double GetRequiredDouble(JsonElement element, string propertyName)
{
	if (!element.TryGetProperty(propertyName, out var property) || !property.TryGetDouble(out var value))
	{
		throw new InvalidOperationException($"JSON entry should contain numeric property {propertyName}");
	}

	return value;
}

static void AssertTrue(bool actual, string message)
{
	if (!actual)
	{
		throw new InvalidOperationException(message);
	}
}

static void AssertEqual<T>(T expected, T actual, string message)
{
	if (!EqualityComparer<T>.Default.Equals(expected, actual))
	{
		throw new InvalidOperationException($"{message}. Expected {expected}, got {actual}");
	}
}

static void AssertFalse(bool actual, string message)
{
	if (actual)
	{
		throw new InvalidOperationException(message);
	}
}
