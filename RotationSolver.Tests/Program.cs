using System.Text.Json;
using RotationSolver.Commands;
using RotationSolver.Basic.Actions.PvPTargetSelection;
using RotationSolver.Basic.Rotations;
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
	("bard forced burst rejects blocked target", BardForcedBurstRejectsBlockedTarget),
	("bard burst gate cannot override blocked target", BardBurstGateCannotOverrideBlockedTarget),
	("bard forced burst allows unblocked target", BardForcedBurstAllowsUnblockedTarget),
	("bard apex arrow rejects active blast arrow window", BardApexArrowRejectsActiveBlastArrowWindow),
	("bard apex arrow allows missing blast arrow window", BardApexArrowAllowsMissingBlastArrowWindow),
	("protective paean rejects healthy unfocused ally", ProtectivePaeanRejectsHealthyUnfocusedAlly),
	("protective paean allows focused ally", ProtectivePaeanAllowsFocusedAlly),
	("machinist target policy prefers killable low resource target", MachinistTargetPolicyPrefersKillableLowResourceTarget),
	("machinist target policy prefers direct secure target", MachinistTargetPolicyPrefersDirectSecureTarget),
	("machinist target policy allows guarded drill punish", MachinistTargetPolicyAllowsGuardedDrillPunish),
	("machinist analysis drill rejects full resource target", MachinistAnalysisDrillRejectsFullResourceTarget),
	("machinist analysis drill accepts direct secure kill", MachinistAnalysisDrillAcceptsDirectSecureKill),
	("machinist analysis air anchor rejects resilient target", MachinistAnalysisAirAnchorRejectsResilientTarget),
	("machinist analysis air anchor accepts direct secure through resilience", MachinistAnalysisAirAnchorAcceptsDirectSecureThroughResilience),
	("machinist analysis air anchor rejects isolated setup", MachinistAnalysisAirAnchorRejectsIsolatedSetup),
	("machinist analysis chain saw requires follow up", MachinistAnalysisChainSawRequiresFollowUp),
	("machinist scattergun rejects unsafe close range", MachinistScattergunRejectsUnsafeCloseRange),
	("machinist wildfire requires committed target and follow up", MachinistWildfireRequiresCommittedTargetAndFollowUp),
	("machinist bishop accepts objective teamfight", MachinistBishopAcceptsObjectiveTeamfight),
	("machinist bishop rejects out of range targets", MachinistBishopRejectsOutOfRangeTargets),
	("machinist marksmans spite rejects guard", MachinistMarksmanSpiteRejectsGuard),
	("machinist marksmans spite holds on dying ally focused target", MachinistMarksmanSpiteHoldsOnDyingAllyFocusedTarget),
	("machinist marksmans spite accepts secure damage", MachinistMarksmanSpiteAcceptsSecureDamage),
	("machinist marksmans spite rejects guard ready solo execute target", MachinistMarksmanSpiteRejectsGuardReadySoloExecuteTarget),
	("machinist marksmans spite rejects unknown guard solo execute target", MachinistMarksmanSpiteRejectsUnknownGuardSoloExecuteTarget),
	("machinist marksmans spite rejects low mp nonlethal target", MachinistMarksmanSpiteRejectsLowMpNonlethalTarget),
	("machinist marksmans spite accepts ally backed nonlethal target", MachinistMarksmanSpiteAcceptsAllyBackedNonlethalTarget),
	("machinist marksmans spite accepts focused allied burst nonlethal target", MachinistMarksmanSpiteAcceptsFocusedAlliedBurstNonlethalTarget),
	("machinist marksmans spite accepts objective backed nonlethal target", MachinistMarksmanSpiteAcceptsObjectiveBackedNonlethalTarget),
	("machinist marksmans spite rejects objective pressure without focus", MachinistMarksmanSpiteRejectsObjectivePressureWithoutFocus),
	("machinist marksmans spite rejects unfocused ally proximity", MachinistMarksmanSpiteRejectsUnfocusedAllyProximity),
	("machinist marksmans spite rejects unsupported narrow lethal target", MachinistMarksmanSpiteRejectsUnsupportedNarrowLethalTarget),
	("machinist marksmans spite rejects objective conversion above leftover cap", MachinistMarksmanSpiteRejectsObjectiveConversionAboveLeftoverCap),
	("machinist marksmans spite rejects focused ally conversion above leftover cap", MachinistMarksmanSpiteRejectsFocusedAllyConversionAboveLeftoverCap),
	("machinist marksmans spite rejects focused pressure above tight cap", MachinistMarksmanSpiteRejectsFocusedPressureAboveTightCap),
	("machinist marksmans spite accepts vulnerable target", MachinistMarksmanSpiteAcceptsVulnerableTarget),
	("machinist marksmans spite rejects vulnerable pressure target", MachinistMarksmanSpiteRejectsVulnerablePressureTarget),
	("machinist marksmans spite rejects unsupported vulnerable target", MachinistMarksmanSpiteRejectsUnsupportedVulnerableTarget),
	("machinist marksmans spite rejects active invulnerability", MachinistMarksmanSpiteRejectsActiveInvulnerability),
	("machinist marksmans spite accepts mitigated secure kill", MachinistMarksmanSpiteAcceptsMitigatedSecureKill),
	("machinist marksmans spite rejects conversion without guard cooldown knowledge", MachinistMarksmanSpiteRejectsConversionWithoutGuardCooldownKnowledge),
	("machinist marksmans spite rejects guard ready conversion target", MachinistMarksmanSpiteRejectsGuardReadyConversionTarget),
	("machinist marksmans spite rejects unknown guard conversion target", MachinistMarksmanSpiteRejectsUnknownGuardConversionTarget),
	("machinist marksmans spite accepts guard cooldown conversion target", MachinistMarksmanSpiteAcceptsGuardCooldownConversionTarget),
	("machinist marksmans spite rejects focused finisher in strict mode", MachinistMarksmanSpiteRejectsFocusedFinisherInStrictMode),
	("machinist marksmans spite accepts strict execute on guard cooldown", MachinistMarksmanSpiteAcceptsStrictExecuteOnGuardCooldown),
	("machinist marksmans spite rejects unknown guard lethal emergency", MachinistMarksmanSpiteRejectsUnknownGuardLethalEmergency),
	("machinist marksmans spite identity rejects adjusted drill", MachinistMarksmanSpiteIdentityRejectsAdjustedDrill),
	("machinist marksmans spite live guard veto blocks inherited pierce", MachinistMarksmanSpiteLiveGuardVetoBlocksInheritedPierce),
	("machinist analysis chain saw accepts low resource kill window", MachinistAnalysisChainSawAcceptsLowResourceKillWindow),
	("machinist full metal rejects uncommitted follow up", MachinistFullMetalRejectsUncommittedFollowUp),
	("machinist full metal accepts direct secure without follow up", MachinistFullMetalAcceptsDirectSecureWithoutFollowUp),
	("machinist full metal rejects guarded direct secure", MachinistFullMetalRejectsGuardedDirectSecure),
	("machinist full metal rejects out of range direct secure", MachinistFullMetalRejectsOutOfRangeDirectSecure),
	("machinist blazing shot accepts direct secure without follow up", MachinistBlazingShotAcceptsDirectSecureWithoutFollowUp),
	("pvp damage gate rejects invulnerability", PvpDamageGateRejectsInvulnerability),
	("pvp damage gate allows mitigated secure kill", PvpDamageGateAllowsMitigatedSecureKill),
	("pvp final guard gate blocks stale guarded target", PvpFinalGuardGateBlocksStaleGuardedTarget),
	("pvp final guard gate allows guard piercing action", PvpFinalGuardGateAllowsGuardPiercingAction),
	("pvp final guard gate allows expiring guard", PvpFinalGuardGateAllowsExpiringGuard),
	("pvp final guard gate allows nonhostile action", PvpFinalGuardGateAllowsNonhostileAction),
	("pvp guard cooldown tracker backdates observed guard", PvpGuardCooldownTrackerBackdatesObservedGuard),
	("pvp guard cooldown tracker keeps cooldown after early cancel", PvpGuardCooldownTrackerKeepsCooldownAfterEarlyCancel),
	("pvp guard cooldown tracker requires safe unavailable window", PvpGuardCooldownTrackerRequiresSafeUnavailableWindow),
	("pvp guard cooldown tracker forgets stale unseen targets", PvpGuardCooldownTrackerForgetsStaleUnseenTargets),
	("pvp guard cooldown tracker forgets target", PvpGuardCooldownTrackerForgetsTarget),
	("bard forced burst allows direct secure target", BardForcedBurstAllowsDirectSecureTarget),
	("bard forced burst rejects blocked direct secure target", BardForcedBurstRejectsBlockedDirectSecureTarget),
	("bard kill secure ranks lethal hostile", BardKillSecureRanksLethalHostile),
	("bard kill secure rejects invulnerability", BardKillSecureRejectsInvulnerability),
	("bard kill secure prefers lowest lethal health", BardKillSecurePrefersLowestLethalHealth),
	("bard offensive target policy prefers direct secure target", BardOffensiveTargetPolicyPrefersDirectSecureTarget),
	("bard offensive target policy prefers low mp target", BardOffensiveTargetPolicyPrefersLowMpTarget),
	("bard offensive target policy uses pitch perfect splash value", BardOffensiveTargetPolicyUsesPitchPerfectSplashValue),
	("bard offensive target policy rejects out of range target", BardOffensiveTargetPolicyRejectsOutOfRangeTarget),
	("bard offensive target policy keeps eagle eye guard target", BardOffensiveTargetPolicyKeepsEagleEyeGuardTarget),
	("bard offensive target policy treats guarded eagle eye target as exposed", BardOffensiveTargetPolicyTreatsGuardedEagleEyeTargetAsExposed),
	("bard offensive target policy preserves eagle eye mitigation", BardOffensiveTargetPolicyPreservesEagleEyeMitigation),
	("bard offensive target policy penalizes blast resilience", BardOffensiveTargetPolicyPenalizesBlastResilience),
	("bard harmonic arrow rejects guarded nonlethal target", BardHarmonicArrowRejectsGuardedNonlethalTarget),
	("bard harmonic arrow accepts unblocked charge overcap", BardHarmonicArrowAcceptsUnblockedChargeOvercap),
	("bard harmonic arrow accepts low mp conversion", BardHarmonicArrowAcceptsLowMpConversion),
	("bard pitch perfect accepts repertoire ally focus follow up", BardPitchPerfectAcceptsRepertoireAllyFocusFollowUp),
	("bard pitch perfect accepts repertoire low mp target", BardPitchPerfectAcceptsRepertoireLowMpTarget),
	("bard pitch perfect accepts repertoire objective target", BardPitchPerfectAcceptsRepertoireObjectiveTarget),
	("bard pitch perfect accepts repertoire ally burst", BardPitchPerfectAcceptsRepertoireAllyBurst),
	("bard pitch perfect rejects repertoire filler", BardPitchPerfectRejectsRepertoireFiller),
	("bard apex arrow accepts objective line value", BardApexArrowAcceptsObjectiveLineValue),
	("bard apex arrow accepts guarded objective pressure", BardApexArrowAcceptsGuardedObjectivePressure),
	("bard apex arrow accepts guarded forced timing", BardApexArrowAcceptsGuardedForcedTiming),
	("bard apex arrow accepts standalone objective value", BardApexArrowAcceptsStandaloneObjectiveValue),
	("bard apex arrow accepts standalone ally burst value", BardApexArrowAcceptsStandaloneAllyBurstValue),
	("bard apex arrow rejects guarded filler", BardApexArrowRejectsGuardedFiller),
	("bard blast arrow accepts objective displacement", BardBlastArrowAcceptsObjectiveDisplacement),
	("bard blast arrow rejects resilience displacement", BardBlastArrowRejectsResilienceDisplacement),
	("bard blast arrow rejects blast ready filler", BardBlastArrowRejectsBlastReadyFiller),
	("bard encore of light accepts low mp conversion", BardEncoreOfLightAcceptsLowMpConversion),
	("bard encore of light accepts ally burst window", BardEncoreOfLightAcceptsAllyBurstWindow),
	("bard encore of light accepts final fantasia push window", BardEncoreOfLightAcceptsFinalFantasiaPushWindow),
	("bard encore of light rejects blocked filler", BardEncoreOfLightRejectsBlockedFiller),
	("bard encore of light rejects guard reaction conversion", BardEncoreOfLightRejectsGuardReactionConversion),
	("bard encore of light rejects unknown guard reaction conversion", BardEncoreOfLightRejectsUnknownGuardReactionConversion),
	("bard powerful shot accepts safe pressure filler", BardPowerfulShotAcceptsSafePressureFiller),
	("bard powerful shot accepts neutral safe filler", BardPowerfulShotAcceptsNeutralSafeFiller),
	("bard powerful shot rejects blocked target", BardPowerfulShotRejectsBlockedTarget),
	("bard offensive decision policy reruns live guard state", BardOffensiveDecisionPolicyRerunsLiveGuardState),
	("bard target refresh updates live spatial signals", BardTargetRefreshUpdatesLiveSpatialSignals),
	("pvp lb json contains verified entries", PvpLbJsonContainsVerifiedEntries),
	("pvp mitigation json contains resilience", PvpMitigationJsonContainsResilience),
	("pvp mitigation json contains ranked cc defensive coverage", PvpMitigationJsonContainsRankedCcDefensiveCoverage),
	("frontline role action policy rejects crystalline conflict", FrontlineRoleActionPolicyRejectsCrystallineConflict),
	("frontline role action policy allows frontline", FrontlineRoleActionPolicyAllowsFrontline),
	("frontline role action policy detects frontline duties", FrontlineRoleActionPolicyDetectsFrontlineDuties),
	("frontline role action policy keeps action passes separate", FrontlineRoleActionPolicyKeepsActionPassesSeparate),
	("frontline role action policy defers bard and machinist eagle eye shot", FrontlineRoleActionPolicyDefersBardAndMachinistEagleEyeShot),
	("frontline eagle eye shot rejects crystalline conflict", FrontlineEagleEyeShotRejectsCrystallineConflict),
	("bard frontline eagle eye shot waits for controller window", BardFrontlineEagleEyeShotWaitsForControllerWindow),
	("bard frontline eagle eye shot accepts controlled target", BardFrontlineEagleEyeShotAcceptsControlledTarget),
	("machinist frontline eagle eye shot rejects healthy filler", MachinistFrontlineEagleEyeShotRejectsHealthyFiller),
	("machinist frontline eagle eye shot accepts injured target", MachinistFrontlineEagleEyeShotAcceptsInjuredTarget),
	("machinist frontline eagle eye shot accepts burst setup target", MachinistFrontlineEagleEyeShotAcceptsBurstSetupTarget),
	("machinist frontline eagle eye shot accepts wildfire target", MachinistFrontlineEagleEyeShotAcceptsWildfireTarget),
	("machinist frontline eagle eye shot accepts guard pressure target", MachinistFrontlineEagleEyeShotAcceptsGuardPressureTarget),
	("machinist frontline eagle eye shot secures through guard", MachinistFrontlineEagleEyeShotSecuresThroughGuard),
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

