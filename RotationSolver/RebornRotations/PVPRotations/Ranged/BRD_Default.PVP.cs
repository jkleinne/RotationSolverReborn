using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using RotationSolver.Basic.Actions.PvPTargetSelection;
using RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.5")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Ranged/BRD_Default.PvP.cs")]

public sealed class BRD_DefaultPvP : BardRotation
{
	private const uint BurstExpiryGcdWindow = 1;
	private const float BurstExpiryOffset = 0f;
	private const float PaeanCriticalHpThreshold = 0.35f;
	private const float PaeanLowHpThreshold = 0.55f;
	private const float PaeanHealthyEngageThreshold = 0.65f;
	private const float PaeanShortCombatDistance = 12f;
	private const int PaeanMaxFocusedHostilesForEngage = 1;
	private const float RepellingBackstepYalms = 10f;
	private const float PaeanPeelScoreThreshold = 2f;
	private const float PaeanEngageScoreThreshold = 3f;
	private const float PaeanCleanseBaseWeight = 100f;
	private const float PaeanCriticalHpWeight = 6f;
	private const float PaeanLowHpWeight = 2.5f;
	private const float PaeanFocusedHostileWeight = 2f;
	private const float PaeanHealerSupportRoleWeight = 3f;
	private const float PaeanRangedSupportRoleWeight = 2f;
	private const float PaeanMeleeSupportRoleWeight = 1.25f;
	private const float PaeanTankSupportRoleWeight = 0.5f;
	private const float PaeanDistanceWeight = 1f;
	private const float PaeanTankEngageWeight = 2.5f;
	private const float PaeanMeleeEngageWeight = 2f;
	private const float PaeanSmartTargetWeight = 1.5f;
	private const double PowerfulShotPotency = 6_000.0;
	private const double PitchPerfectPotency = 9_000.0;
	private const double ApexArrowPotency = 8_000.0;
	private const double HarmonicArrowPotency = 9_000.0;
	private const double BlastArrowPotency = 10_000.0;
	private const double EncoreOfLightPotency = 10_000.0;
	private const double EagleEyeShotPotency = 12_000.0;

	private readonly record struct PaeanCandidate(IBattleChara Target, float Score);

	private enum PaeanCastIntent
	{
		Cleanse,
		Protect,
	}

	#region Configurations

	[RotationConfig(CombatType.PvP, Name = "Use Warden's Paean on other players")]
	public bool BRDEsuna2 { get; set; } = true;
	#endregion

	#region oGCDs
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
	{
		if (StatusHelper.PlayerHasStatus(false, StatusHelper.PurifyPvPStatuses))
		{
			if (TheWardensPaeanPvP.CanUse(out action, targetOverride: TargetType.Self))
			{
				return true;
			}
		}

		if (BRDEsuna2 && TryUseSupportPaean(out action))
		{
			return true;
		}

		if (BraveryPvP.CanUse(out action))
		{
			if (InCombat)
			{
				return true;
			}
		}

		if (DervishPvP.CanUse(out action))
		{
			if (InCombat)
			{
				return true;
			}
		}

		return base.EmergencyAbility(nextGCD, out action);
	}

	private bool TryUseSupportPaean(out IAction? action)
	{
		action = null;

		foreach (var cleanseTarget in SelectCleansePaeanTargets())
		{
			if (TryUseWardensPaeanOn(TheWardensPaeanPvP, cleanseTarget.Target, PaeanCastIntent.Cleanse, out action))
			{
				return true;
			}
		}

		foreach (var peelTarget in SelectProtectivePaeanTargets())
		{
			if (TryUseWardensPaeanOn(TheWardensPaeanPvP, peelTarget.Target, PaeanCastIntent.Protect, out action))
			{
				return true;
			}
		}

		foreach (var engageTarget in SelectEngagePaeanTargets())
		{
			if (TryUseWardensPaeanOn(TheWardensPaeanPvP, engageTarget.Target, PaeanCastIntent.Protect, out action))
			{
				return true;
			}
		}

		return false;
	}

