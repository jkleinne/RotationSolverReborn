using System.Numerics;
using RotationSolver.Basic.Actions.PvPTargetSelection;
using RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

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
	private const uint MarksmansSpitePvPActionId = 29415;
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

		var snapshot = CreateTargetSnapshot(target, intent, analysisWillBuffAction: true);
		var input = CreateDecisionInput(snapshot, target, intent);
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
				&& (baseAction.ID == MarksmansSpitePvPActionId || baseAction.AdjustedID == MarksmansSpitePvPActionId))
			{
				return baseAction;
			}
		}

		return null;
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
		foreach (var targetSnapshot in RankTargets(intent))
		{
			var target = FindHostileById(targetSnapshot.TargetId);
			if (target == null)
			{
				continue;
			}

			var input = CreateDecisionInput(targetSnapshot, target, intent);
			if (!shouldUse(input))
			{
				continue;
			}

			if (TryUseActionOn(baseAction, targetSnapshot.TargetId, out action, usedUp, skipAoeCheck))
			{
				return true;
			}
		}

		return false;
	}

	private static List<MachinistPvPTargetSnapshot> RankTargets(MachinistPvPActionIntent intent)
	{
		List<MachinistPvPTargetSnapshot> snapshots = [];
		var analysisWillBuffAction = StatusHelper.PlayerHasStatus(true, StatusID.Analysis);
		foreach (var hostile in AllHostileTargets)
		{
			if (hostile == null || hostile.CurrentHp == 0)
			{
				continue;
			}

			snapshots.Add(CreateTargetSnapshot(hostile, intent, analysisWillBuffAction));
		}

		return MachinistPvPTargetPolicy.Rank(snapshots, intent);
	}

	private static MachinistPvPTargetSnapshot CreateTargetSnapshot(
		IBattleChara target,
		MachinistPvPActionIntent intent,
		bool analysisWillBuffAction)
	{
		var objectiveTargets = PvPObjectiveState.BuildObjectiveRelevantTargetIds();
		var distance = target.DistanceToPlayer();
		var hasGuard = target.HasStatus(false, StatusID.Guard);
		var hasResilience = target.HasStatus(false, StatusID.Resilience);
		var mitigationDatabase = PvPMitigationDatabaseProvider.Current;
		var effectiveHp = EffectiveHpCalculator.Compute(target, mitigationDatabase);
		var effectiveHpRatio = target.MaxHp == 0 || double.IsPositiveInfinity(effectiveHp)
			? double.PositiveInfinity
			: effectiveHp / target.MaxHp;
		var range = intent switch
		{
			MachinistPvPActionIntent.MarksmanSpite => LimitBreakRangeYalms,
			MachinistPvPActionIntent.EagleEyeShot => EagleEyeShotRangeYalms,
			_ => NormalToolRangeYalms,
		};

		return new MachinistPvPTargetSnapshot(
			TargetId: target.GameObjectId,
			HealthRatio: target.GetHealthRatio(),
			CurrentMp: target.CurrentMp,
			HasGuard: hasGuard,
			HasResilience: hasResilience,
			IsObjectiveRelevant: objectiveTargets.Contains(target.GameObjectId),
			HasAllyFocus: CountAlliesTargeting(target) > 0,
			IsVulnerable: false,
			HasInvulnerability: HasNonGuardInvulnerability(target, mitigationDatabase),
			HasWildfire: target.HasStatus(true, StatusID.Wildfire, StatusID.Wildfire_1323),
			ExpectedDamageRatio: ExpectedDamageRatio(intent, target, analysisWillBuffAction),
			EffectiveHealthRatio: effectiveHpRatio,
			ActiveDamageReduction: MitigationPenalty.Compute(target, mitigationDatabase),
			IsExposed: !hasGuard && distance <= range,
			IsInNormalRange: distance <= range,
			IsInCloseRange: distance <= CloseToolRangeYalms);
	}

	private static bool HasNonGuardInvulnerability(IBattleChara target, IMitigationDatabase database)
	{
		var statusList = target.StatusList;
		if (statusList == null)
		{
			return false;
		}

		foreach (var status in statusList)
		{
			var statusId = (StatusID)status.StatusId;
			if (statusId == StatusID.Guard)
			{
				continue;
			}

			if (database.TryGet(statusId, out var entry) && entry.Kind == MitigationKind.Invuln)
			{
				return true;
			}
		}

		return false;
	}

	private MachinistPvPDecisionInput CreateDecisionInput(
		MachinistPvPTargetSnapshot snapshot,
		IBattleChara target,
		MachinistPvPActionIntent intent)
	{
		var objectiveControlNeeded = snapshot.IsObjectiveRelevant || IsForcedTeamfight(target);
		return new MachinistPvPDecisionInput(
			Target: snapshot,
			SafeCloseRange: IsSafeCloseRange(target, objectiveControlNeeded),
			FollowUpAvailable: HasImmediateFollowUp(intent),
			AlliesCanBurst: snapshot.HasAllyFocus || CountAlliesNear(target, TeamfightRadiusYalms) > 0,
			ObjectiveControlNeeded: objectiveControlNeeded,
			TargetCommitted: IsTargetCommitted(snapshot, target, objectiveControlNeeded),
			ExpectedDamageRatio: snapshot.ExpectedDamageRatio);
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
		var originalCanTarget = baseAction.Setting.CanTarget;
		baseAction.Setting.CanTarget = candidate =>
			originalCanTarget(candidate) && candidate.GameObjectId == targetId;

		try
		{
			return baseAction.CanUse(
				out action,
				usedUp: usedUp,
				skipAoeCheck: skipAoeCheck,
				targetOverride: TargetType.Nearest);
		}
		finally
		{
			baseAction.Setting.CanTarget = originalCanTarget;
		}
	}

	private static IBattleChara? FindHostileById(ulong targetId)
	{
		foreach (var hostile in AllHostileTargets)
		{
			if (hostile != null && hostile.GameObjectId == targetId)
			{
				return hostile;
			}
		}

		return null;
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

	private static bool IsSafeCloseRange(IBattleChara target, bool objectiveControlNeeded)
	{
		if (Player == null || target.DistanceToPlayer() > CloseToolRangeYalms)
		{
			return false;
		}

		if (objectiveControlNeeded || target.GetHealthRatio() <= CloseRangeCommitHealthRatio)
		{
			return true;
		}

		return CountHostilesNear(Player.Position, CloseToolRangeYalms) <= SafeCloseRangeHostileLimit;
	}

	private static bool IsForcedTeamfight(IBattleChara target)
	{
		return CountHostilesNear(target.Position, TeamfightRadiusYalms) >= ForcedTeamfightHostileCount;
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

	private static int CountAlliesTargeting(IBattleChara target)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (member != null && member.TargetObjectId == target.GameObjectId)
			{
				count++;
			}
		}

		return count;
	}

	private static int CountAlliesNear(IBattleChara target, float radius)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (member != null
				&& member.CurrentHp > 0
				&& Vector3.Distance(member.Position, target.Position) <= radius)
			{
				count++;
			}
		}

		return count;
	}

	private static int CountHostilesNear(Vector3 position, float radius)
	{
		var count = 0;
		foreach (var hostile in AllHostileTargets)
		{
			if (hostile != null
				&& hostile.CurrentHp > 0
				&& Vector3.Distance(hostile.Position, position) <= radius)
			{
				count++;
			}
		}

		return count;
	}
	#endregion
}
