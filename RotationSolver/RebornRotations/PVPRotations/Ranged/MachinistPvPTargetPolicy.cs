using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

internal enum MachinistPvPActionIntent
{
	AnalysisDrill,
	AnalysisAirAnchor,
	AnalysisChainSaw,
	AnalysisBioblaster,
	Wildfire,
	Scattergun,
	Bishop,
	MarksmanSpite,
	FullMetalField,
	BlazingShot,
}

internal readonly record struct MachinistPvPTargetSnapshot(
	ulong TargetId,
	float HealthRatio,
	uint CurrentMp,
	bool HasGuard,
	bool HasResilience,
	bool IsObjectiveRelevant,
	bool HasAllyFocus,
	bool IsVulnerable,
	bool IsExposed,
	bool IsInNormalRange,
	bool IsInCloseRange);

internal static class MachinistPvPTargetPolicy
{
	private const double LowMpScore = 3.0;
	private const double MediumMpScore = 1.5;
	private const double ObjectiveScore = 1.5;
	private const double AllyFocusScore = 1.25;
	private const double VulnerableScore = 1.5;
	private const double ExposedScore = 1.0;
	private const double NormalRangeScore = 0.5;
	private const double CloseRangeScore = 0.5;
	private const double GuardPenalty = 4.0;
	private const double ResiliencePenalty = 2.5;
	private const double DrillGuardPunishScore = 4.0;
	private const float GuardDrillPunishHealthRatio = 0.35f;

	internal static MachinistPvPTargetSnapshot? SelectBest(
		IReadOnlyList<MachinistPvPTargetSnapshot> targets,
		MachinistPvPActionIntent intent)
	{
		var rankedTargets = Rank(targets, intent);
		return rankedTargets.Count == 0 ? null : rankedTargets[0];
	}

	internal static List<MachinistPvPTargetSnapshot> Rank(
		IReadOnlyList<MachinistPvPTargetSnapshot> targets,
		MachinistPvPActionIntent intent)
	{
		List<(MachinistPvPTargetSnapshot Target, double Score)> scoredTargets = [];

		foreach (var target in targets)
		{
			var score = Score(target, intent);
			if (double.IsNegativeInfinity(score))
			{
				continue;
			}

			scoredTargets.Add((target, score));
		}

		scoredTargets.Sort((left, right) => right.Score.CompareTo(left.Score));

		List<MachinistPvPTargetSnapshot> rankedTargets = [];
		foreach (var scoredTarget in scoredTargets)
		{
			rankedTargets.Add(scoredTarget.Target);
		}

		return rankedTargets;
	}

	internal static double Score(MachinistPvPTargetSnapshot target, MachinistPvPActionIntent intent)
	{
		if (!target.IsInNormalRange)
		{
			return double.NegativeInfinity;
		}

		var score = HealthPressure(target.HealthRatio);
		score += MpPressure(target.CurrentMp);

		if (target.IsObjectiveRelevant)
		{
			score += ObjectiveScore;
		}

		if (target.HasAllyFocus)
		{
			score += AllyFocusScore;
		}

		if (target.IsVulnerable)
		{
			score += VulnerableScore;
		}

		if (target.IsExposed)
		{
			score += ExposedScore;
		}

		if (target.IsInNormalRange)
		{
			score += NormalRangeScore;
		}

		if (target.IsInCloseRange)
		{
			score += CloseRangeScore;
		}

		score -= GuardCost(target, intent);
		score -= ResilienceCost(target, intent);

		return score;
	}

	private static double HealthPressure(float healthRatio)
	{
		return (1.0 - Math.Clamp(healthRatio, 0f, 1f)) * 4.0;
	}

	private static double MpPressure(uint currentMp)
	{
		if (currentMp <= PvPScoringFactors.LowMp)
		{
			return LowMpScore;
		}

		return currentMp <= PvPScoringFactors.MediumMp ? MediumMpScore : 0.0;
	}

	private static double GuardCost(MachinistPvPTargetSnapshot target, MachinistPvPActionIntent intent)
	{
		if (!target.HasGuard)
		{
			return 0.0;
		}

		if (intent == MachinistPvPActionIntent.AnalysisDrill
			&& target.HealthRatio <= GuardDrillPunishHealthRatio)
		{
			return -DrillGuardPunishScore;
		}

		return GuardPenalty;
	}

	private static double ResilienceCost(MachinistPvPTargetSnapshot target, MachinistPvPActionIntent intent)
	{
		if (!target.HasResilience)
		{
			return 0.0;
		}

		return intent is MachinistPvPActionIntent.AnalysisAirAnchor or MachinistPvPActionIntent.Scattergun
			? ResiliencePenalty
			: 0.0;
	}
}