	private List<PaeanCandidate> SelectCleansePaeanTargets()
	{
		List<PaeanCandidate> candidates = [];

		foreach (var member in PartyMembers)
		{
			if (!IsValidPaeanTarget(member) || !member.HasStatus(false, StatusHelper.PurifyPvPStatuses))
			{
				continue;
			}

			var score = PaeanCleanseBaseWeight
				+ ScorePaeanHealth(member)
				+ CountHostilesTargeting(member) * PaeanFocusedHostileWeight
				+ ScoreSupportRole(member)
				+ ScorePaeanDistance(member);

			candidates.Add(new PaeanCandidate(member, score));
		}

		candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
		return candidates;
	}

	private List<PaeanCandidate> SelectProtectivePaeanTargets()
	{
		List<PaeanCandidate> candidates = [];

		foreach (var member in PartyMembers)
		{
			if (!IsValidPaeanTarget(member)
				|| member.HasStatus(false, StatusHelper.PurifyPvPStatuses)
				|| HasPaeanLockout(member))
			{
				continue;
			}

			var focusCount = CountHostilesTargeting(member);
			var healthRatio = member.GetHealthRatio();
			if (!BardPvPDecisionPolicy.ShouldUseProtectivePaean(
				healthRatio,
				focusCount))
			{
				continue;
			}

			var score = ScorePaeanHealth(member)
				+ focusCount * PaeanFocusedHostileWeight
				+ ScoreSupportRole(member)
				+ ScorePaeanDistance(member);

			if (score < PaeanPeelScoreThreshold)
			{
				continue;
			}

			candidates.Add(new PaeanCandidate(member, score));
		}

		candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
		return candidates;
	}

	private List<PaeanCandidate> SelectEngagePaeanTargets()
	{
		List<PaeanCandidate> candidates = [];

		foreach (var member in PartyMembers)
		{
			if (!IsValidPaeanTarget(member)
				|| member.HasStatus(false, StatusHelper.PurifyPvPStatuses)
				|| HasPaeanLockout(member)
				|| member.GetHealthRatio() < PaeanHealthyEngageThreshold
				|| CountHostilesTargeting(member) > PaeanMaxFocusedHostilesForEngage
				|| !IsEngageRole(member)
				|| !IsPushingIntoEnemies(member))
			{
				continue;
			}

			var score = ScoreEngageRole(member) + ScoreSmartTargetProximity(member);
			if (score < PaeanEngageScoreThreshold)
			{
				continue;
			}

			candidates.Add(new PaeanCandidate(member, score));
		}

		candidates.Sort((left, right) => right.Score.CompareTo(left.Score));
		return candidates;
	}

	private bool IsValidPaeanTarget(IBattleChara? member)
	{
		return member != null
			&& member.GameObjectId != 0
			&& Player != null
			&& member.GameObjectId != Player.GameObjectId
			&& member.CurrentHp > 0
			&& member.DistanceToPlayer() <= TheWardensPaeanPvP.TargetInfo.Range;
	}

	private static bool HasPaeanLockout(IBattleChara member)
	{
		return member.HasStatus(false, StatusID.TheWardensPaean_3143, StatusID.WardensGrace);
	}

	private static float ScorePaeanHealth(IBattleChara member)
	{
		var healthRatio = member.GetHealthRatio();
		if (healthRatio <= PaeanCriticalHpThreshold)
		{
			return PaeanCriticalHpWeight;
		}

		return healthRatio <= PaeanLowHpThreshold ? PaeanLowHpWeight : 0f;
	}

	private static float ScoreSupportRole(IBattleChara member)
	{
		if (member.IsJobCategory(JobRole.Healer))
		{
			return PaeanHealerSupportRoleWeight;
		}

		if (member.IsJobCategory(JobRole.RangedPhysical) || member.IsJobCategory(JobRole.RangedMagical))
		{
			return PaeanRangedSupportRoleWeight;
		}

		if (member.IsJobCategory(JobRole.Melee))
		{
			return PaeanMeleeSupportRoleWeight;
		}

		return member.IsJobCategory(JobRole.Tank) ? PaeanTankSupportRoleWeight : 0f;
	}

