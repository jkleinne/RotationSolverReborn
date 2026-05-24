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
        new("bard ascended apex cap fallback respects burst availability", BardAscendedApexCapFallbackRespectsBurstAvailability),
        new("bard ascended apex uses planned kill time over song fallback", BardAscendedApexUsesPlannedKillTimeOverSongFallback),
        new("bard ascended blast arrow waits for urgent gcds", BardAscendedBlastArrowWaitsForUrgentGcds),
        new("bard ascended filler waits for enhanced filler or resonant ready", BardAscendedFillerWaitsForEnhancedFillerOrResonantReady),
        new("bard ascended aoe thresholds distinguish gcd and ogcd", BardAscendedAoeThresholdsDistinguishGcdAndOgcd),
        new("bard ascended first cycle starts on combat entry and timer reset", BardAscendedFirstCycleStartsOnCombatEntryAndTimerReset),
        new("bard ascended runtime does not cache level synced choices", BardAscendedRuntimeDoesNotCacheLevelSyncedChoices),
        new("bard ascended potion config is constructor safe", BardAscendedPotionConfigIsConstructorSafe),
        new("bard ascended runtime spends resonant ready before filler", BardAscendedRuntimeSpendsResonantReadyBeforeFiller),
        new("bard ascended runtime spends pitch perfect before burst hold", BardAscendedRuntimeSpendsPitchPerfectBeforeBurstHold),
        new("bard ascended custom timing follows standard burst path", BardAscendedCustomTimingFollowsStandardBurstPath),
        new("bard ascended battle voice waits only for available radiant finale", BardAscendedBattleVoiceWaitsOnlyForAvailableRadiantFinale),
        new("bard ascended potion presets map to expected timings", BardAscendedPotionPresetsMapToExpectedTimings),
        new("bard ascended custom potion timings reject empty input", BardAscendedCustomPotionTimingsRejectEmptyInput),
    ];
}
