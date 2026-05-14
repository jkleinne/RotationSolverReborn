using ECommons.GameFunctions;
using RotationSolver.Basic.Actions.PvPTargetSelection.Factors;

namespace RotationSolver.Basic.Actions.PvPTargetSelection;

/// <summary>
/// Boundary helper that adapts live rotation state into the pure
/// <see cref="PvPBurstDecision"/> policy.
/// </summary>
public static class PvPBurstGate
{
    /// <summary>
    /// Return <see langword="true"/> when the configured PvP burst policy allows
    /// spending <paramref name="action"/> on its resolved action target.
    /// </summary>
    public static bool ShouldUse(IBaseAction action, PvPBurstIntent intent = PvPBurstIntent.Burst)
    {
        if (!DataCenter.IsPvP || !Service.Config.PvpBurstConservation)
        {
            return true;
        }

        return ShouldUseTarget(ResolveBurstTarget(action), intent);
    }

    private static bool ShouldUseTarget(IBattleChara? target, PvPBurstIntent intent)
    {
        if (!IsUsableHostile(target))
        {
            return false;
        }

        var database = PvPMitigationDatabaseProvider.Current;
        var effectiveHp = EffectiveHpCalculator.Compute(target, database);
        var effectiveHpRatio = double.IsPositiveInfinity(effectiveHp)
            ? double.PositiveInfinity
            : effectiveHp / target.MaxHp;

        var context = PvPScoringContextBuilder.BuildCurrent(GetContextHostiles(target));
        var score = PvPTargetScorer.Explain(target, context);
        var input = new PvPBurstDecisionInput(
            Intent: intent,
            EffectiveHpRatio: effectiveHpRatio,
            ActiveDamageReduction: MitigationPenalty.Compute(target, database),
            Score: score);

        return PvPBurstDecision.Evaluate(input) != PvPBurstRecommendation.Hold;
    }

    private static IBattleChara? ResolveBurstTarget(IBaseAction action)
    {
        if (IsUsableHostile(action.Target.Target))
        {
            return action.Target.Target;
        }

        return FindBestAffectedHostile(action.Target.AffectedTargets);
    }

    private static IBattleChara? FindBestAffectedHostile(IReadOnlyList<IBattleChara> affectedTargets)
    {
        if (affectedTargets.Count == 0)
        {
            return null;
        }

        List<IBattleChara> hostiles = [];
        foreach (var target in affectedTargets)
        {
            if (IsUsableHostile(target))
            {
                hostiles.Add(target);
            }
        }

        if (hostiles.Count == 0)
        {
            return null;
        }

        var context = PvPScoringContextBuilder.BuildCurrent(GetContextHostiles(hostiles));
        IBattleChara? best = null;
        var bestScore = double.NegativeInfinity;
        foreach (var target in hostiles)
        {
            var score = PvPTargetScorer.Score(target, context);
            if (score > bestScore)
            {
                best = target;
                bestScore = score;
            }
        }

        return best;
    }

    private static IReadOnlyList<IBattleChara> GetContextHostiles(IBattleChara target)
    {
        if (DataCenter.AllHostileTargets.Count > 0)
        {
            return DataCenter.AllHostileTargets;
        }

        return [target];
    }

    private static IReadOnlyList<IBattleChara> GetContextHostiles(IReadOnlyList<IBattleChara> affectedTargets)
    {
        if (DataCenter.AllHostileTargets.Count > 0)
        {
            return DataCenter.AllHostileTargets;
        }

        return affectedTargets;
    }

    private static bool IsUsableHostile(IBattleChara? target)
    {
        return target != null && target.MaxHp > 0 && target.IsEnemy();
    }

}
