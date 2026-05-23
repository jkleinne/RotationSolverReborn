namespace RotationSolver.Tests;

internal static partial class PvETestSuite
{
    internal static IReadOnlyList<TestCase> Tests { get; } =
    [
        new("bard ascended dot thresholds honor target time", BardAscendedDotThresholdsHonorTargetTime),
        new("bard ascended dot thresholds use boss fallback only when ttk is unknown", BardAscendedDotThresholdsUseBossFallbackOnlyWhenTtkIsUnknown),
        new("bard ascended song presets map to expected durations", BardAscendedSongPresetsMapToExpectedDurations),
        new("bard ascended apex spends during burst and mage ballad windows", BardAscendedApexSpendsDuringBurstAndMageBalladWindows),
        new("bard ascended apex holds during army paeon", BardAscendedApexHoldsDuringArmyPaeon),
        new("bard ascended apex uses planned kill time over song fallback", BardAscendedApexUsesPlannedKillTimeOverSongFallback),
        new("bard ascended blast arrow waits for urgent gcds", BardAscendedBlastArrowWaitsForUrgentGcds),
        new("bard ascended aoe thresholds distinguish gcd and ogcd", BardAscendedAoeThresholdsDistinguishGcdAndOgcd),
        new("bard ascended potion presets map to expected timings", BardAscendedPotionPresetsMapToExpectedTimings),
        new("bard ascended custom potion timings reject empty input", BardAscendedCustomPotionTimingsRejectEmptyInput),
    ];

    static void AssertTrue(bool actual, string message)
    {
        if (!actual)
        {
            throw new InvalidOperationException(message);
        }
    }

    static void AssertFalse(bool actual, string message)
    {
        if (actual)
        {
            throw new InvalidOperationException(message);
        }
    }

    static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}. Expected {expected}, got {actual}");
        }
    }

    static void AssertSequenceEqual(float[] expected, float[] actual, string message)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException($"{message}. Expected [{string.Join(", ", expected)}], got [{string.Join(", ", actual)}]");
        }
    }
}
