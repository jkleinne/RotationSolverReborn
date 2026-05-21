using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.5")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/MCH_Default.PvP.cs")]

public sealed class MCH_DefaultPvP : MachinistRotation
{
	private const float NormalToolRangeYalms = 25f;
	private const float EagleEyeShotRangeYalms = 40f;
	private const float CloseToolRangeYalms = 12f;
	private const float LimitBreakRangeYalms = 50f;
	private const float TeamfightRadiusYalms = 8f;
	private const float CloseRangeCommitHealthRatio = 0.25f;
	private const int ForcedTeamfightHostileCount = 2;
	private const int SafeCloseRangeHostileLimit = 2;
	private const double MarksmanGuardReactionWindowSeconds = 1.25;
	private const double MarksmansSpitePotency = 40_000.0;
	private const double EagleEyeShotPotency = 12_000.0;
	private const double DrillPotency = 10_000.0;
	private const double AnalysisDrillPotency = 20_000.0;
	private const double BioblasterPotency = 4_000.0;
	private const double AnalysisBioblasterPotency = 6_000.0;
	private const double AirAnchorPotency = 8_000.0;
	private const double AnalysisAirAnchorPotency = 12_000.0;
	private const double ChainSawPotency = 12_000.0;
	private const double ScattergunPotency = 8_000.0;
	private const double FullMetalFieldPrimaryPotency = 15_000.0;
	private const double BlazingShotPotency = 8_000.0;

	#region Configurations
	#endregion