static void FrontlineRoleActionPolicyRejectsCrystallineConflict()
{
	var shouldTry = FrontlinePvPRoleActionPolicy.ShouldTryFrontlineRoleAction(
		isInFrontline: false,
		isInCrystallineConflict: true);

	AssertFalse(shouldTry, "Crystalline Conflict must not use the Frontlines role action path");
}

static void FrontlineRoleActionPolicyAllowsFrontline()
{
	var shouldTry = FrontlinePvPRoleActionPolicy.ShouldTryFrontlineRoleAction(
		isInFrontline: true,
		isInCrystallineConflict: false);

	AssertTrue(shouldTry, "Frontline should opt into PvP role action automation");
}

static void FrontlineRoleActionPolicyDetectsFrontlineDuties()
{
	foreach (var contentFinderName in FrontlinePvPRoleActionPolicy.FrontlineContentFinderNames)
	{
		AssertTrue(
			FrontlinePvPRoleActionPolicy.IsFrontlineContentFinderName(contentFinderName),
			$"{contentFinderName} should be detected as Frontline");
	}

	AssertFalse(
		FrontlinePvPRoleActionPolicy.IsFrontlineContentFinderName("Crystalline Conflict"),
		"Crystalline Conflict must not be detected as Frontline");
}

static void FrontlineRoleActionPolicyKeepsActionPassesSeparate()
{
	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldUseInCurrentPass(
			isRealGcd: true,
			requireGcdAction: true),
		"GCD role actions should be evaluated in the GCD pass");

	AssertFalse(
		FrontlinePvPRoleActionPolicy.ShouldUseInCurrentPass(
			isRealGcd: true,
			requireGcdAction: false),
		"GCD role actions must not be evaluated in the ability pass");

	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldUseInCurrentPass(
			isRealGcd: false,
			requireGcdAction: false),
		"ability role actions should be evaluated in the ability pass");

	AssertFalse(
		FrontlinePvPRoleActionPolicy.ShouldUseInCurrentPass(
			isRealGcd: false,
			requireGcdAction: true),
		"ability role actions must not be evaluated in the GCD pass");
}