	private static bool IsEngageRole(IBattleChara member)
	{
		return member.IsJobCategory(JobRole.Tank) || member.IsJobCategory(JobRole.Melee);
	}

	private static float ScoreEngageRole(IBattleChara member)
	{
		if (member.IsJobCategory(JobRole.Tank))
		{
			return PaeanTankEngageWeight;
		}

		return member.IsJobCategory(JobRole.Melee) ? PaeanMeleeEngageWeight : 0f;
	}

	private float ScorePaeanDistance(IBattleChara member)
	{
		var range = TheWardensPaeanPvP.TargetInfo.Range;
		if (range <= 0f)
		{
			return 0f;
		}

		var distanceRatio = Math.Clamp(member.DistanceToPlayer() / range, 0f, 1f);
		return (1f - distanceRatio) * PaeanDistanceWeight;
	}

	private static int CountHostilesTargeting(IBattleChara ally)
	{
		var count = 0;
		foreach (var hostile in AllHostileTargets)
		{
			if (hostile != null && hostile.TargetObjectId == ally.GameObjectId)
			{
				count++;
			}
		}

		return count;
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

	private static bool IsPushingIntoEnemies(IBattleChara ally)
	{
		var smartTarget = HostileTarget;
		if (Player == null || smartTarget == null)
		{
			return false;
		}

		if (DistanceToNearestHostile(ally) > PaeanShortCombatDistance)
		{
			return false;
		}

		var allyDistanceToSmartTarget = Vector3.Distance(ally.Position, smartTarget.Position);
		var bardDistanceToSmartTarget = Vector3.Distance(Player.Position, smartTarget.Position);
		return allyDistanceToSmartTarget < bardDistanceToSmartTarget;
	}

	private static float ScoreSmartTargetProximity(IBattleChara ally)
	{
		var smartTarget = HostileTarget;
		if (smartTarget == null)
		{
			return 0f;
		}

		return Vector3.Distance(ally.Position, smartTarget.Position) <= PaeanShortCombatDistance
			? PaeanSmartTargetWeight
			: 0f;
	}

	private static float DistanceToNearestHostile(IBattleChara ally)
	{
		var nearestDistance = float.MaxValue;
		foreach (var hostile in AllHostileTargets)
		{
			if (hostile == null || hostile.CurrentHp == 0)
			{
				continue;
			}

			var distance = Vector3.Distance(ally.Position, hostile.Position) - hostile.HitboxRadius;
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
			}
		}

		return nearestDistance;
	}

	private static bool TryUseWardensPaeanOn(IBaseAction wardensPaean, IBattleChara? target, PaeanCastIntent intent, out IAction? action)
	{
		action = null;

		if (target == null || target.GameObjectId == 0 || target.CurrentHp == 0)
		{
			return false;
		}

		var targetObjectId = target.GameObjectId;
		var originalCanTarget = wardensPaean.Setting.CanTarget;

		wardensPaean.Setting.CanTarget = candidate =>
			originalCanTarget(candidate) && candidate.GameObjectId == targetObjectId;

		try
		{
			return wardensPaean.CanUse(
				out action,
				skipTargetStatusNeedCheck: intent == PaeanCastIntent.Protect,
				targetOverride: TargetType.Nearest);
		}
		finally
		{
			wardensPaean.Setting.CanTarget = originalCanTarget;
		}
	}