	#region oGCDs
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
	{
		return base.EmergencyAbility(nextGCD, out action);
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? action)
	{
		if (TryUseAnalysisFor(nextGCD, out action))
		{
			return true;
		}

		if (TryUseMarksmanSpite(out action))
		{
			return true;
		}

		if (TryUsePolicyAction(
			WildfirePvP,
			MachinistPvPActionIntent.Wildfire,
			MachinistPvPDecisionPolicy.ShouldUseWildfire,
			out action))
		{
			return true;
		}

		if (TryUseFrontlineEagleEyeShot(out action))
		{
			return true;
		}

		if (TryUsePolicyAction(
			BishopAutoturretPvP,
			MachinistPvPActionIntent.Bishop,
			MachinistPvPDecisionPolicy.ShouldUseBishop,
			out action))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out action);
	}

	#endregion

	#region GCDs
	protected override bool GeneralGCD(out IAction? action)
	{
		if (TryUseBurstGcd(out action))
		{
			return true;
		}

		if (TryUseAnalysisTool(out action))
		{
			return true;
		}

		if (TryUseScattergun(out action))
		{
			return true;
		}

		if (BlastChargePvP.CanUse(out action))
		{
			return true;
		}

		return base.GeneralGCD(out action);
	}

	private bool TryUseBurstGcd(out IAction? action)
	{
		if (TryUsePolicyAction(
			FullMetalFieldPvP,
			MachinistPvPActionIntent.FullMetalField,
			MachinistPvPDecisionPolicy.ShouldUseFullMetalField,
			out action))
		{
			return true;
		}

		return StatusHelper.PlayerHasStatus(true, StatusID.Overheated_3149)
			&& !StatusHelper.PlayerHasStatus(true, StatusID.Analysis)
			&& TryUsePolicyAction(
				BlazingShotPvP,
				MachinistPvPActionIntent.BlazingShot,
				MachinistPvPDecisionPolicy.ShouldUseBlazingShot,
				out action);
	}

	private bool TryUseAnalysisTool(out IAction? action)
	{
		if (TryUsePolicyAction(
			DrillPvP,
			MachinistPvPActionIntent.AnalysisDrill,
			MachinistPvPDecisionPolicy.ShouldUseAnalysisDrill,
			out action,
			usedUp: true))
		{
			return true;
		}

		if (TryUsePolicyAction(
			AirAnchorPvP,
			MachinistPvPActionIntent.AnalysisAirAnchor,
			MachinistPvPDecisionPolicy.ShouldUseAnalysisAirAnchor,
			out action,
			usedUp: true))
		{
			return true;
		}

		if (TryUsePolicyAction(
			ChainSawPvP,
			MachinistPvPActionIntent.AnalysisChainSaw,
			MachinistPvPDecisionPolicy.ShouldUseAnalysisChainSaw,
			out action,
			usedUp: true))
		{
			return true;
		}

		return TryUsePolicyAction(
			BioblasterPvP,
			MachinistPvPActionIntent.AnalysisBioblaster,
			MachinistPvPDecisionPolicy.ShouldUseAnalysisBioblaster,
			out action,
			usedUp: true);
	}

	private bool TryUseScattergun(out IAction? action)
	{
		action = null;

		return !StatusHelper.PlayerHasStatus(true, StatusID.Overheated_3149)
			&& TryUsePolicyAction(
				ScattergunPvP,
				MachinistPvPActionIntent.Scattergun,
				MachinistPvPDecisionPolicy.ShouldUseScattergun,
				out action);
	}

	private bool TryUseAnalysisFor(IAction nextGCD, out IAction? action)
	{
		action = null;

		if (nextGCD is not IBaseAction nextGcdAction || !TryGetAnalysisIntent(nextGCD, out var intent))
		{
			return false;
		}

		var target = nextGcdAction.Target.Target;
		if (target == null || target.CurrentHp == 0)
		{
			return false;
		}

		var liveFrame = CreateLiveTargetFrame(intent);
		var snapshot = CreateTargetSnapshot(
			target,
			intent,
			analysisWillBuffAction: true,
			context: liveFrame.FactsContext);
		var input = CreateDecisionInput(snapshot, target, intent, liveFrame.Allies, liveFrame.Hostiles);
		if (!ShouldUseAnalysis(intent, input))
		{
			return false;
		}

		return AnalysisPvP.CanUse(out action, usedUp: true);
	}

	private static bool TryGetAnalysisIntent(IAction action, out MachinistPvPActionIntent intent)
	{
		if (action.IsTheSameTo(false, ActionID.DrillPvP))
		{
			intent = MachinistPvPActionIntent.AnalysisDrill;
			return true;
		}

		if (action.IsTheSameTo(false, ActionID.AirAnchorPvP))
		{
			intent = MachinistPvPActionIntent.AnalysisAirAnchor;
			return true;
		}

		if (action.IsTheSameTo(false, ActionID.ChainSawPvP))
		{
			intent = MachinistPvPActionIntent.AnalysisChainSaw;
			return true;
		}

		if (action.IsTheSameTo(false, ActionID.BioblasterPvP))
		{
			intent = MachinistPvPActionIntent.AnalysisBioblaster;
			return true;
		}

		intent = default;
		return false;
	}

	private static bool ShouldUseAnalysis(MachinistPvPActionIntent intent, MachinistPvPDecisionInput input)
	{
		return intent switch
		{
			MachinistPvPActionIntent.AnalysisDrill => MachinistPvPDecisionPolicy.ShouldUseAnalysisDrill(input),
			MachinistPvPActionIntent.AnalysisAirAnchor => MachinistPvPDecisionPolicy.ShouldUseAnalysisAirAnchor(input),
			MachinistPvPActionIntent.AnalysisChainSaw => MachinistPvPDecisionPolicy.ShouldUseAnalysisChainSaw(input),
			MachinistPvPActionIntent.AnalysisBioblaster => MachinistPvPDecisionPolicy.ShouldUseAnalysisBioblaster(input),
			_ => false,
		};
	}

	private bool TryUseFrontlineEagleEyeShot(out IAction? action)
	{
		action = null;

		return TryUsePolicyAction(
			EagleEyeShotPvP,
			MachinistPvPActionIntent.EagleEyeShot,
			ShouldUseFrontlineEagleEyeShot,
			out action);
	}

	private static bool ShouldUseFrontlineEagleEyeShot(MachinistPvPDecisionInput input)
	{
		var target = input.Target;
		var eagleEyeShotInput = new FrontlineEagleEyeShotInput(
			Job: FrontlinePvPRangedJob.Machinist,
			IsInFrontline: DataCenter.IsInFrontline,
			IsInCrystallineConflict: DataCenter.IsInCrystallineConflict,
			Target: new FrontlineEagleEyeShotTargetState(
				HealthRatio: target.HealthRatio,
				CurrentMp: target.CurrentMp,
				HasGuard: target.HasGuard,
				HasResilience: target.HasResilience,
				HasNonGuardInvulnerability: target.HasInvulnerability,
				HasAllyFocus: target.HasAllyFocus,
				IsObjectiveRelevant: target.IsObjectiveRelevant,
				IsControlled: false,
				IsBurstWorthy: target.IsVulnerable,
				TargetCommitted: input.TargetCommitted,
				ImmediateFollowUpAvailable: input.FollowUpAvailable,
				HasWildfire: target.HasWildfire,
				ExpectedDamageRatio: input.ExpectedDamageRatio));

		return FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(eagleEyeShotInput);
	}

	private bool TryUseMarksmanSpite(out IAction? action)
	{
		action = null;
		var marksmanSpite = FindMarksmansSpiteAction();
		if (marksmanSpite == null)
		{
			return false;
		}

		return TryUsePolicyAction(
			marksmanSpite,
			MachinistPvPActionIntent.MarksmanSpite,
			MachinistPvPDecisionPolicy.ShouldUseMarksmanSpite,
			out action);
	}

	private IBaseAction? FindMarksmansSpiteAction()
	{
		foreach (var candidate in AllActions)
		{
			if (candidate is IBaseAction baseAction
				&& MachinistPvPDecisionPolicy.IsDirectMarksmansSpiteAction(baseAction.ID, baseAction.AdjustedID))
			{
				return baseAction;
			}
		}

		return null;
	}

	private static bool ShouldVetoMarksmanSpiteForLiveGuard(
		MachinistPvPActionIntent intent,
		IBaseAction action,
		IBattleChara target,
		out MachinistPvPLiveGuardInput input)
	{
		if (intent != MachinistPvPActionIntent.MarksmanSpite)
		{
			input = default;
			return false;
		}

		var hasGuard = target.HasStatus(false, StatusID.Guard);
		var guardWillExpire = target.WillStatusEnd((float)action.Info.CastTime, false, StatusID.Guard);
		input = new MachinistPvPLiveGuardInput(
			TargetHasGuard: hasGuard,
			GuardWillExpireBeforeAction: guardWillExpire);

		return MachinistPvPDecisionPolicy.ShouldVetoMarksmanSpiteForLiveGuard(input);
	}

	private bool TryUsePolicyAction(
		IBaseAction baseAction,
		MachinistPvPActionIntent intent,
		Func<MachinistPvPDecisionInput, bool> shouldUse,
		out IAction? action,
		bool usedUp = false,
		bool skipAoeCheck = false)
	{
		action = null;
		var rankedFrame = RankTargets(intent);
		foreach (var targetSnapshot in rankedFrame.RankedTargets)
		{
			var target = PvPLiveTargetFactsBuilder.FindLiveTargetById(AllHostileTargets, targetSnapshot.TargetId);
			if (target == null)
			{
				continue;
			}

			var input = CreateDecisionInput(
				targetSnapshot,
				target,
				intent,
				rankedFrame.LiveFrame.Allies,
				rankedFrame.LiveFrame.Hostiles);
			if (!shouldUse(input))
			{
				TraceMarksmanSpiteDecision("policy_reject", baseAction, target, targetSnapshot, input);
				continue;
			}

			if (ShouldVetoMarksmanSpiteForLiveGuard(intent, baseAction, target, out var liveGuardInput))
			{
				TraceMarksmanSpiteDecision("live_guard_veto", baseAction, target, targetSnapshot, input, liveGuardInput);
				continue;
			}

			if (TryUseActionOn(baseAction, targetSnapshot.TargetId, out action, usedUp, skipAoeCheck))
			{
				TraceMarksmanSpiteDecision("accepted", baseAction, target, targetSnapshot, input);
				return true;
			}

			TraceMarksmanSpiteDecision("canuse_reject", baseAction, target, targetSnapshot, input);
		}

		return false;
	}

	private static void TraceMarksmanSpiteDecision(
		string outcome,
		IBaseAction action,
		IBattleChara target,
		MachinistPvPTargetSnapshot snapshot,
		MachinistPvPDecisionInput decisionInput,
		MachinistPvPLiveGuardInput? liveGuardInput = null)
	{
		if (!ActionTracer.Enabled || action.ID != MachinistPvPDecisionPolicy.MarksmansSpitePvPActionId)
		{
			return;
		}

		var liveGuard = liveGuardInput ?? new MachinistPvPLiveGuardInput(
			TargetHasGuard: target.HasStatus(false, StatusID.Guard),
			GuardWillExpireBeforeAction: target.WillStatusEnd((float)action.Info.CastTime, false, StatusID.Guard));
		string[] traceFields =
		[
			$"outcome={outcome}",
			$"actionId={action.ID}",
			$"adjustedId={action.AdjustedID}",
			$"ignoreGuard={action.Setting.IgnoreGuard}",
			$"targetId={target.GameObjectId}",
			$"hp={snapshot.HealthRatio:F3}",
			$"mp={snapshot.CurrentMp}",
			$"snapshotGuard={snapshot.HasGuard}",
			$"liveGuard={liveGuard.TargetHasGuard}",
			$"guardExpires={liveGuard.GuardWillExpireBeforeAction}",
			$"guardAvailability={snapshot.GuardAvailability}",
			$"effectiveHp={snapshot.EffectiveHealthRatio:F3}",
			$"expectedDamage={decisionInput.ExpectedDamageRatio:F3}",
			$"followUp={decisionInput.FollowUpAvailable}",
			$"alliesBurst={decisionInput.AlliesCanBurst}",
			$"objective={decisionInput.ObjectiveControlNeeded}",
			$"committed={decisionInput.TargetCommitted}",
		];
		var message = string.Join(" ", traceFields);
		ActionTracer.Note(
			"MCH_LB",
			message);
	}

	private readonly record struct MachinistPvPLiveTargetFrame(
		PvPLiveTargetFactsContext FactsContext,
		IReadOnlyList<PvPCombatantSnapshot> Allies,
		IReadOnlyList<PvPCombatantSnapshot> Hostiles);

	private readonly record struct MachinistPvPRankedTargetFrame(
		IReadOnlyList<MachinistPvPTargetSnapshot> RankedTargets,
		MachinistPvPLiveTargetFrame LiveFrame);

	private static MachinistPvPRankedTargetFrame RankTargets(MachinistPvPActionIntent intent)
	{
		List<MachinistPvPTargetSnapshot> snapshots = [];
		var analysisWillBuffAction = StatusHelper.PlayerHasStatus(true, StatusID.Analysis);
		var liveFrame = CreateLiveTargetFrame(intent);
		foreach (var hostile in AllHostileTargets)
		{
			if (hostile == null || hostile.CurrentHp == 0)
			{
				continue;
			}

			snapshots.Add(CreateTargetSnapshot(hostile, intent, analysisWillBuffAction, liveFrame.FactsContext));
		}

		return new MachinistPvPRankedTargetFrame(
			MachinistPvPTargetPolicy.Rank(snapshots, intent),
			liveFrame);
	}

	private static MachinistPvPLiveTargetFrame CreateLiveTargetFrame(MachinistPvPActionIntent intent)
	{
		var hostiles = PvPLiveTargetFactsBuilder.ToCombatantSnapshots(
			AllHostileTargets,
			target => target.GetHealthRatio());
		var allies = PvPLiveTargetFactsBuilder.ToCombatantSnapshots(
			PartyMembers,
			target => target.GetHealthRatio(),
			excludedObjectId: Player?.GameObjectId ?? 0);
		var objectiveTargets = PvPObjectiveState.BuildObjectiveRelevantTargetIds();

		return new MachinistPvPLiveTargetFrame(
			CreateFactsContext(intent, allies, objectiveTargets),
			allies,
			hostiles);
	}

	private static PvPLiveTargetFactsContext CreateFactsContext(
		MachinistPvPActionIntent intent,
		IReadOnlyList<PvPCombatantSnapshot> allies,
		IReadOnlySet<ulong> objectiveTargets)
	{
		return new PvPLiveTargetFactsContext(
			MitigationDatabase: PvPMitigationDatabaseProvider.Current,
			ObjectiveRelevantTargetIds: objectiveTargets,
			Allies: allies,
			CurrentTime: TimeSpan.FromMilliseconds(Environment.TickCount64),
			GuardCooldownTracker: DataCenter.PvPGuardCooldownTracker,
			GuardReactionWindow: TimeSpan.FromSeconds(MarksmanGuardReactionWindowSeconds),
			ActionRange: ResolveActionRange(intent),
			DistanceToPlayerProvider: target => target.DistanceToPlayer(),
			HealthRatioProvider: target => target.GetHealthRatio(),
			HasStatus: (target, statusId) => target.HasStatus(false, statusId));
	}

	private static float ResolveActionRange(MachinistPvPActionIntent intent)
	{
		return intent switch
		{
			MachinistPvPActionIntent.MarksmanSpite => LimitBreakRangeYalms,
			MachinistPvPActionIntent.EagleEyeShot => EagleEyeShotRangeYalms,
			_ => NormalToolRangeYalms,
		};
	}

	private static MachinistPvPTargetSnapshot CreateTargetSnapshot(
		IBattleChara target,
		MachinistPvPActionIntent intent,
		bool analysisWillBuffAction,
		PvPLiveTargetFactsContext context)
	{
		var facts = PvPLiveTargetFactsBuilder.Create(target, context);

		return new MachinistPvPTargetSnapshot(
			TargetId: facts.TargetId,
			HealthRatio: facts.HealthRatio,
			CurrentMp: facts.CurrentMp,
			HasGuard: facts.HasGuard,
			HasResilience: facts.HasResilience,
			IsObjectiveRelevant: facts.IsObjectiveRelevant,
			AllyFocusCount: facts.AllyFocusCount,
			IsVulnerable: false,
			HasInvulnerability: facts.HasNonGuardInvulnerability,
			HasWildfire: target.HasStatus(true, StatusID.Wildfire, StatusID.Wildfire_1323),
			ExpectedDamageRatio: ExpectedDamageRatio(intent, target, analysisWillBuffAction),
			EffectiveHealthRatio: facts.EffectiveHealthRatio,
			ActiveDamageReduction: facts.ActiveDamageReduction,
			IsExposed: facts.IsExposed,
			IsInNormalRange: facts.IsInNormalRange,
			IsInCloseRange: context.DistanceToPlayerProvider(target) <= CloseToolRangeYalms,
			GuardAvailability: facts.GuardAvailability);
	}

	private MachinistPvPDecisionInput CreateDecisionInput(
		MachinistPvPTargetSnapshot snapshot,
		IBattleChara target,
		MachinistPvPActionIntent intent,
		IReadOnlyList<PvPCombatantSnapshot> allies,
		IReadOnlyList<PvPCombatantSnapshot> hostiles)
	{
		var objectiveControlNeeded = snapshot.IsObjectiveRelevant || IsForcedTeamfight(target, hostiles);
		return new MachinistPvPDecisionInput(
			Target: snapshot,
			SafeCloseRange: IsSafeCloseRange(snapshot, objectiveControlNeeded, hostiles),
			FollowUpAvailable: HasImmediateFollowUp(intent),
			AlliesCanBurst: snapshot.HasAllyFocus
				|| PvPCombatantQueries.CountAlliesNear(allies, target.Position, TeamfightRadiusYalms) > 0,
			ObjectiveControlNeeded: objectiveControlNeeded,
			TargetCommitted: IsTargetCommitted(snapshot, target, objectiveControlNeeded),
			ExpectedDamageRatio: snapshot.ExpectedDamageRatio,
			HasGuardCooldownKnowledge: DataCenter.IsInCrystallineConflict,
			StrictMarksmanExecuteOnly: Service.Config.MachinistMarksmansSpiteStrictExecuteOnly);
	}

	private static double ExpectedDamageRatio(
		MachinistPvPActionIntent intent,
		IBattleChara target,
		bool analysisWillBuffAction)
	{
		if (target.MaxHp == 0)
		{
			return 0.0;
		}

		return intent switch
		{
			MachinistPvPActionIntent.MarksmanSpite => MarksmansSpitePotency / target.MaxHp,
			MachinistPvPActionIntent.EagleEyeShot => EagleEyeShotPotency / target.MaxHp,
			MachinistPvPActionIntent.AnalysisDrill => (analysisWillBuffAction ? AnalysisDrillPotency : DrillPotency) / target.MaxHp,
			MachinistPvPActionIntent.AnalysisBioblaster => (analysisWillBuffAction ? AnalysisBioblasterPotency : BioblasterPotency) / target.MaxHp,
			MachinistPvPActionIntent.AnalysisAirAnchor => (analysisWillBuffAction ? AnalysisAirAnchorPotency : AirAnchorPotency) / target.MaxHp,
			MachinistPvPActionIntent.AnalysisChainSaw => ChainSawPotency / target.MaxHp,
			MachinistPvPActionIntent.Scattergun => ScattergunPotency / target.MaxHp,
			MachinistPvPActionIntent.FullMetalField => FullMetalFieldPrimaryPotency / target.MaxHp,
			MachinistPvPActionIntent.BlazingShot => BlazingShotPotency / target.MaxHp,
			_ => 0.0,
		};
	}

	private static bool TryUseActionOn(
		IBaseAction baseAction,
		ulong targetId,
		out IAction? action,
		bool usedUp,
		bool skipAoeCheck)
	{
		action = null;
		return PvPSingleTargetActionUse.TryUseOn(
			baseAction,
			targetId,
			new PvPSingleTargetActionOptions(
				UsedUp: usedUp,
				SkipAoeCheck: skipAoeCheck,
				TargetOverride: TargetType.Nearest),
			out action);
	}

	private bool HasImmediateFollowUp(MachinistPvPActionIntent intent)
	{
		if (StatusHelper.PlayerHasStatus(true, StatusID.Overheated_3149))
		{
			return true;
		}

		if (intent != MachinistPvPActionIntent.Wildfire && WildfirePvP.Cooldown.HasOneCharge)
		{
			return true;
		}

		return FullMetalFieldPvP.Cooldown.HasOneCharge
			|| IsPrimedToolReady(DrillPvP, StatusID.DrillPrimed)
			|| IsPrimedToolReady(AirAnchorPvP, StatusID.AirAnchorPrimed)
			|| IsPrimedToolReady(ChainSawPvP, StatusID.ChainSawPrimed)
			|| IsPrimedToolReady(BioblasterPvP, StatusID.BioblasterPrimed);
	}

	private static bool IsPrimedToolReady(IBaseAction action, StatusID primedStatus)
	{
		return action.Cooldown.HasOneCharge && StatusHelper.PlayerHasStatus(true, primedStatus);
	}

	private static bool IsSafeCloseRange(
		MachinistPvPTargetSnapshot snapshot,
		bool objectiveControlNeeded,
		IReadOnlyList<PvPCombatantSnapshot> hostiles)
	{
		if (Player == null || !snapshot.IsInCloseRange)
		{
			return false;
		}

		if (objectiveControlNeeded || snapshot.HealthRatio <= CloseRangeCommitHealthRatio)
		{
			return true;
		}

		return PvPCombatantQueries.CountHostilesNear(hostiles, Player.Position, CloseToolRangeYalms) <= SafeCloseRangeHostileLimit;
	}

	private static bool IsForcedTeamfight(IBattleChara target, IReadOnlyList<PvPCombatantSnapshot> hostiles)
	{
		return PvPCombatantQueries.CountHostilesNear(hostiles, target.Position, TeamfightRadiusYalms) >= ForcedTeamfightHostileCount;
	}

	private static bool IsTargetCommitted(
		MachinistPvPTargetSnapshot snapshot,
		IBattleChara target,
		bool objectiveControlNeeded)
	{
		return objectiveControlNeeded
			|| snapshot.HasAllyFocus
			|| snapshot.CurrentMp <= PvPScoringFactors.MediumMp
			|| target.TargetObjectId != 0;
	}

	#endregion
}
