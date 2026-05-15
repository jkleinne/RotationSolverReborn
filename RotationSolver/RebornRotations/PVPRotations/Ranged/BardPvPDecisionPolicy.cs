namespace RotationSolver.RebornRotations.PVPRotations.Ranged;

internal readonly record struct BardPvPShutdownInput(
    bool TargetHasResilience,
    bool TargetIsCasting,
    bool TargetThreatensFragileAlly,
    bool TargetIsBurstWorthy,
    bool TargetHasLowMp,
    float TargetHealthRatio,
    float TargetDistance,
    bool SafeBackstepExists,
    bool ObjectiveControlNeeded);

internal static class BardPvPDecisionPolicy
{
    private const float KillPressureHealthRatio = 0.55f;
    private const float PaeanLowHealthRatio = 0.55f;
    private const float PaeanFocusedHealthRatio = 0.65f;
    private const float RepellingRangeYalms = 10f;

    internal static bool ShouldUseSilentNocturne(BardPvPShutdownInput input)
    {
        if (input.TargetHasResilience)
        {
            return false;
        }

        return input.TargetIsCasting
            || input.TargetThreatensFragileAlly
            || input.TargetIsBurstWorthy
            || input.TargetHasLowMp
            || input.TargetHealthRatio <= KillPressureHealthRatio;
    }

    internal static bool ShouldUseRepellingShot(BardPvPShutdownInput input)
    {
        if (input.TargetHasResilience || input.TargetDistance > RepellingRangeYalms)
        {
            return false;
        }

        if (!input.SafeBackstepExists)
        {
            return false;
        }

        return input.TargetThreatensFragileAlly
            || input.ObjectiveControlNeeded
            || input.TargetIsBurstWorthy
            || input.TargetHealthRatio <= KillPressureHealthRatio;
    }

    internal static bool ShouldUseBurstOrForcedSpend(bool targetIsBurstWorthy, bool targetBlocksDamage, bool forcedSpendWindow)
    {
        if (targetBlocksDamage)
        {
            return false;
        }

        if (targetIsBurstWorthy)
        {
            return true;
        }

        return forcedSpendWindow;
    }

    internal static bool ShouldUseProtectivePaean(float healthRatio, int focusCount)
    {
        if (focusCount > 0)
        {
            return healthRatio <= PaeanFocusedHealthRatio;
        }

        return healthRatio <= PaeanLowHealthRatio;
    }
}
