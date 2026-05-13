using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using RotationSolver.Basic.Actions.PvPTargetSelection;

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
	private const float PaeanPeelScoreThreshold = 2f;
	private const float PaeanEngageScoreThreshold = 3f;
	private const float PaeanCleanseBaseWeight = 100f;
	private const float PaeanCriticalHpWeight = 4f;
	private const float PaeanLowHpWeight = 2f;
	private const float PaeanFocusedHostileWeight = 2f;
	private const float PaeanPressureRoleWeight = 1.5f;
	private const float PaeanMeleePressureRoleWeight = 1f;
	private const float PaeanTankPressureRoleWeight = 0.5f;
	private const float PaeanDistanceWeight = 1f;
	private const float PaeanTankEngageWeight = 2.5f;
	private const float PaeanMeleeEngageWeight = 2f;
	private const float PaeanSmartTargetWeight = 1.5f;

	private readonly record struct PaeanCandidate(IBattleChara Target, float Score);

	#region Configurations

	[RotationConfig(CombatType.PvP, Name = "Use Warden's Paean on other players")]
	public bool BRDEsuna2 { get; set; } = false;
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
			if (TryUseWardensPaeanOn(TheWardensPaeanPvP, cleanseTarget.Target, isProtectivePaean: false, out action))
			{
				return true;
			}
		}

		foreach (var peelTarget in SelectProtectivePaeanTargets())
		{
			if (TryUseWardensPaeanOn(TheWardensPaeanPvP, peelTarget.Target, isProtectivePaean: true, out action))
			{
				return true;
			}
		}

		foreach (var engageTarget in SelectEngagePaeanTargets())
		{
			if (TryUseWardensPaeanOn(TheWardensPaeanPvP, engageTarget.Target, isProtectivePaean: true, out action))
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
				+ ScoreCleanseRole(member)
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
			if (focusCount == 0 && healthRatio > PaeanLowHpThreshold)
			{
				continue;
			}

			var score = ScorePaeanHealth(member)
				+ focusCount * PaeanFocusedHostileWeight
				+ ScoreProtectiveRole(member, focusCount)
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

	private static float ScoreCleanseRole(IBattleChara member)
	{
		if (member.IsJobCategory(JobRole.Healer) || member.IsJobCategory(JobRole.RangedPhysical) || member.IsJobCategory(JobRole.RangedMagical))
		{
			return PaeanPressureRoleWeight;
		}

		if (member.IsJobCategory(JobRole.Melee))
		{
			return PaeanMeleePressureRoleWeight;
		}

		return member.IsJobCategory(JobRole.Tank) ? PaeanTankPressureRoleWeight : 0f;
	}

	private static float ScoreProtectiveRole(IBattleChara member, int focusCount)
	{
		if (focusCount == 0)
		{
			return 0f;
		}

		if (member.IsJobCategory(JobRole.Healer) || member.IsJobCategory(JobRole.RangedPhysical) || member.IsJobCategory(JobRole.RangedMagical))
		{
			return PaeanPressureRoleWeight;
		}

		if (member.IsJobCategory(JobRole.Melee))
		{
			return PaeanMeleePressureRoleWeight;
		}

		return member.IsJobCategory(JobRole.Tank) ? PaeanTankPressureRoleWeight : 0f;
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

	private static bool TryUseWardensPaeanOn(IBaseAction wardensPaean, IBattleChara? target, bool isProtectivePaean, out IAction? action)
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
				skipTargetStatusNeedCheck: isProtectivePaean,
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
			if (!StatusHelper.PlayerHasStatus(true, StatusID.Repertoire))
			{
				return true;
			}
		}

		if (SilentNocturnePvP.CanUse(out action))
		{
			if (!StatusHelper.PlayerHasStatus(true, StatusID.Repertoire))
			{
				return true;
			}
		}

		if (EagleEyeShotPvP.CanUse(out action) && PvPBurstGate.ShouldUse(EagleEyeShotPvP))
		{
			return true;
		}

		if (EncoreOfLightPvP.CanUse(out action, skipAoeCheck: true)
			&& ShouldUseBurstOrBeforeStatusExpires(EncoreOfLightPvP, StatusID.EncoreOfLightReady))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out action);
	}
	#endregion

	#region GCDs
	protected override bool GeneralGCD(out IAction? action)
	{
		if (HarmonicArrowPvP.CanUse(out action)
			&& ShouldUseBurstOrPreventChargeOvercap(HarmonicArrowPvP))
		{
			return true;
		}

		if (PitchPerfectPvP.CanUse(out action, skipAoeCheck: true))
		{
			return true;
		}

		if (BlastArrowPvP.CanUse(out action)
			&& ShouldUseBurstOrBeforeStatusExpires(BlastArrowPvP, StatusID.BlastArrowReady_3142))
		{
			return true;
		}

		if (!StatusHelper.PlayerHasStatus(true, StatusID.BlastArrowReady_3142)
			&& ApexArrowPvP.CanUse(out action))
		{
			return true;
		}

		if (PowerfulShotPvP.CanUse(out action))
		{
			return true;
		}

		return base.GeneralGCD(out action);
	}

	private static bool ShouldUseBurstOrPreventChargeOvercap(IBaseAction action)
	{
		return PvPBurstGate.ShouldUse(action)
			|| action.Cooldown.WillHaveXChargesGCD(action.Cooldown.MaxCharges, BurstExpiryGcdWindow, BurstExpiryOffset);
	}

	private static bool ShouldUseBurstOrBeforeStatusExpires(IBaseAction action, StatusID status)
	{
		return PvPBurstGate.ShouldUse(action)
			|| (StatusHelper.PlayerHasStatus(true, status)
				&& StatusHelper.PlayerWillStatusEndGCD(BurstExpiryGcdWindow, BurstExpiryOffset, true, status));
	}
	#endregion
}