	protected override bool AttackAbility(IAction nextGCD, out IAction? action)
	{
		if (RepellingShotPvP.CanUse(out action))
		{
			var input = BuildShutdownInput(
				RepellingShotPvP,
				IsRepellingBackstepSafe(RepellingShotPvP.Target.Target));

			if (BardPvPDecisionPolicy.ShouldUseRepellingShot(input))
			{
				return true;
			}
		}

		if (SilentNocturnePvP.CanUse(out action))
		{
			var input = BuildShutdownInput(SilentNocturnePvP, safeBackstepExists: true);
			if (BardPvPDecisionPolicy.ShouldUseSilentNocturne(input))
			{
				return true;
			}
		}

		if (TryUseFrontlineEagleEyeShot(out action))
		{
			return true;
		}

		if (TryUseDirectSecureAction(EncoreOfLightPvP, EncoreOfLightPotency, out action, skipAoeCheck: true))
		{
			return true;
		}

		if (EncoreOfLightPvP.CanUse(out action, skipAoeCheck: true)
			&& ShouldUseBurstOrBeforeStatusExpires(
				EncoreOfLightPvP,
				StatusID.EncoreOfLightReady,
				EncoreOfLightPotency))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out action);
	}

	private bool TryUseFrontlineEagleEyeShot(out IAction? action)
	{
		action = null;
		if (!EagleEyeShotPvP.CanUse(out var candidateAction))
		{
			return false;
		}

		var target = EagleEyeShotPvP.Target.Target;
		if (target == null || target.CurrentHp == 0)
		{
			return false;
		}

		var input = new FrontlineEagleEyeShotInput(
			Job: FrontlinePvPRangedJob.Bard,
			IsInFrontline: DataCenter.IsInFrontline,
			IsInCrystallineConflict: DataCenter.IsInCrystallineConflict,
			Target: CreateEagleEyeShotTargetState(target));

		if (!FrontlinePvPRoleActionPolicy.ShouldUseEagleEyeShot(input))
		{
			return false;
		}

		action = candidateAction;
		return true;
	}

	private static FrontlineEagleEyeShotTargetState CreateEagleEyeShotTargetState(IBattleChara target)
	{
		return new FrontlineEagleEyeShotTargetState(
			HealthRatio: target.GetHealthRatio(),
			CurrentMp: target.CurrentMp,
			HasGuard: target.HasStatus(false, StatusID.Guard),
			HasResilience: target.HasStatus(false, StatusID.Resilience),
			HasNonGuardInvulnerability: HasNonGuardInvulnerability(target),
			HasAllyFocus: CountAlliesTargeting(target) > 0,
			IsObjectiveRelevant: IsObjectiveRelevantTarget(target),
			IsControlled: IsControlledForEagleEyeShot(target),
			IsBurstWorthy: IsBurstWorthy(target),
			TargetCommitted: target.TargetObjectId != 0,
			ImmediateFollowUpAvailable: false,
			HasWildfire: false,
			ExpectedDamageRatio: ExpectedEagleEyeShotDamageRatio(target));
	}

	private static bool IsControlledForEagleEyeShot(IBattleChara target)
	{
		return target.HasStatus(
			false,
			StatusID.Silenced,
			StatusID.Bind,
			StatusID.Bind_1345,
			StatusID.Stun,
			StatusID.Stun_1343,
			StatusID.DeepFreeze_3219,
			StatusID.MiracleOfNature);
	}

	private static bool HasNonGuardInvulnerability(IBattleChara target)
	{
		var statusList = target.StatusList;
		if (statusList == null)
		{
			return false;
		}

		var database = PvPMitigationDatabaseProvider.Current;
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

	private static double ExpectedEagleEyeShotDamageRatio(IBattleChara target)
	{
		return target.MaxHp == 0 ? 0.0 : EagleEyeShotPotency / target.MaxHp;
	}

	private static BardPvPShutdownInput BuildShutdownInput(IBaseAction action, bool safeBackstepExists)
	{
		var target = action.Target.Target;
		return new BardPvPShutdownInput(
			TargetHasResilience: target.HasStatus(false, StatusID.Resilience),
			TargetIsCasting: target.IsCasting && target.IsCastInterruptible,
			TargetThreatensFragileAlly: TargetThreatensProtectedAlly(target),
			TargetIsBurstWorthy: IsBurstWorthy(target),
			TargetHasLowMp: target.CurrentMp <= PvPScoringFactors.MediumMp,
			TargetHealthRatio: target.GetHealthRatio(),
			TargetDistance: target.DistanceToPlayer(),
			SafeBackstepExists: safeBackstepExists,
			ObjectiveControlNeeded: IsObjectiveRelevantTarget(target));
	}

	private static bool TargetThreatensProtectedAlly(IBattleChara target)
	{
		if (target.TargetObjectId == 0)
		{
			return false;
		}

		return ThreatenedAllyState.BuildThreatenedAllyIds().Contains(target.TargetObjectId);
	}

	private static bool IsBurstWorthy(IBattleChara target)
	{
		if (target.MaxHp <= 0 || !target.IsEnemy())
		{
			return false;
		}

		var database = PvPMitigationDatabaseProvider.Current;
		var effectiveHp = EffectiveHpCalculator.Compute(target, database);
		var effectiveHpRatio = double.IsPositiveInfinity(effectiveHp)
			? double.PositiveInfinity
			: effectiveHp / target.MaxHp;

		var score = PvPTargetScorer.Explain(target, PvPScoringContextBuilder.BuildCurrent(GetContextHostiles(target)));
		var input = new PvPBurstDecisionInput(
			Intent: PvPBurstIntent.Burst,
			EffectiveHpRatio: effectiveHpRatio,
			ActiveDamageReduction: MitigationPenalty.Compute(target, database),
			Score: score);

		return PvPBurstDecision.Evaluate(input) != PvPBurstRecommendation.Hold;
	}

	private static IReadOnlyList<IBattleChara> GetContextHostiles(IBattleChara target)
	{
		return DataCenter.AllHostileTargets.Count > 0 ? DataCenter.AllHostileTargets : [target];
	}

	private static bool IsObjectiveRelevantTarget(IBattleChara target)
	{
		return PvPObjectiveState.BuildObjectiveRelevantTargetIds().Contains(target.GameObjectId);
	}

	private static bool IsRepellingBackstepSafe(IBattleChara target)
	{
		if (Player == null)
		{
			return false;
		}

		var awayFromTarget = Player.Position - target.Position;
		if (awayFromTarget.LengthSquared() <= float.Epsilon)
		{
			return false;
		}

		var destination = Player.Position + Vector3.Normalize(awayFromTarget) * RepellingBackstepYalms;
		return DataCenter.IsMovementDestinationSafe(destination)
			&& DataCenter.IsFixedDashSafe(Player.Position, destination);
	}
	#endregion

	#region GCDs
	protected override bool GeneralGCD(out IAction? action)
	{
		if (TryUseDirectSecureGcd(out action))
		{
			return true;
		}

		if (HarmonicArrowPvP.CanUse(out action)
			&& ShouldUseBurstOrPreventChargeOvercap(HarmonicArrowPvP, HarmonicArrowPotency))
		{
			return true;
		}

		if (PitchPerfectPvP.CanUse(out action, skipAoeCheck: true))
		{
			return true;
		}

		if (BlastArrowPvP.CanUse(out action)
			&& ShouldUseBurstOrBeforeStatusExpires(BlastArrowPvP, StatusID.BlastArrowReady_3142, BlastArrowPotency))
		{
			return true;
		}

		if (BardPvPDecisionPolicy.ShouldUseApexArrow(
				StatusHelper.PlayerHasStatus(true, StatusID.BlastArrowReady_3142))
			&& ApexArrowPvP.CanUse(out action, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (PowerfulShotPvP.CanUse(out action))
		{
			return true;
		}

		return base.GeneralGCD(out action);
	}

	private static bool TryUseDirectSecureGcd(out IAction? action)
	{
		if (TryUseDirectSecureAction(HarmonicArrowPvP, HarmonicArrowPotency, out action))
		{
			return true;
		}

		if (TryUseDirectSecureAction(PitchPerfectPvP, PitchPerfectPotency, out action, skipAoeCheck: true))
		{
			return true;
		}

		if (TryUseDirectSecureAction(BlastArrowPvP, BlastArrowPotency, out action))
		{
			return true;
		}

		if (BardPvPDecisionPolicy.ShouldUseApexArrow(StatusHelper.PlayerHasStatus(true, StatusID.BlastArrowReady_3142))
			&& TryUseDirectSecureAction(ApexArrowPvP, ApexArrowPotency, out action, skipStatusProvideCheck: true))
		{
			return true;
		}

		return TryUseDirectSecureAction(PowerfulShotPvP, PowerfulShotPotency, out action);
	}

	private static bool ShouldUseBurstOrPreventChargeOvercap(IBaseAction action, double expectedPotency)
	{
		return BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
			targetIsBurstWorthy: PvPBurstGate.ShouldUse(action),
			targetBlocksDamage: PvPBurstGate.TargetBlocksDamage(action),
			forcedSpendWindow: action.Cooldown.WillHaveXChargesGCD(action.Cooldown.MaxCharges, BurstExpiryGcdWindow, BurstExpiryOffset),
			targetCanBeKilled: PvPBurstGate.CanSecure(action, expectedPotency));
	}

	private static bool ShouldUseBurstOrBeforeStatusExpires(IBaseAction action, StatusID status, double expectedPotency)
	{
		return BardPvPDecisionPolicy.ShouldUseBurstOrForcedSpend(
			targetIsBurstWorthy: PvPBurstGate.ShouldUse(action),
			targetBlocksDamage: PvPBurstGate.TargetBlocksDamage(action),
			forcedSpendWindow: StatusHelper.PlayerHasStatus(true, status)
				&& StatusHelper.PlayerWillStatusEndGCD(BurstExpiryGcdWindow, BurstExpiryOffset, true, status),
			targetCanBeKilled: PvPBurstGate.CanSecure(action, expectedPotency));
	}

	private static bool TryUseDirectSecureAction(
		IBaseAction baseAction,
		double expectedPotency,
		out IAction? action,
		bool skipAoeCheck = false,
		bool skipStatusProvideCheck = false)
	{
		action = null;

		foreach (var targetId in RankDirectSecureTargets(baseAction, expectedPotency))
		{
			if (TryUseActionOn(baseAction, targetId, out action, skipAoeCheck, skipStatusProvideCheck))
			{
				return true;
			}
		}

		return false;
	}

	private static List<ulong> RankDirectSecureTargets(IBaseAction baseAction, double expectedPotency)
	{
		List<BardPvPKillSecureSnapshot> snapshots = [];
		var range = baseAction.TargetInfo.Range;
		var database = PvPMitigationDatabaseProvider.Current;

		foreach (var hostile in AllHostileTargets)
		{
			if (hostile == null
				|| hostile.CurrentHp == 0
				|| hostile.MaxHp == 0
				|| hostile.DistanceToPlayer() > range)
			{
				continue;
			}

			var effectiveHp = EffectiveHpCalculator.Compute(hostile, database);
			var effectiveHpRatio = double.IsPositiveInfinity(effectiveHp)
				? double.PositiveInfinity
				: effectiveHp / hostile.MaxHp;

			snapshots.Add(new BardPvPKillSecureSnapshot(
				TargetId: hostile.GameObjectId,
				HealthRatio: hostile.GetHealthRatio(),
				EffectiveHealthRatio: effectiveHpRatio,
				ExpectedDamageRatio: expectedPotency / hostile.MaxHp,
				ActiveDamageReduction: MitigationPenalty.Compute(hostile, database),
				HasInvulnerability: double.IsPositiveInfinity(effectiveHp),
				HasAllyFocus: CountAlliesTargeting(hostile) > 0,
				IsObjectiveRelevant: IsObjectiveRelevantTarget(hostile)));
		}

		return BardPvPDecisionPolicy.RankDirectSecureTargets(snapshots);
	}

	private static bool TryUseActionOn(
		IBaseAction baseAction,
		ulong targetId,
		out IAction? action,
		bool skipAoeCheck,
		bool skipStatusProvideCheck)
	{
		action = null;
		var originalCanTarget = baseAction.Setting.CanTarget;
		baseAction.Setting.CanTarget = candidate =>
			originalCanTarget(candidate) && candidate.GameObjectId == targetId;

		try
		{
			return baseAction.CanUse(
				out action,
				skipAoeCheck: skipAoeCheck,
				skipStatusProvideCheck: skipStatusProvideCheck,
				targetOverride: TargetType.Nearest);
		}
		finally
		{
			baseAction.Setting.CanTarget = originalCanTarget;
		}
	}
	#endregion
}