static void FrontlineRoleActionPolicyDefersBardAndMachinistEagleEyeShot()
{
	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldDeferEagleEyeShotToJobPolicy(FrontlinePvPRangedJob.Bard),
		"Bard should route Eagle Eye Shot through its controller support policy");

	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldDeferEagleEyeShotToJobPolicy(FrontlinePvPRangedJob.Machinist),
		"Machinist should route Eagle Eye Shot through its burst pick policy");

	AssertFalse(
		FrontlinePvPRoleActionPolicy.ShouldDeferEagleEyeShotToJobPolicy(FrontlinePvPRangedJob.Other),
		"Other physical ranged jobs should keep the generic Frontline role action path");
}

static void FrontlineEagleEyeShotRejectsCrystallineConflict()
{
	var input = FrontlineEagleEyeShotInput(
		FrontlinePvPRangedJob.Machinist,
		NeutralEagleEyeShotTarget() with { HasWildfire = true },
		isInFrontline: false,
		isInCrystallineConflict: true);

	AssertFalse(
		FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input),
		"Crystalline Conflict must not enter the Frontline Eagle Eye Shot policy");
}

static void BardFrontlineEagleEyeShotWaitsForControllerWindow()
{
	var input = FrontlineEagleEyeShotInput(
		FrontlinePvPRangedJob.Bard,
		NeutralEagleEyeShotTarget() with { HealthRatio = 0.90f });

	AssertFalse(
		FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input),
		"Bard should not spend Eagle Eye Shot as filler");
}

static void BardFrontlineEagleEyeShotAcceptsControlledTarget()
{
	var input = FrontlineEagleEyeShotInput(
		FrontlinePvPRangedJob.Bard,
		NeutralEagleEyeShotTarget() with
		{
			HealthRatio = 0.54f,
			IsControlled = true,
		});

	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input),
		"Bard should spend Eagle Eye Shot into a controlled pressure target");
}

static void MachinistFrontlineEagleEyeShotRejectsHealthyFiller()
{
	var input = FrontlineEagleEyeShotInput(
		FrontlinePvPRangedJob.Machinist,
		NeutralEagleEyeShotTarget() with { HealthRatio = 0.90f });

	AssertFalse(
		FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input),
		"Machinist should not spend Eagle Eye Shot on healthy filler targets");
}

static void MachinistFrontlineEagleEyeShotAcceptsInjuredTarget()
{
	var input = FrontlineEagleEyeShotInput(
		FrontlinePvPRangedJob.Machinist,
		NeutralEagleEyeShotTarget() with { HealthRatio = 0.65f });

	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input),
		"Machinist should spend Eagle Eye Shot on injured targets because it has a short recast");
}

static void MachinistFrontlineEagleEyeShotAcceptsBurstSetupTarget()
{
	var input = FrontlineEagleEyeShotInput(
		FrontlinePvPRangedJob.Machinist,
		NeutralEagleEyeShotTarget() with
		{
			HealthRatio = 0.80f,
			ImmediateFollowUpAvailable = true,
		});

	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input),
		"Machinist should spend Eagle Eye Shot as part of a normal burst setup");
}

static void MachinistFrontlineEagleEyeShotAcceptsWildfireTarget()
{
	var input = FrontlineEagleEyeShotInput(
		FrontlinePvPRangedJob.Machinist,
		NeutralEagleEyeShotTarget() with
		{
			HealthRatio = 0.80f,
			HasWildfire = true,
		});

	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input),
		"Machinist should spend Eagle Eye Shot into its Wildfire pick window");
}

static void MachinistFrontlineEagleEyeShotAcceptsGuardPressureTarget()
{
	var input = FrontlineEagleEyeShotInput(
		FrontlinePvPRangedJob.Machinist,
		NeutralEagleEyeShotTarget() with
		{
			HealthRatio = 0.60f,
			HasGuard = true,
			TargetCommitted = true,
		});

	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input),
		"Machinist should pressure committed Guard targets because Eagle Eye Shot ignores Guard");
}

static void MachinistFrontlineEagleEyeShotSecuresThroughGuard()
{
	var input = FrontlineEagleEyeShotInput(
		FrontlinePvPRangedJob.Machinist,
		NeutralEagleEyeShotTarget() with
		{
			HealthRatio = 0.20f,
			HasGuard = true,
			ExpectedDamageRatio = 0.25,
		});

	AssertTrue(
		FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input),
		"Machinist should secure killable Guard targets because Eagle Eye Shot ignores Guard");
}

static FrontlineEagleEyeShotInput FrontlineEagleEyeShotInput(
	FrontlinePvPRangedJob job,
	FrontlineEagleEyeShotTargetState target,
	bool isInFrontline = true,
	bool isInCrystallineConflict = false)
{
	return new FrontlineEagleEyeShotInput(
		Job: job,
		IsInFrontline: isInFrontline,
		IsInCrystallineConflict: isInCrystallineConflict,
		Target: target);
}

