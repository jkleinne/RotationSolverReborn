using RotationSolver.RebornRotations.Ranged;

namespace RotationSolver.Tests;

internal static partial class PvETestSuite
{
    static void BardAscendedDotThresholdsHonorTargetTime()
    {
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldApplyBothDots(14.99f, isBossTarget: false, replacesEnhancedFiller: false),
            "both DoTs should reject targets below the 15 second floor");
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldApplyBothDots(15f, isBossTarget: false, replacesEnhancedFiller: false),
            "both DoTs should accept targets at the 15 second floor");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldApplyBothDots(17.99f, isBossTarget: false, replacesEnhancedFiller: true),
            "both DoTs should reject enhanced filler replacement below 18 seconds");
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldApplyBothDots(18f, isBossTarget: false, replacesEnhancedFiller: true),
            "both DoTs should accept enhanced filler replacement at 18 seconds");

        AssertFalse(
            BardAscendedDecisionPolicy.ShouldRefreshIronJaws(8.99f, isBossTarget: false, replacesEnhancedFiller: false),
            "Iron Jaws should reject targets below the 9 second floor");
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldRefreshIronJaws(9f, isBossTarget: false, replacesEnhancedFiller: false),
            "Iron Jaws should accept targets at the 9 second floor");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldRefreshIronJaws(11.99f, isBossTarget: false, replacesEnhancedFiller: true),
            "Iron Jaws should reject enhanced filler replacement below 12 seconds");
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldRefreshIronJaws(12f, isBossTarget: false, replacesEnhancedFiller: true),
            "Iron Jaws should accept enhanced filler replacement at 12 seconds");

        AssertFalse(
            BardAscendedDecisionPolicy.ShouldApplyCausticOnly(11.99f, isBossTarget: false),
            "Caustic Bite alone should reject targets below 12 seconds");
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldApplyCausticOnly(12f, isBossTarget: false),
            "Caustic Bite alone should accept targets at 12 seconds");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldApplyStormbiteOnly(14.99f, isBossTarget: false),
            "Stormbite alone should reject targets below 15 seconds");
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldApplyStormbiteOnly(15f, isBossTarget: false),
            "Stormbite alone should accept targets at 15 seconds");
    }

    static void BardAscendedDotThresholdsUseBossFallbackOnlyWhenTtkIsUnknown()
    {
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldApplyBothDots(float.NaN, isBossTarget: true, replacesEnhancedFiller: true),
            "boss fallback should allow both DoTs only when target time is unknown");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldApplyBothDots(14.99f, isBossTarget: true, replacesEnhancedFiller: false),
            "boss targets with known planned kill time should still honor the 15 second floor");
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldRefreshIronJaws(float.NaN, isBossTarget: true, replacesEnhancedFiller: true),
            "boss fallback should allow Iron Jaws only when target time is unknown");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldRefreshIronJaws(8.99f, isBossTarget: true, replacesEnhancedFiller: false),
            "boss targets with known planned kill time should still honor the 9 second floor");
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldApplyCausticOnly(float.NaN, isBossTarget: true),
            "boss fallback should allow Caustic Bite when target time is unknown");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldApplyStormbiteOnly(14.99f, isBossTarget: true),
            "boss targets with known planned kill time should still honor the Stormbite floor");
    }

    static void BardAscendedSongPresetsMapToExpectedDurations()
    {
        var standard = BardAscendedDecisionPolicy.GetSongDurations(
            BardAscendedSongTiming.Standard,
            new BardAscendedSongDurations(1f, 2f, 3f));
        var cycle369 = BardAscendedDecisionPolicy.GetSongDurations(
            BardAscendedSongTiming.Cycle369,
            new BardAscendedSongDurations(1f, 2f, 3f));
        var custom = BardAscendedDecisionPolicy.GetSongDurations(
            BardAscendedSongTiming.Custom,
            new BardAscendedSongDurations(40f, 38f, 37f));

        AssertEqual(new BardAscendedSongDurations(42f, 42f, 33f), standard, "standard should hold songs for the 3 3 12 preset");
        AssertEqual(new BardAscendedSongDurations(42f, 39f, 36f), cycle369, "cycle 3 6 9 should hold songs for the expected preset");
        AssertEqual(new BardAscendedSongDurations(40f, 38f, 37f), custom, "custom should return caller supplied durations");
    }

    static void BardAscendedApexSpendsDuringBurstAndMageBalladWindows()
    {
        AssertTrue(
            ShouldSpendApex(BardAscendedSongPhase.WanderersMinuet, soulVoice: 80, isInBurst: true),
            "Apex should spend at 80 Soul Voice during burst");
        AssertFalse(
            ShouldSpendApex(BardAscendedSongPhase.WanderersMinuet, soulVoice: 79, isInBurst: true),
            "Apex should hold below 80 Soul Voice during burst");
        AssertTrue(
            ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 100, songSecondsRemaining: 30f),
            "Apex should spend at 100 Soul Voice in Mage's Ballad");
        AssertTrue(
            ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 80, songSecondsRemaining: 18f),
            "Apex should spend at the early Mage's Ballad window boundary");
        AssertTrue(
            ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 80, songSecondsRemaining: 21f),
            "Apex should spend at the late Mage's Ballad window boundary");
        AssertFalse(
            ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 80, songSecondsRemaining: 17.99f),
            "Apex should hold before the Mage's Ballad window");
        AssertFalse(
            ShouldSpendApex(BardAscendedSongPhase.MagesBallad, soulVoice: 80, songSecondsRemaining: 21.01f),
            "Apex should hold after the Mage's Ballad window");
    }

    static void BardAscendedApexHoldsDuringArmyPaeon()
    {
        AssertFalse(
            ShouldSpendApex(BardAscendedSongPhase.ArmysPaeon, soulVoice: 100),
            "Apex should hold through Army's Paeon when no end of fight dump is needed");
        AssertFalse(
            ShouldSpendApex(BardAscendedSongPhase.ArmysPaeon, soulVoice: 80, isInBurst: false),
            "Apex should not spend only because Army's Paeon has enough Soul Voice");
    }

    static void BardAscendedApexUsesPlannedKillTimeOverSongFallback()
    {
        AssertTrue(
            ShouldSpendApex(
                BardAscendedSongPhase.ArmysPaeon,
                soulVoice: 80,
                targetSecondsRemaining: 4.96f,
                weaponTotalSeconds: 2.48f),
            "Apex should spend at 80 Soul Voice when planned kill time leaves two GCDs");
        AssertTrue(
            ShouldSpendApex(
                BardAscendedSongPhase.ArmysPaeon,
                soulVoice: 80,
                wouldUseIronJaws: true,
                targetSecondsRemaining: 4.96f,
                weaponTotalSeconds: 2.48f),
            "Apex should spend at 80 Soul Voice over Iron Jaws when planned kill time leaves two GCDs");
        AssertFalse(
            ShouldSpendApex(
                BardAscendedSongPhase.ArmysPaeon,
                soulVoice: 79,
                targetSecondsRemaining: 4.96f,
                weaponTotalSeconds: 2.48f),
            "Apex should not use the two GCD end of fight dump below 80 Soul Voice");
        AssertTrue(
            ShouldSpendApex(
                BardAscendedSongPhase.ArmysPaeon,
                soulVoice: 32,
                targetSecondsRemaining: 2.48f,
                weaponTotalSeconds: 2.48f,
                noFutureBlastPossible: true),
            "Apex should dump at 32 Soul Voice over Burst Shot when only one GCD remains");
        AssertFalse(
            ShouldSpendApex(
                BardAscendedSongPhase.ArmysPaeon,
                soulVoice: 31,
                targetSecondsRemaining: 2.48f,
                weaponTotalSeconds: 2.48f,
                noFutureBlastPossible: true),
            "Apex should hold below the Burst Shot dump threshold");
        AssertTrue(
            ShouldSpendApex(
                BardAscendedSongPhase.ArmysPaeon,
                soulVoice: 40,
                targetSecondsRemaining: 2.48f,
                weaponTotalSeconds: 2.48f,
                wouldUseEnhancedFiller: true,
                noFutureBlastPossible: true),
            "Apex should dump at 40 Soul Voice over enhanced filler when only one GCD remains");
        AssertFalse(
            ShouldSpendApex(
                BardAscendedSongPhase.ArmysPaeon,
                soulVoice: 39,
                targetSecondsRemaining: 2.48f,
                weaponTotalSeconds: 2.48f,
                wouldUseEnhancedFiller: true,
                noFutureBlastPossible: true),
            "Apex should hold below the enhanced filler dump threshold");
        AssertTrue(
            ShouldSpendApex(
                BardAscendedSongPhase.ArmysPaeon,
                soulVoice: 40,
                wouldUseIronJaws: true,
                targetSecondsRemaining: 2.48f,
                weaponTotalSeconds: 2.48f,
                wouldUseEnhancedFiller: true,
                noFutureBlastPossible: true),
            "Apex should dump at 40 Soul Voice over Iron Jaws when no future Blast Arrow is possible");
        AssertFalse(
            ShouldSpendApex(
                BardAscendedSongPhase.ArmysPaeon,
                soulVoice: 40,
                targetSecondsRemaining: 2.48f,
                weaponTotalSeconds: 2.48f,
                wouldUseEnhancedFiller: true,
                noFutureBlastPossible: false),
            "Apex should not dump low Soul Voice when a future Blast Arrow is still possible");
    }

    static void BardAscendedBlastArrowWaitsForUrgentGcds()
    {
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldUseBlastArrow(hasBlastReady: true, wouldUseDots: false, wouldUseIronJaws: false),
            "Blast Arrow should spend when Blast Ready is active and urgent GCDs are clear");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldUseBlastArrow(hasBlastReady: true, wouldUseDots: true, wouldUseIronJaws: false),
            "urgent DoTs should block Blast Ready spends");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldUseBlastArrow(hasBlastReady: true, wouldUseDots: false, wouldUseIronJaws: true),
            "Iron Jaws should block Blast Ready spends");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldUseBlastArrow(hasBlastReady: false, wouldUseDots: false, wouldUseIronJaws: false),
            "Blast Arrow should not spend without Blast Ready");
    }

    static void BardAscendedAoeThresholdsDistinguishGcdAndOgcd()
    {
        AssertFalse(BardAscendedDecisionPolicy.ShouldUseGcdAoE(1), "GCD AoE should reject one target");
        AssertTrue(BardAscendedDecisionPolicy.ShouldUseGcdAoE(2), "GCD AoE should start at two targets");
        AssertFalse(BardAscendedDecisionPolicy.ShouldUseOgcdAoE(2), "oGCD AoE should reject two targets");
        AssertTrue(BardAscendedDecisionPolicy.ShouldUseOgcdAoE(3), "oGCD AoE should start at three targets");
    }

    static void BardAscendedPotionPresetsMapToExpectedTimings()
    {
        AssertSequenceEqual(
            [0f],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Opener, []),
            "opener potion timing should be pull only");
        AssertSequenceEqual(
            [120f, 480f],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.TwoEight, []),
            "two eight potion timing should mirror the 2 and 8 minute preset");
        AssertSequenceEqual(
            [0f, 360f],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.ZeroSix, []),
            "zero six potion timing should mirror the opener and 6 minute preset");
        AssertSequenceEqual(
            [0f, 300f, 600f],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.ZeroFiveTen, []),
            "zero five ten potion timing should mirror the opener, 5 minute, and 10 minute preset");
        AssertSequenceEqual(
            [15f, 180f, 420f],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, [15f, 180f, 420f]),
            "custom potion timing should return caller supplied timing arrays");
        AssertSequenceEqual(
            [],
            BardAscendedDecisionPolicy.GetPotionTimings((BardAscendedPotionTiming)99, []),
            "unknown potion timing should fail closed");
    }

    static void BardAscendedCustomPotionTimingsRejectEmptyInput()
    {
        AssertSequenceEqual(
            [],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, null!),
            "custom potion timing should reject null timing arrays");
        AssertSequenceEqual(
            [],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, []),
            "custom potion timing should reject empty timing arrays");
        AssertSequenceEqual(
            [],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, [0f, 0f]),
            "custom potion timing should reject all zero timing arrays");
    }

    static bool ShouldSpendApex(
        BardAscendedSongPhase songPhase,
        byte soulVoice,
        bool isInBurst = false,
        bool wouldUseIronJaws = false,
        float songSecondsRemaining = 45f,
        float targetSecondsRemaining = float.PositiveInfinity,
        float weaponTotalSeconds = 2.48f,
        bool wouldUseEnhancedFiller = false,
        bool noFutureBlastPossible = false)
    {
        return BardAscendedDecisionPolicy.ShouldSpendApex(
            songPhase,
            soulVoice,
            isInBurst,
            wouldUseIronJaws,
            songSecondsRemaining,
            targetSecondsRemaining,
            weaponTotalSeconds,
            wouldUseEnhancedFiller,
            noFutureBlastPossible);
    }
}
