using RotationSolver.Basic.Data;
using RotationSolver.Basic.Rotations;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	private static readonly string[] ExpectedFrontlineContentFinderNames =
	[
		"The Borderland Ruins (Secure)",
		"Seal Rock (Seize)",
		"The Fields of Glory (Shatter)",
		"Onsal Hakair (Danshig Naadam)",
		"Worqor Chirteh (Triumph)",
	];

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

	static void PvPModeClassifierDetectsCrystallineConflict()
	{
		AssertTrue(
			PvPModeClassifier.IsCrystallineConflict("Crystalline Conflict"),
			"English Crystalline Conflict content finder name should be detected");
	}

	static void PvPModeClassifierDetectsFrontlineDuties()
	{
		AssertEqual(
			ExpectedFrontlineContentFinderNames.Length,
			PvPModeClassifier.FrontlineContentFinderNames.Count,
			"Frontline classifier should expose each verified Frontline duty name");

		foreach (var contentFinderName in ExpectedFrontlineContentFinderNames)
		{
			AssertTrue(
				PvPModeClassifier.FrontlineContentFinderNames.Contains(contentFinderName),
				$"{contentFinderName} should be present in the classifier set");
			AssertTrue(
				PvPModeClassifier.IsFrontline(contentFinderName),
				$"{contentFinderName} should be detected as Frontline");
		}
	}

	static void PvPModeClassifierRejectsCrystallineConflictAsFrontline()
	{
		AssertFalse(
			PvPModeClassifier.IsFrontline("Crystalline Conflict"),
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
}
