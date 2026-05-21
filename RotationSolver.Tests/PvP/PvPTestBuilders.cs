using RotationSolver.Basic.Actions.PvPTargetSelection;
using RotationSolver.RebornRotations.PVPRotations.Ranged;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	static MachinistPvPTargetSnapshot MachinistTarget(
		ulong targetId,
		float healthRatio,
		uint currentMp,
		bool hasGuard = false,
		bool hasResilience = false,
		bool isObjectiveRelevant = false,
		bool hasAllyFocus = false,
		int allyFocusCount = 0,
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
			AllyFocusCount: allyFocusCount,
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
		int allyFocusCount = 0,
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
			AllyFocusCount: allyFocusCount,
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
}