static FrontlineEagleEyeShotTargetState NeutralEagleEyeShotTarget()
{
	return new FrontlineEagleEyeShotTargetState(
		HealthRatio: 1.0f,
		CurrentMp: 10_000,
		HasGuard: false,
		HasResilience: false,
		HasNonGuardInvulnerability: false,
		HasAllyFocus: false,
		IsObjectiveRelevant: false,
		IsControlled: false,
		IsBurstWorthy: false,
		TargetCommitted: false,
		ImmediateFollowUpAvailable: false,
		HasWildfire: false,
		ExpectedDamageRatio: 0.20);
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

static void BardForcedBurstRejectsBlockedTarget()
{
	AssertFalse(
		BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
			targetIsBurstWorthy: false,
			targetBlocksDamage: true,
			forcedSpendWindow: true),
		"Bard forced burst should not override a blocked damage target");
}

static void BardBurstGateCannotOverrideBlockedTarget()
{
	AssertFalse(
		BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
			targetIsBurstWorthy: true,
			targetBlocksDamage: true,
			forcedSpendWindow: true),
		"Bard burst should not fire when active mitigation blocks the damage");
}

static void BardForcedBurstAllowsUnblockedTarget()
{
	AssertTrue(
		BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
			targetIsBurstWorthy: false,
			targetBlocksDamage: false,
			forcedSpendWindow: true),
		"Bard forced burst may prevent expiry or overcap when damage is not blocked");
}

static void BardApexArrowRejectsActiveBlastArrowWindow()
{
	AssertFalse(
		BardPvPDecisionPolicy.ShouldUseApexArrow(true),
		"Apex Arrow must not overwrite an active Blast Arrow window");
}

static void BardApexArrowAllowsMissingBlastArrowWindow()
{
	AssertTrue(
		BardPvPDecisionPolicy.ShouldUseApexArrow(false),
		"Apex Arrow should remain available when Blast Arrow is not ready");
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

static void MachinistTargetPolicyPrefersDirectSecureTarget()
{
	var directSecureTarget = MachinistTarget(
		1,
		healthRatio: 0.15f,
		currentMp: 10_000,
		effectiveHealthRatio: 0.15,
		expectedDamageRatio: 0.20);
	var lowResourceTarget = MachinistTarget(2, healthRatio: 0.40f, currentMp: 2_000);

	var selected = MachinistPvPTargetPolicy.SelectBest(
		[directSecureTarget, lowResourceTarget],
		MachinistPvPActionIntent.AnalysisDrill);

	AssertEqual(1UL, selected?.TargetId, "MCH should prefer a direct secure target over a low MP pressure target");
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

static void MachinistAnalysisDrillAcceptsDirectSecureKill()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.18f,
			currentMp: 10_000,
			effectiveHealthRatio: 0.18,
			expectedDamageRatio: 0.25),
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: false);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseAnalysisDrill(input), "Analysis Drill should secure a lethal target even when MP is high");
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

static void MachinistAnalysisAirAnchorAcceptsDirectSecureThroughResilience()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.18f,
			currentMp: 10_000,
			hasResilience: true,
			effectiveHealthRatio: 0.18,
			expectedDamageRatio: 0.20),
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: false);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseAnalysisAirAnchor(input), "Analysis Air Anchor damage should secure through Resilience");
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
		Target: MachinistTarget(1, healthRatio: 0.70f, currentMp: 10_000),
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

static void MachinistMarksmanSpiteHoldsOnDyingAllyFocusedTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.14f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.14),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend LB on targets allies are already cleaning up");
}

static void MachinistMarksmanSpiteAcceptsSecureDamage()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.55f,
			currentMp: 2_000,
			effectiveHealthRatio: 0.55),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should convert targets inside secure damage range");
}

static void MachinistMarksmanSpiteRejectsGuardReadySoloExecuteTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.55f,
			currentMp: 2_000,
			effectiveHealthRatio: 0.55,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.Ready),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend on a solo execute target who can Guard on reaction");
}

static void MachinistMarksmanSpiteRejectsUnknownGuardSoloExecuteTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.55f,
			currentMp: 2_000,
			effectiveHealthRatio: 0.55,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.Unknown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend on a solo execute target with unknown Guard availability");
}

static void MachinistMarksmanSpiteRejectsLowMpNonlethalTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire when low MP is the only nonlethal signal");
}

static void MachinistMarksmanSpiteAcceptsAllyBackedNonlethalTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.CoolingDown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should fire when allies can convert the leftover health");
}

static void MachinistMarksmanSpiteAcceptsFocusedAlliedBurstNonlethalTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.CoolingDown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should fire when allied burst can convert the leftover health");
}

static void MachinistMarksmanSpiteAcceptsObjectiveBackedNonlethalTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.CoolingDown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: true,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should fire when focused objective pressure can convert the leftover health");
}

static void MachinistMarksmanSpiteRejectsObjectivePressureWithoutFocus()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.72f,
			currentMp: 2_000,
			effectiveHealthRatio: 0.72,
			expectedDamageRatio: 0.67),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: true,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not use LB as objective pressure when no one can convert the leftover health");
}

static void MachinistMarksmanSpiteRejectsUnfocusedAllyProximity()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not treat nearby allies as focused conversion pressure");
}

static void MachinistMarksmanSpiteRejectsUnsupportedNarrowLethalTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.665f,
			currentMp: 2_000,
			effectiveHealthRatio: 0.665),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should hold narrow solo lethal reads without conversion support");
}

static void MachinistMarksmanSpiteRejectsObjectiveConversionAboveLeftoverCap()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.76f,
			currentMp: 2_000,
			effectiveHealthRatio: 0.76,
			expectedDamageRatio: 0.67),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: true,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire on objective pressure above the leftover cap");
}

static void MachinistMarksmanSpiteRejectsFocusedAllyConversionAboveLeftoverCap()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.76f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.76,
			expectedDamageRatio: 0.67),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire on focused ally pressure above the leftover cap");
}

static void MachinistMarksmanSpiteRejectsFocusedPressureAboveTightCap()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.72f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.72,
			expectedDamageRatio: 0.67),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend LB when focused pressure leaves too much health to clean up");
}

static void MachinistMarksmanSpiteAcceptsVulnerableTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 10_000,
			hasAllyFocus: true,
			isVulnerable: true,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.CoolingDown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should accept vulnerable targets when focused pressure can convert the leftover health");
}

static void MachinistMarksmanSpiteRejectsVulnerablePressureTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.80f, currentMp: 10_000, isVulnerable: true),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend LB only to leave a vulnerable target low");
}

static void MachinistMarksmanSpiteRejectsUnsupportedVulnerableTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.80f, currentMp: 10_000, isVulnerable: true),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire on vulnerability without conversion support");
}

static void MachinistMarksmanSpiteRejectsActiveInvulnerability()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.10f, currentMp: 0, hasInvulnerability: true),
		ExpectedDamageRatio: 1.00,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: true,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not fire into active invulnerability");
}

static void MachinistMarksmanSpiteAcceptsMitigatedSecureKill()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.45f,
			currentMp: 10_000,
			effectiveHealthRatio: 0.62,
			activeDamageReduction: 0.25),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: false);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should fire through mitigation when expected damage still kills");
}

