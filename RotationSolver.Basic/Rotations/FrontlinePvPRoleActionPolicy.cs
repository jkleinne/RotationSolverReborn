using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.Basic.Rotations;

internal enum FrontlinePvPRangedJob
{
	Other,
	Bard,
	Machinist,
}

internal readonly record struct FrontlineEagleEyeShotInput(
	FrontlinePvPRangedJob Job,
	bool IsInFrontline,
	bool IsInCrystallineConflict,
	FrontlineEagleEyeShotTargetState Target);

internal readonly record struct FrontlineEagleEyeShotTargetState(
	float HealthRatio,
	uint CurrentMp,
	bool HasGuard,
	bool HasResilience,
	bool HasNonGuardInvulnerability,
	bool HasAllyFocus,
	bool IsObjectiveRelevant,
	bool IsControlled,
	bool IsBurstWorthy,
	bool TargetCommitted,
	bool ImmediateFollowUpAvailable,
	bool HasWildfire,
	double ExpectedDamageRatio);

internal static class FrontlinePvPRoleActionPolicy
{
	private const string BorderlandRuinsFrontlineName = "The Borderland Ruins (Secure)";
	private const string SealRockFrontlineName = "Seal Rock (Seize)";
	private const string FieldsOfGloryFrontlineName = "The Fields of Glory (Shatter)";
	private const string OnsalHakairFrontlineName = "Onsal Hakair (Danshig Naadam)";
	private const string WorqorChirtehFrontlineName = "Worqor Chirteh (Triumph)";
	private const float EagleEyeShotSecureHealthRatio = 0.30f;
	private const float BardControlledPressureHealthRatio = 0.55f;
	private const float BardBurstPressureHealthRatio = 0.55f;
	private const float BardGuardPressureHealthRatio = 0.45f;
	private const float MachinistInjuredPressureHealthRatio = 0.65f;
	private const float MachinistBurstSetupHealthRatio = 0.80f;
	private const float MachinistGuardPressureHealthRatio = 0.65f;

	internal static readonly string[] FrontlineContentFinderNames =
	[
		BorderlandRuinsFrontlineName,
		SealRockFrontlineName,
		FieldsOfGloryFrontlineName,
		OnsalHakairFrontlineName,
		WorqorChirtehFrontlineName,
	];

	internal static bool IsFrontlineContentFinderName(string? contentFinderName)
	{
		if (string.IsNullOrEmpty(contentFinderName))
		{
			return false;
		}

		for (var i = 0; i < FrontlineContentFinderNames.Length; i++)
		{
			if (string.Equals(contentFinderName, FrontlineContentFinderNames[i], StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	internal static bool ShouldTryFrontlineRoleAction(bool isInFrontline, bool isInCrystallineConflict)
	{
		return isInFrontline && !isInCrystallineConflict;
	}

	internal static bool ShouldUseInCurrentPass(bool isRealGcd, bool requireGcdAction)
	{
		return isRealGcd == requireGcdAction;
	}

	internal static bool ShouldDeferEagleEyeShotToJobPolicy(FrontlinePvPRangedJob job)
	{
		return job is FrontlinePvPRangedJob.Bard or FrontlinePvPRangedJob.Machinist;
	}

	internal static bool ShouldUseEagleEyeShot(FrontlineEagleEyeShotInput input)
	{
		if (!ShouldTryFrontlineRoleAction(input.IsInFrontline, input.IsInCrystallineConflict))
		{
			return false;
		}

		if (input.Target.HasNonGuardInvulnerability)
		{
			return false;
		}

		return input.Job switch
		{
			FrontlinePvPRangedJob.Bard => ShouldUseBardEagleEyeShot(input.Target),
			FrontlinePvPRangedJob.Machinist => ShouldUseMachinistEagleEyeShot(input.Target),
			_ => true,
		};
	}

	private static bool ShouldUseBardEagleEyeShot(FrontlineEagleEyeShotTargetState target)
	{
		if (CanSecureWithEagleEyeShot(target))
		{
			return true;
		}

		if (target.IsControlled && !target.HasResilience)
		{
			return target.HealthRatio <= BardControlledPressureHealthRatio
				|| HasResourcePressure(target)
				|| HasTeamPressure(target)
				|| target.IsBurstWorthy;
		}

		if (target.HasGuard)
		{
			return target.HealthRatio <= BardGuardPressureHealthRatio && HasTeamPressure(target);
		}

		return target.IsBurstWorthy
			&& (target.HealthRatio <= BardBurstPressureHealthRatio
				|| HasResourcePressure(target)
				|| HasTeamPressure(target));
	}

	private static bool ShouldUseMachinistEagleEyeShot(FrontlineEagleEyeShotTargetState target)
	{
		if (CanSecureWithEagleEyeShot(target))
		{
			return true;
		}

		if (target.HasWildfire)
		{
			return true;
		}

		if (target.HealthRatio <= MachinistInjuredPressureHealthRatio)
		{
			return true;
		}

		if (!HasMachinistBurstSetupSignal(target))
		{
			return false;
		}

		if (target.HasGuard)
		{
			return target.HealthRatio <= MachinistGuardPressureHealthRatio;
		}

		return target.HealthRatio <= MachinistBurstSetupHealthRatio
			|| HasResourcePressure(target);
	}

	private static bool CanSecureWithEagleEyeShot(FrontlineEagleEyeShotTargetState target)
	{
		return target.HealthRatio <= EagleEyeShotSecureHealthRatio
			|| (target.ExpectedDamageRatio > 0.0 && target.HealthRatio <= target.ExpectedDamageRatio);
	}

	private static bool HasMachinistBurstSetupSignal(FrontlineEagleEyeShotTargetState target)
	{
		return target.TargetCommitted
			|| target.ImmediateFollowUpAvailable
			|| target.IsBurstWorthy
			|| HasResourcePressure(target)
			|| HasTeamPressure(target);
	}

	private static bool HasResourcePressure(FrontlineEagleEyeShotTargetState target)
	{
		return target.CurrentMp <= PvPScoringFactors.MediumMp;
	}

	private static bool HasTeamPressure(FrontlineEagleEyeShotTargetState target)
	{
		return target.HasAllyFocus || target.IsObjectiveRelevant;
	}
}