static void MachinistMarksmanSpiteRejectsConversionWithoutGuardCooldownKnowledge()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.Ready),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should require Guard cooldown knowledge before spending on a focused conversion target");
}

static void MachinistMarksmanSpiteRejectsGuardReadyConversionTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.Ready),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should not spend on a narrow conversion target who can Guard on reaction");
}

static void MachinistMarksmanSpiteRejectsUnknownGuardConversionTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.Unknown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should treat unknown Guard availability as too risky for nonlethal conversion");
}

static void MachinistMarksmanSpiteAcceptsGuardCooldownConversionTarget()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.CoolingDown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite should allow existing conversion gates when Guard is confirmed unavailable");
}

static void MachinistMarksmanSpiteRejectsFocusedFinisherInStrictMode()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.68f,
			currentMp: 2_000,
			hasAllyFocus: true,
			effectiveHealthRatio: 0.68,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.CoolingDown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true,
		StrictMarksmanExecuteOnly: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite mode should reject focused team finishers");
}

static void MachinistMarksmanSpiteAcceptsStrictExecuteOnGuardCooldown()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 2_000,
			effectiveHealthRatio: 0.40,
			expectedDamageRatio: 0.67,
			guardAvailability: PvPGuardAvailability.CoolingDown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true,
		StrictMarksmanExecuteOnly: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Strict Marksman's Spite mode should still accept a clear execute when Guard is cooling down");
}

static void MachinistMarksmanSpiteRejectsUnknownGuardLethalEmergency()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.40f,
			currentMp: 0,
			effectiveHealthRatio: 0.40,
			guardAvailability: PvPGuardAvailability.Unknown),
		ExpectedDamageRatio: 0.67,
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: true,
		TargetCommitted: true,
		HasGuardCooldownKnowledge: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite(input), "Marksman's Spite objective pressure should not override unknown Guard availability");
}

static void MachinistMarksmanSpiteIdentityRejectsAdjustedDrill()
{
	const uint drillPvPActionId = 29405;
	const uint marksmanSpitePvPActionId = 29415;

	AssertFalse(
		MachinistPvPDecisionPolicy.IsDirectMarksmansSpiteAction(drillPvPActionId, marksmanSpitePvPActionId),
		"Marksman's Spite lookup must not accept Drill only because Drill adjusted into the LB action");
	AssertTrue(
		MachinistPvPDecisionPolicy.IsDirectMarksmansSpiteAction(marksmanSpitePvPActionId, marksmanSpitePvPActionId),
		"Marksman's Spite lookup should accept the direct PvP LB action");
}

static void MachinistMarksmanSpiteLiveGuardVetoBlocksInheritedPierce()
{
	var activeGuard = new MachinistPvPLiveGuardInput(
		TargetHasGuard: true,
		GuardWillExpireBeforeAction: false);
	var expiringGuard = new MachinistPvPLiveGuardInput(
		TargetHasGuard: true,
		GuardWillExpireBeforeAction: true);

	AssertTrue(
		MachinistPvPDecisionPolicy.ShouldVetoMarksmanSpiteForLiveGuard(activeGuard),
		"Marksman's Spite should be vetoed by live Guard even if the selected action object inherited Guard piercing settings");
	AssertFalse(
		MachinistPvPDecisionPolicy.ShouldVetoMarksmanSpiteForLiveGuard(expiringGuard),
		"Marksman's Spite should not be vetoed when Guard expires before the LB resolves");
}

static void MachinistAnalysisChainSawAcceptsLowResourceKillWindow()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(1, healthRatio: 0.50f, currentMp: 2_000),
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: true);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseAnalysisChainSaw(input), "Analysis Chain Saw should mark low MP targets before they stabilize");
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

static void MachinistFullMetalAcceptsDirectSecureWithoutFollowUp()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.20f,
			currentMp: 10_000,
			effectiveHealthRatio: 0.20,
			expectedDamageRatio: 0.25),
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: false);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseFullMetalField(input), "Full Metal Field should secure lethal targets without setup signals");
}

static void MachinistFullMetalRejectsGuardedDirectSecure()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.20f,
			currentMp: 10_000,
			hasGuard: true,
			effectiveHealthRatio: 0.20,
			expectedDamageRatio: 0.25),
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: true,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseFullMetalField(input), "Full Metal Field should not treat Guard as killable");
}

static void MachinistFullMetalRejectsOutOfRangeDirectSecure()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.20f,
			currentMp: 10_000,
			effectiveHealthRatio: 0.20,
			expectedDamageRatio: 0.25,
			isInNormalRange: false),
		SafeCloseRange: true,
		FollowUpAvailable: true,
		AlliesCanBurst: true,
		ObjectiveControlNeeded: true,
		TargetCommitted: true);

	AssertFalse(MachinistPvPDecisionPolicy.ShouldUseFullMetalField(input), "Full Metal Field should not secure targets outside action range");
}

static void MachinistBlazingShotAcceptsDirectSecureWithoutFollowUp()
{
	var input = new MachinistPvPDecisionInput(
		Target: MachinistTarget(
			1,
			healthRatio: 0.12f,
			currentMp: 10_000,
			effectiveHealthRatio: 0.12,
			expectedDamageRatio: 0.15),
		SafeCloseRange: true,
		FollowUpAvailable: false,
		AlliesCanBurst: false,
		ObjectiveControlNeeded: false,
		TargetCommitted: false);

	AssertTrue(MachinistPvPDecisionPolicy.ShouldUseBlazingShot(input), "Blazing Shot should secure lethal targets without setup signals");
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

static void BardForcedBurstAllowsDirectSecureTarget()
{
	AssertTrue(
		BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
			targetIsBurstWorthy: false,
			targetBlocksDamage: false,
			forcedSpendWindow: false,
			targetCanBeKilled: true),
		"Bard burst actions should spend when the current action can secure the target");
}

static void BardForcedBurstRejectsBlockedDirectSecureTarget()
{
	AssertFalse(
		BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
			targetIsBurstWorthy: true,
			targetBlocksDamage: true,
			forcedSpendWindow: true,
			targetCanBeKilled: true),
		"Bard burst actions should not spend into blocked damage even when HP looks lethal");
}

static void BardKillSecureRanksLethalHostile()
{
	var targets = new[]
	{
		BardKillTarget(1, healthRatio: 0.20f, effectiveHealthRatio: 0.20, expectedDamageRatio: 0.10),
		BardKillTarget(2, healthRatio: 0.12f, effectiveHealthRatio: 0.09, expectedDamageRatio: 0.10),
	};

	var ranked = BardPvPDecisionPolicy.RankDirectSecureTargets(targets);

	AssertEqual(1, ranked.Count, "only lethal Bard targets should be ranked");
	AssertEqual(2UL, ranked[0], "Bard should force target selection onto the lethal hostile");
}

static void BardKillSecureRejectsInvulnerability()
{
	var targets = new[]
	{
		BardKillTarget(1, healthRatio: 0.05f, effectiveHealthRatio: 0.05, expectedDamageRatio: 0.10, hasInvulnerability: true),
	};

	var ranked = BardPvPDecisionPolicy.RankDirectSecureTargets(targets);

	AssertEqual(0, ranked.Count, "Bard kill secure must not target active invulnerability");
}

static void BardKillSecurePrefersLowestLethalHealth()
{
	var targets = new[]
	{
		BardKillTarget(1, healthRatio: 0.18f, effectiveHealthRatio: 0.09, expectedDamageRatio: 0.10),
		BardKillTarget(2, healthRatio: 0.08f, effectiveHealthRatio: 0.07, expectedDamageRatio: 0.10),
	};

	var ranked = BardPvPDecisionPolicy.RankDirectSecureTargets(targets);

	AssertEqual(2, ranked.Count, "all lethal Bard targets should remain available");
	AssertEqual(2UL, ranked[0], "Bard should target the lowest health lethal hostile first");
}

static void BardOffensiveTargetPolicyPrefersDirectSecureTarget()
{
	var directSecureTarget = BardOffensiveTarget(
		1,
		healthRatio: 0.15f,
		currentMp: 10_000,
		effectiveHealthRatio: 0.15,
		expectedDamageRatio: 0.20);
	var lowMpTarget = BardOffensiveTarget(2, healthRatio: 0.40f, currentMp: 2_000);

	var selected = BardPvPTargetPolicy.SelectBest(
		[directSecureTarget, lowMpTarget],
		BardPvPActionIntent.HarmonicArrow);

	AssertEqual(1UL, selected?.TargetId, "Bard should prefer a target that the current action can secure");
}

static void BardOffensiveTargetPolicyPrefersLowMpTarget()
{
	var highMpTarget = BardOffensiveTarget(1, healthRatio: 0.40f, currentMp: 10_000);
	var lowMpTarget = BardOffensiveTarget(2, healthRatio: 0.40f, currentMp: 2_000);

	var selected = BardPvPTargetPolicy.SelectBest(
		[highMpTarget, lowMpTarget],
		BardPvPActionIntent.PowerfulShot);

	AssertEqual(2UL, selected?.TargetId, "Bard should prefer pressure on enemies with limited Recuperate resources");
}

static void BardOffensiveTargetPolicyUsesPitchPerfectSplashValue()
{
	var isolatedTarget = BardOffensiveTarget(1, healthRatio: 0.40f, currentMp: 10_000, splashTargetCount: 1);
	var splashTarget = BardOffensiveTarget(2, healthRatio: 0.40f, currentMp: 10_000, splashTargetCount: 3);

	var selected = BardPvPTargetPolicy.SelectBest(
		[isolatedTarget, splashTarget],
		BardPvPActionIntent.PitchPerfect);

	AssertEqual(2UL, selected?.TargetId, "Pitch Perfect should prefer targets that add splash value");
}

static void BardOffensiveTargetPolicyRejectsOutOfRangeTarget()
{
	var inRangeTarget = BardOffensiveTarget(1, healthRatio: 0.80f, currentMp: 10_000, isInNormalRange: true);
	var outOfRangeTarget = BardOffensiveTarget(2, healthRatio: 0.10f, currentMp: 0, isInNormalRange: false);

	var rankedTargets = BardPvPTargetPolicy.Rank(
		[inRangeTarget, outOfRangeTarget],
		BardPvPActionIntent.HarmonicArrow);

	AssertEqual(1, rankedTargets.Count, "Bard offensive targeting should keep only reachable targets");
	AssertEqual(1UL, rankedTargets[0].TargetId, "Bard offensive targeting should choose the reachable target");
}

static void BardOffensiveTargetPolicyKeepsEagleEyeGuardTarget()
{
	var guardedTarget = BardOffensiveTarget(
		1,
		healthRatio: 0.40f,
		currentMp: 10_000,
		hasGuard: true,
		expectedDamageRatio: 0.0);
	var exposedTarget = guardedTarget with { TargetId = 2, HasGuard = false };

	var guardedScore = BardPvPTargetPolicy.Score(guardedTarget, BardPvPActionIntent.EagleEyeShot);
	var exposedScore = BardPvPTargetPolicy.Score(exposedTarget, BardPvPActionIntent.EagleEyeShot);

	AssertEqual(exposedScore, guardedScore, "Eagle Eye Shot should not penalize Guard because the role action ignores Guard");
}

static void BardOffensiveTargetPolicyTreatsGuardedEagleEyeTargetAsExposed()
{
	const ulong guardedTargetId = 1;
	const ulong exposedTargetId = 2;
	const float targetHealthRatio = 0.40f;
	const uint fullMp = 10_000;
	const double noExpectedDamage = 0.0;
	var guardedTarget = BardOffensiveTarget(
		guardedTargetId,
		healthRatio: targetHealthRatio,
		currentMp: fullMp,
		hasGuard: true,
		isExposed: false,
		isInNormalRange: true,
		expectedDamageRatio: noExpectedDamage);
	var exposedTarget = guardedTarget with { TargetId = exposedTargetId, HasGuard = false, IsExposed = true };

	var guardedScore = BardPvPTargetPolicy.Score(guardedTarget, BardPvPActionIntent.EagleEyeShot);
	var exposedScore = BardPvPTargetPolicy.Score(exposedTarget, BardPvPActionIntent.EagleEyeShot);

	AssertEqual(exposedScore, guardedScore, "Eagle Eye Shot should treat guarded in-range targets as exposed because it ignores Guard");
}

static void BardOffensiveTargetPolicyPreservesEagleEyeMitigation()
{
	var mitigatedTarget = BardOffensiveTarget(
		1,
		healthRatio: 0.10f,
		currentMp: 10_000,
		hasGuard: true,
		effectiveHealthRatio: 0.40,
		guardPiercingEffectiveHealthRatio: 0.40,
		expectedDamageRatio: 0.20);
	var noDamageTarget = mitigatedTarget with { ExpectedDamageRatio = 0.0 };

	var score = BardPvPTargetPolicy.Score(mitigatedTarget, BardPvPActionIntent.EagleEyeShot);
	var noDamageScore = BardPvPTargetPolicy.Score(noDamageTarget, BardPvPActionIntent.EagleEyeShot);

	AssertEqual(noDamageScore, score, "Eagle Eye Shot should not treat nonlethal mitigated targets as direct secure");
}

static void BardOffensiveTargetPolicyPenalizesBlastResilience()
{
	var resilientTarget = BardOffensiveTarget(
		1,
		healthRatio: 0.40f,
		currentMp: 10_000,
		hasResilience: true,
		lineTargetCount: 1);
	var exposedTarget = resilientTarget with { TargetId = 2, HasResilience = false };

	var resilientScore = BardPvPTargetPolicy.Score(resilientTarget, BardPvPActionIntent.BlastArrow);
	var exposedScore = BardPvPTargetPolicy.Score(exposedTarget, BardPvPActionIntent.BlastArrow);

	AssertTrue(resilientScore < exposedScore, "Blast Arrow should penalize Resilience when displacement value matters");
}

static void BardHarmonicArrowRejectsGuardedNonlethalTarget()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.42f,
			currentMp: 10_000,
			hasGuard: true,
			effectiveHealthRatio: 0.42,
			expectedDamageRatio: 0.30),
		alliesCanBurst: true,
		objectiveControlNeeded: true,
		harmonicWouldOvercap: true);

	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseHarmonicArrow(input), "Harmonic Arrow should not spend into Guard when the target survives");
}

static void BardHarmonicArrowAcceptsUnblockedChargeOvercap()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000),
		harmonicWouldOvercap: true);
	var guardedInput = input with { Target = input.Target with { HasGuard = true } };

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseHarmonicArrow(input), "Harmonic Arrow should spend before wasting a charge on an unblocked target");
	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseHarmonicArrow(guardedInput), "Harmonic Arrow overcap should still respect blocked damage");
}

static void BardHarmonicArrowAcceptsLowMpConversion()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: PvPScoringFactors.LowMp));

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseHarmonicArrow(input), "Harmonic Arrow should convert high-health low MP pressure");
}

static void BardPitchPerfectAcceptsRepertoireAllyFocusFollowUp()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.75f, currentMp: 10_000, hasAllyFocus: true),
		followUpAvailable: true,
		hasRepertoire: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should convert Repertoire into an ally focused follow up");
}

static void BardPitchPerfectAcceptsRepertoireLowMpTarget()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: PvPScoringFactors.LowMp),
		hasRepertoire: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should convert Repertoire into high-health low MP pressure");
}

static void BardPitchPerfectAcceptsRepertoireObjectiveTarget()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000, isObjectiveRelevant: true),
		hasRepertoire: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should convert Repertoire into objective pressure");
}

static void BardPitchPerfectAcceptsRepertoireAllyBurst()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000),
		alliesCanBurst: true,
		hasRepertoire: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should convert Repertoire during allied burst");
}

static void BardPitchPerfectRejectsRepertoireFiller()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000),
		hasRepertoire: true);

	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUsePitchPerfect(input), "Pitch Perfect should hold Repertoire when the target has no pressure value");
}

static void BardApexArrowAcceptsObjectiveLineValue()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: 10_000,
			isObjectiveRelevant: true,
			lineTargetCount: 2),
		objectiveControlNeeded: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend when the line pressures an objective target");
}

static void BardApexArrowAcceptsGuardedObjectivePressure()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: 10_000,
			hasGuard: true,
			isObjectiveRelevant: true),
		objectiveControlNeeded: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend into Guard when objective pressure is valuable");
}

static void BardApexArrowAcceptsGuardedForcedTiming()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.85f, currentMp: 10_000, hasGuard: true),
		forcedExpiryWindow: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend into Guard when buff timing would be lost");
}

static void BardApexArrowAcceptsStandaloneObjectiveValue()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: 10_000,
			isObjectiveRelevant: true),
		objectiveControlNeeded: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend for objective value without requiring line value");
}

static void BardApexArrowAcceptsStandaloneAllyBurstValue()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.85f, currentMp: 10_000),
		alliesCanBurst: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should spend for ally burst without requiring line value");
}

static void BardApexArrowRejectsGuardedFiller()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.85f, currentMp: 10_000, hasGuard: true));

	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseApexArrow(input), "Apex Arrow should not spend filler into Guard");
}

static void BardBlastArrowAcceptsObjectiveDisplacement()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: 10_000,
			isObjectiveRelevant: true),
		objectiveControlNeeded: true,
		hasBlastArrowReady: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseBlastArrow(input), "Blast Arrow should spend for objective displacement");
}

static void BardBlastArrowRejectsResilienceDisplacement()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: 10_000,
			isObjectiveRelevant: true),
		objectiveControlNeeded: true,
		hasBlastArrowReady: true);
	var resilientInput = input with { Target = input.Target with { HasResilience = true } };

	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseBlastArrow(resilientInput), "Blast Arrow should reject Resilience when displacement is the primary value");
}

static void BardBlastArrowRejectsBlastReadyFiller()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.85f, currentMp: 10_000),
		hasBlastArrowReady: true);

	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseBlastArrow(input), "Blast Arrow should not spend Blast Ready without line, objective, peel, or committed follow up value");
}

static void BardEncoreOfLightAcceptsLowMpConversion()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: PvPScoringFactors.LowMp,
			guardAvailability: PvPGuardAvailability.CoolingDown),
		hasFinalFantasia: true,
		hasFrontlinersMarch: true,
		hasGuardCooldownKnowledge: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should convert low MP pressure when Guard is unavailable");
}

static void BardEncoreOfLightAcceptsAllyBurstWindow()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: 10_000,
			guardAvailability: PvPGuardAvailability.Ready),
		alliesCanBurst: true,
		hasFrontlinersMarch: true,
		hasGuardCooldownKnowledge: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should spend for ally burst even when Guard can react");
}

static void BardEncoreOfLightAcceptsFinalFantasiaPushWindow()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: 10_000,
			guardAvailability: PvPGuardAvailability.Ready),
		hasFinalFantasia: true,
		hasGuardCooldownKnowledge: true);

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should spend for Final Fantasia push windows");
}

static void BardEncoreOfLightRejectsBlockedFiller()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: PvPScoringFactors.LowMp,
			hasGuard: true,
			guardAvailability: PvPGuardAvailability.Active),
		hasFinalFantasia: true,
		hasFrontlinersMarch: true);

	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should not spend into blocked damage");
}

static void BardEncoreOfLightRejectsGuardReactionConversion()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: PvPScoringFactors.LowMp,
			guardAvailability: PvPGuardAvailability.Ready),
		hasFrontlinersMarch: true,
		hasGuardCooldownKnowledge: true);

	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should hold low MP conversion when the target can Guard and no priority signal exists");
}

static void BardEncoreOfLightRejectsUnknownGuardReactionConversion()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.85f,
			currentMp: PvPScoringFactors.LowMp,
			guardAvailability: PvPGuardAvailability.Unknown),
		hasFrontlinersMarch: true);

	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUseEncoreOfLight(input), "Encore of Light should hold low MP conversion when Guard reaction knowledge is unavailable");
}

static void BardPowerfulShotAcceptsSafePressureFiller()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.55f, currentMp: PvPScoringFactors.MediumMp));

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(input), "Powerful Shot should fill safe pressure into a low resource kill window");
}

static void BardPowerfulShotAcceptsNeutralSafeFiller()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(1, healthRatio: 0.90f, currentMp: 10_000));

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(input), "Powerful Shot should remain available as safe neutral filler");
}

static void BardPowerfulShotRejectsBlockedTarget()
{
	var input = BardOffensiveInput(
		BardOffensiveTarget(
			1,
			healthRatio: 0.55f,
			currentMp: PvPScoringFactors.MediumMp,
			hasGuard: true));

	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(input), "Powerful Shot should not spend into blocked targets");
}

static void BardOffensiveDecisionPolicyRerunsLiveGuardState()
{
	var target = BardOffensiveTarget(1, healthRatio: 0.55f, currentMp: PvPScoringFactors.MediumMp);
	var clearInput = BardOffensiveInput(target);
	var guardedInput = clearInput with { Target = target with { HasGuard = true } };

	AssertTrue(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(clearInput), "Bard should accept safe pressure before a live Guard refresh");
	AssertFalse(BardPvPOffensiveDecisionPolicy.ShouldUsePowerfulShot(guardedInput), "Bard should reject the same target after live state refresh shows Guard");
}

static void BardTargetRefreshUpdatesLiveSpatialSignals()
{
	const ulong targetId = 1;
	const float staleHealthRatio = 0.75f;
	const uint fullMp = 10_000;
	const int staleTargetCount = 1;
	const int expectedLineTargetCount = 3;
	const int expectedSplashTargetCount = 4;
	var staleSnapshot = BardOffensiveTarget(
		targetId,
		healthRatio: staleHealthRatio,
		currentMp: fullMp,
		isExposed: false,
		isInNormalRange: false,
		lineTargetCount: staleTargetCount,
		splashTargetCount: staleTargetCount);

	var spatialState = new BardPvPTargetSpatialState(
		IsInNormalRange: true,
		LineTargetCount: expectedLineTargetCount,
		SplashTargetCount: expectedSplashTargetCount);

	var refreshedSnapshot = BardPvPTargetSnapshotRefresher.RefreshSpatialState(staleSnapshot, spatialState);

	AssertTrue(refreshedSnapshot.IsInNormalRange, "refresh should replace stale range state");
	AssertTrue(refreshedSnapshot.IsExposed, "refresh should recompute exposure from live Guard and range state");
	AssertEqual(expectedLineTargetCount, refreshedSnapshot.LineTargetCount, "refresh should replace stale line target count");
	AssertEqual(expectedSplashTargetCount, refreshedSnapshot.SplashTargetCount, "refresh should replace stale splash target count");
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
	bool hasInvulnerability = false,
	double effectiveHealthRatio = 1.0,
	double activeDamageReduction = 0.0,
	double expectedDamageRatio = 0.0,
	bool isExposed = true,
	bool isInNormalRange = true,
	bool isInCloseRange = false,
	PvPGuardAvailability guardAvailability = PvPGuardAvailability.CoolingDown)
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
		HasInvulnerability: hasInvulnerability,
		ExpectedDamageRatio: expectedDamageRatio,
		EffectiveHealthRatio: effectiveHealthRatio,
		ActiveDamageReduction: activeDamageReduction,
		IsExposed: isExposed,
		IsInNormalRange: isInNormalRange,
		IsInCloseRange: isInCloseRange,
		GuardAvailability: guardAvailability);
}

static BardPvPKillSecureSnapshot BardKillTarget(
	ulong targetId,
	float healthRatio,
	double effectiveHealthRatio,
	double expectedDamageRatio,
	bool hasInvulnerability = false)
{
	return new BardPvPKillSecureSnapshot(
		TargetId: targetId,
		HealthRatio: healthRatio,
		EffectiveHealthRatio: effectiveHealthRatio,
		ExpectedDamageRatio: expectedDamageRatio,
		ActiveDamageReduction: 0.0,
		HasInvulnerability: hasInvulnerability);
}

static BardPvPTargetSnapshot BardOffensiveTarget(
	ulong targetId,
	float healthRatio,
	uint currentMp,
	bool hasGuard = false,
	bool hasResilience = false,
	bool isObjectiveRelevant = false,
	bool hasAllyFocus = false,
	bool isVulnerable = false,
	bool isControlled = false,
	bool hasInvulnerability = false,
	double effectiveHealthRatio = 1.0,
	double guardPiercingEffectiveHealthRatio = 1.0,
	double activeDamageReduction = 0.0,
	double expectedDamageRatio = 0.0,
	bool isExposed = true,
	bool isInNormalRange = true,
	int lineTargetCount = 1,
	int splashTargetCount = 1,
	PvPGuardAvailability guardAvailability = PvPGuardAvailability.CoolingDown)
{
	return new BardPvPTargetSnapshot(
		TargetId: targetId,
		HealthRatio: healthRatio,
		CurrentMp: currentMp,
		HasGuard: hasGuard,
		HasResilience: hasResilience,
		IsObjectiveRelevant: isObjectiveRelevant,
		HasAllyFocus: hasAllyFocus,
		IsVulnerable: isVulnerable,
		IsControlled: isControlled,
		HasInvulnerability: hasInvulnerability,
		ExpectedDamageRatio: expectedDamageRatio,
		EffectiveHealthRatio: effectiveHealthRatio,
		GuardPiercingEffectiveHealthRatio: guardPiercingEffectiveHealthRatio,
		ActiveDamageReduction: activeDamageReduction,
		IsExposed: isExposed,
		IsInNormalRange: isInNormalRange,
		LineTargetCount: lineTargetCount,
		SplashTargetCount: splashTargetCount,
		GuardAvailability: guardAvailability);
}

static BardPvPOffensiveDecisionInput BardOffensiveInput(
	BardPvPTargetSnapshot target,
	bool followUpAvailable = false,
	bool alliesCanBurst = false,
	bool objectiveControlNeeded = false,
	bool targetCommitted = false,
	bool hasFinalFantasia = false,
	bool hasFrontlinersMarch = false,
	bool hasRepertoire = false,
	bool hasBlastArrowReady = false,
	bool harmonicWouldOvercap = false,
	bool forcedExpiryWindow = false,
	bool peelValueNeeded = false,
	double expectedDamageRatio = 0.0,
	bool hasGuardCooldownKnowledge = false)
{
	return new BardPvPOffensiveDecisionInput(
		Target: target,
		FollowUpAvailable: followUpAvailable,
		AlliesCanBurst: alliesCanBurst,
		ObjectiveControlNeeded: objectiveControlNeeded,
		TargetCommitted: targetCommitted,
		HasFinalFantasia: hasFinalFantasia,
		HasFrontlinersMarch: hasFrontlinersMarch,
		HasRepertoire: hasRepertoire,
		HasBlastArrowReady: hasBlastArrowReady,
		HarmonicWouldOvercap: harmonicWouldOvercap,
		ForcedExpiryWindow: forcedExpiryWindow,
		PeelValueNeeded: peelValueNeeded,
		ExpectedDamageRatio: expectedDamageRatio,
		HasGuardCooldownKnowledge: hasGuardCooldownKnowledge);
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

static void PvpMitigationJsonContainsRankedCcDefensiveCoverage()
{
	using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("RotationSolver.Basic", "Data", "PvPMitigations.json")));
	var root = document.RootElement;
	var entries = new Dictionary<string, (string Kind, double DamageReductionPercent)>();

	foreach (var entry in root.EnumerateArray())
	{
		entries[GetRequiredString(entry, "Id")] = (
			GetRequiredString(entry, "Kind"),
			GetRequiredDouble(entry, "DamageReductionPercent"));
	}

	var expected = new Dictionary<string, (string Kind, double DamageReductionPercent)>
	{
		["Guard"] = ("Invuln", 0.0),
		["HallowedGround_1302"] = ("Invuln", 0.0),
		["GuardiansWill"] = ("Invuln", 0.0),
		["Phalanx"] = ("HeavyDR", 0.33),
		["UndeadRedemption"] = ("Invuln", 0.0),
		["Hidden_1316"] = ("Invuln", 0.0),
		["HardenedScales"] = ("HeavyDR", 0.50),
		["Forte"] = ("HeavyDR", 0.50),
		["WardensGrace"] = ("HeavyDR", 0.25),
		["RelentlessRush"] = ("HeavyDR", 0.25),
		["RadiantAegis_3224"] = ("HeavyDR", 0.25),
		["FanDance"] = ("HeavyDR", 0.20),
		["SaltedEarth_3037"] = ("HeavyDR", 0.20),
		["ClarityOfCorundum"] = ("HeavyDR", 0.10),
		["Catalyze"] = ("HeavyDR", 0.10),
	};

	foreach (var (id, expectedEntry) in expected)
	{
		AssertTrue(entries.TryGetValue(id, out var actual), $"PvPMitigations.json should include ranked CC defensive status {id}");
		AssertEqual(expectedEntry.Kind, actual.Kind, $"PvPMitigations.json should classify {id}");
		AssertEqual(expectedEntry.DamageReductionPercent, actual.DamageReductionPercent, $"PvPMitigations.json should set DR for {id}");
	}
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
