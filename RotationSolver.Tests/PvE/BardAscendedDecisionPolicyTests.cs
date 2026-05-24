using System.Text.RegularExpressions;
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
        var adjustedStandard = BardAscendedDecisionPolicy.GetSongDurations(
            BardAscendedSongTiming.AdjustedStandard,
            new BardAscendedSongDurations(1f, 2f, 3f));
        var custom = BardAscendedDecisionPolicy.GetSongDurations(
            BardAscendedSongTiming.Custom,
            new BardAscendedSongDurations(40f, 38f, 37f));

        AssertEqual(new BardAscendedSongDurations(42f, 42f, 33f), standard, "standard should hold songs for the 3 3 12 preset");
        AssertEqual(new BardAscendedSongDurations(42f, 42f, 33f), adjustedStandard, "adjusted standard should hold songs for the standard preset");
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

    static void BardAscendedApexCapFallbackRespectsBurstAvailability()
    {
        AssertFalse(
            ShouldSpendApex(BardAscendedSongPhase.WanderersMinuet, soulVoice: 100, canEnterBurst: true),
            "Apex should hold capped Soul Voice in Wanderer's Minuet when burst can enter");
        AssertTrue(
            ShouldSpendApex(BardAscendedSongPhase.WanderersMinuet, soulVoice: 100, canEnterBurst: false),
            "Apex should spend capped Soul Voice in Wanderer's Minuet when burst cannot enter");
        AssertFalse(
            ShouldSpendApex(BardAscendedSongPhase.ArmysPaeon, soulVoice: 100, canEnterBurst: true),
            "Apex should hold capped Soul Voice in Army's Paeon when burst can enter");
        AssertTrue(
            ShouldSpendApex(BardAscendedSongPhase.ArmysPaeon, soulVoice: 100, canEnterBurst: false),
            "Apex should spend capped Soul Voice in Army's Paeon when burst cannot enter");
        AssertFalse(
            ShouldSpendApex(
                BardAscendedSongPhase.WanderersMinuet,
                soulVoice: 100,
                wouldUseIronJaws: true,
                canEnterBurst: false),
            "Iron Jaws should still block capped Soul Voice fallback");
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

    static void BardAscendedFillerWaitsForEnhancedFillerOrResonantReady()
    {
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldUseFiller(hasEnhancedFiller: false, hasResonantReady: false),
            "filler should spend when no higher value filler or Resonant Ready is active");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldUseFiller(hasEnhancedFiller: true, hasResonantReady: false),
            "filler should wait for enhanced filler");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldUseFiller(hasEnhancedFiller: false, hasResonantReady: true),
            "filler should wait for Resonant Ready");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldUseFiller(hasEnhancedFiller: true, hasResonantReady: true),
            "filler should wait when both higher value actions are available");
    }

    static void BardAscendedAoeThresholdsDistinguishGcdAndOgcd()
    {
        AssertFalse(BardAscendedDecisionPolicy.ShouldUseGcdAoE(1), "GCD AoE should reject one target");
        AssertTrue(BardAscendedDecisionPolicy.ShouldUseGcdAoE(2), "GCD AoE should start at two targets");
        AssertFalse(BardAscendedDecisionPolicy.ShouldUseOgcdAoE(2), "oGCD AoE should reject two targets");
        AssertTrue(BardAscendedDecisionPolicy.ShouldUseOgcdAoE(3), "oGCD AoE should start at three targets");
    }

    static void BardAscendedRuntimeUsesResolvedAoeTargetCounts()
    {
        var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
        var enhancedFiller = ExtractMethodBody(source, "bool TryUseEnhancedFiller");
        var aoe = ExtractMethodBody(source, "bool TryUseAoE");
        var bloodletterVariant = ExtractMethodBody(source, "bool TryUseBloodletterVariant");

        AssertSourceMatches(
            source,
            @"\bprivate\s+static\s+bool\s+HasEnoughGcdAoETargets\s*\(\s*IAction\?\s+act\s*\)\s*=>\s*act\s+is\s+IBaseAction\s+baseAction\s*&&\s*BardAscendedDecisionPolicy\.ShouldUseGcdAoE\s*\(\s*baseAction\.Target\.AffectedTargets\.Length\s*\)\s*;",
            "BRD Ascended should gate GCD AoE by the resolved action affected target count");
        AssertSourceMatches(
            source,
            @"\bprivate\s+static\s+bool\s+HasEnoughOgcdAoETargets\s*\(\s*IAction\?\s+act\s*\)\s*=>\s*act\s+is\s+IBaseAction\s+baseAction\s*&&\s*BardAscendedDecisionPolicy\.ShouldUseOgcdAoE\s*\(\s*baseAction\.Target\.AffectedTargets\.Length\s*\)\s*;",
            "BRD Ascended should gate oGCD AoE by the resolved action affected target count");

        AssertSourceDoesNotMatch(
            enhancedFiller,
            @"\bNumberOfHostilesInRange\b",
            "enhanced filler AoE should not use field hostiles before target resolution");
        AssertSourceDoesNotMatch(
            aoe,
            @"\bNumberOfHostilesInRange\b",
            "GCD AoE should not use field hostiles before target resolution");
        AssertSourceDoesNotMatch(
            bloodletterVariant,
            @"\bNumberOfHostilesInRange\b",
            "Rain of Death should not use field hostiles before target resolution");

        AssertSourceMatches(
            enhancedFiller,
            @"\bprocAoE\.CanUse\s*\(\s*out\s+var\s+procAoEAct\s*,\s*skipAoeCheck\s*:\s*true\s*,\s*skipComboCheck\s*:\s*true\s*\)\s*&&\s*HasEnoughGcdAoETargets\s*\(\s*procAoEAct\s*\).*?\bact\s*=\s*procAoEAct\s*;",
            "enhanced filler AoE should assign only resolved targets that pass the Ascended GCD AoE threshold");
        AssertSourceMatches(
            aoe,
            @"\bprocAoE\.CanUse\s*\(\s*out\s+var\s+procAoEAct\s*,\s*skipAoeCheck\s*:\s*true\s*,\s*skipComboCheck\s*:\s*true\s*\)\s*&&\s*HasEnoughGcdAoETargets\s*\(\s*procAoEAct\s*\).*?\bact\s*=\s*procAoEAct\s*;",
            "proc AoE should assign only resolved targets that pass the Ascended GCD AoE threshold");
        AssertSourceMatches(
            aoe,
            @"\baoeAction\.CanUse\s*\(\s*out\s+var\s+aoeActionAct\s*,\s*skipAoeCheck\s*:\s*true\s*\)\s*&&\s*HasEnoughGcdAoETargets\s*\(\s*aoeActionAct\s*\).*?\bact\s*=\s*aoeActionAct\s*;",
            "standard AoE should assign only resolved targets that pass the Ascended GCD AoE threshold");
        AssertSourceMatches(
            bloodletterVariant,
            @"\bRainOfDeathPvE\.CanUse\s*\(\s*out\s+var\s+rainOfDeathAct\s*,\s*usedUp\s*:\s*usedUp\s*,\s*skipAoeCheck\s*:\s*true\s*\)\s*&&\s*HasEnoughOgcdAoETargets\s*\(\s*rainOfDeathAct\s*\).*?\bact\s*=\s*rainOfDeathAct\s*;",
            "Rain of Death should assign only resolved targets that pass the Ascended oGCD AoE threshold");
    }

    static void BardAscendedFirstCycleStartsOnCombatEntryAndTimerReset()
    {
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldStartFirstCycle(
                isInCombat: true,
                hasCombatState: false,
                currentCombatTime: 0.5f,
                previousCombatTime: 0f),
            "first cycle should start when combat begins without countdown state");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldStartFirstCycle(
                isInCombat: true,
                hasCombatState: true,
                currentCombatTime: 15f,
                previousCombatTime: 10f),
            "first cycle should not restart while combat time advances");
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldStartFirstCycle(
                isInCombat: true,
                hasCombatState: true,
                currentCombatTime: 0.25f,
                previousCombatTime: 120f),
            "first cycle should restart when a new pull resets combat time before an out of combat tick");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldStartFirstCycle(
                isInCombat: false,
                hasCombatState: true,
                currentCombatTime: 0f,
                previousCombatTime: 120f),
            "first cycle should not start while out of combat");
    }

    static void BardAscendedRuntimeDoesNotCacheLevelSyncedChoices()
    {
        var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));

        AssertSourceDoesNotMatch(
            source,
            @"\bfield\s*\?\?=",
            "BRD Ascended should not cache action choices that depend on EnoughLevel");
        AssertSourceDoesNotMatch(
            source,
            @"\bif\s*\(\s*field\s*!=\s*null\s*\)\s*return\s+field\s*;",
            "BRD Ascended should not reuse stale field-backed action lists after level sync");
        AssertSourceDoesNotMatch(
            source,
            @"\bprivate\s+IBaseAction\[\]\s+DoTActions\b",
            "BRD Ascended should not allocate DoT action arrays in runtime paths");
        AssertSourceDoesNotMatch(
            source,
            @"\bprivate\s+static\s+StatusID\[\]\s+BurstStatus\b",
            "BRD Ascended burst status selection depends on instance action availability");
        AssertSourceDoesNotMatch(
            source,
            @"\bprivate\s+static\s+IBaseAction\s+(ActiveFiller|ActiveBloodletterVariant)\b",
            "BRD Ascended level-synced action choices depend on instance action availability");
        AssertSourceDoesNotMatch(
            source,
            @"\bprivate\s+static\s+bool\s+(HasBurstActions|HasSongActions)\b",
            "BRD Ascended action availability checks depend on instance action availability");
        AssertSourceMatches(
            source,
            @"\bprivate\s+IBaseAction\s+ActiveFiller\s*=>\s*BurstShotPvE\.EnoughLevel\s*\?\s*BurstShotPvE\s*:\s*HeavyShotPvE\s*;",
            "BRD Ascended should select Heavy Shot when Burst Shot is not level-synced");
        AssertSourceMatches(
            source,
            @"\bprivate\s+IBaseAction\s+ActiveBloodletterVariant\s*=>\s*HeartbreakShotPvE\.EnoughLevel\s*\?\s*HeartbreakShotPvE\s*:\s*BloodletterPvE\s*;",
            "BRD Ascended should use one canonical Bloodletter variant for fallback and cooldown checks");
        AssertSourceMatches(
            source,
            @"\bTryUseFirstAvailableSong\s*\(\s*out\s+IAction\?\s+act\s*\).*?MagesBalladPvE\.EnoughLevel.*?MagesBalladPvE\.CanUse\(out\s+act\).*?ArmysPaeonPvE\.EnoughLevel\s*&&\s*ArmysPaeonPvE\.CanUse\(out\s+act\)",
            "BRD Ascended should start the first level-synced song instead of depending on unavailable song cooldowns");
        AssertSourceMatches(
            source,
            @"\bActiveBloodletterVariant\.CanUse\(out\s+act,\s*usedUp:\s*usedUp\)",
            "BRD Ascended prepull and combat Bloodletter paths should use the level-synced active variant");
        AssertSourceMatches(
            source,
            @"\bprivate\s+static\s+readonly\s+BardAscendedPotions\s+AscendedPotions\s*=\s*new\s*\(\s*\)\s*;",
            "BRD Ascended should keep potion config state available during base config discovery");
        AssertSourceDoesNotMatch(
            source,
            @"\bprivate\s+readonly\s+BardAscendedPotions\s+_ascendedPotions\b",
            "BRD Ascended should not put rotation config state behind post-base-constructor instance initialization");
        AssertSourceMatches(
            source,
            @"\bpublic\s+bool\s+ShouldUsePotion\s*\(\s*BRD_Ascended\s+rotation\s*,\s*out\s+IAction\?\s+act\s*,\s*bool\s+clippingCheck\s*=\s*true\s*\)",
            "BRD Ascended potion conditions should receive the active rotation at runtime");
        AssertSourceDoesNotMatch(
            source,
            @"\bif\s*\(\s*InBurst\s*\)\s*return\s+true\s*;",
            "BRD Ascended nested potion conditions should not read instance burst state without an owner");
        AssertSourceMatches(
            source,
            @"\bif\s*\(\s*_rotation\?\.InBurst\s*==\s*true\s*\)\s*return\s+true\s*;",
            "BRD Ascended nested potion conditions should read burst state from the active rotation context");
        AssertSourceMatches(
            source,
            @"\bfinally\s*\{.*?_rotation\s*=\s*null\s*;.*?\}",
            "BRD Ascended potion conditions should clear the active rotation after each check");
        AssertSourceDoesNotMatch(
            source,
            @"\bif\s*\(\s*!\s*Is369\s*\|\|\s*!\s*ShouldSwapSong\s*\)\s*return\s+false\s*;",
            "BRD Ascended custom song timing should not be blocked from Army's Paeon");
    }

    static void BardAscendedPotionConfigIsConstructorSafe()
    {
        var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));

        AssertSourceMatches(
            source,
            @"\bprivate\s+static\s+readonly\s+BardAscendedPotions\s+AscendedPotions\s*=\s*new\s*\(\s*\)\s*;",
            "BRD Ascended potion config state should be available before base rotation config discovery runs");
        AssertSourceDoesNotMatch(
            source,
            @"\bprivate\s+readonly\s+BardAscendedPotions\s+_ascendedPotions\b",
            "BRD Ascended should not initialize potion config state after the base constructor reads rotation configs");
        AssertSourceDoesNotMatch(
            source,
            @"\bget\s*=>\s*_ascendedPotions\.",
            "BRD Ascended rotation config getters should not depend on post-base-constructor instance fields");
        AssertSourceMatches(
            source,
            @"\bAscendedPotions\.ShouldUsePotion\s*\(\s*this\s*,\s*out\s+(var\s+)?(?:potionAct|act)\s*\)",
            "BRD Ascended should pass the active rotation when checking potion conditions");
        AssertSourceMatches(
            source,
            @"\bpublic\s+bool\s+ShouldUsePotion\s*\(\s*BRD_Ascended\s+rotation\s*,\s*out\s+IAction\?\s+act\s*,\s*bool\s+clippingCheck\s*=\s*true\s*\)",
            "BRD Ascended potion helper should accept the active rotation during runtime checks");
    }

    static void BardAscendedRuntimeSpendsResonantReadyBeforeFiller()
    {
        var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
        var generalGcd = ExtractMethodBody(source, "GeneralGCD");

        AssertSourceMatches(
            generalGcd,
            @"\bTryUseBurst\(out\s+act\).*?\bTryUseApexArrow\(out\s+act\).*?\bTryUseBlastArrow\(out\s+act\).*?\bTryUseResonantArrow\(out\s+act\).*?\bTryUseFiller\(out\s+act\)",
            "BRD Ascended should reach Resonant Arrow before filler even when burst is inactive");
    }

    static void BardAscendedRuntimeSpendsPitchPerfectBeforeBurstHold()
    {
        var source = StripSourceComments(File.ReadAllText(RepositoryPath("RotationSolver", "RebornRotations", "Ranged", "BRD_Ascended.cs")));
        var pitchPerfect = ExtractMethodBody(source, "bool TryUsePitchPerfect");

        AssertSourceDoesNotMatch(
            pitchPerfect,
            @"!\s*InBurst\s*&&\s*!\s*RagingStrikesPvE\.Cooldown\.IsCoolingDown",
            "Pitch Perfect should not inherit the pre-stack burst-ready hold");
        AssertSourceMatches(
            pitchPerfect,
            @"\bPitchPerfectPvE\.CanUse\s*\(\s*out\s+act\s*,\s*skipAoeCheck\s*:\s*true\s*,\s*skipComboCheck\s*:\s*true\s*\)",
            "Pitch Perfect should skip AoE and combo checks before evaluating stack safety");
        AssertSourceMatches(
            pitchPerfect,
            @"\bif\s*\(\s*Repertoire\s*==\s*3\s*\)\s*return\s+true\s*;",
            "Pitch Perfect should still spend immediately at three stacks");
    }

    static void BardAscendedCustomTimingFollowsStandardBurstPath()
    {
        AssertTrue(
            BardAscendedDecisionPolicy.UsesStandardBurstPath(BardAscendedSongTiming.Standard),
            "standard timing should use the standard burst path");
        AssertTrue(
            BardAscendedDecisionPolicy.UsesStandardBurstPath(BardAscendedSongTiming.AdjustedStandard),
            "adjusted standard timing should use the standard burst path");
        AssertTrue(
            BardAscendedDecisionPolicy.UsesStandardBurstPath(BardAscendedSongTiming.Custom),
            "custom timing should use the standard burst path with custom song durations");
        AssertFalse(
            BardAscendedDecisionPolicy.UsesStandardBurstPath(BardAscendedSongTiming.Cycle369),
            "3 6 9 timing keeps its dedicated burst path");
    }

    static void BardAscendedBattleVoiceWaitsOnlyForAvailableRadiantFinale()
    {
        AssertTrue(
            BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
                radiantFinaleEnoughLevel: true,
                radiantFinaleCanUse: true,
                hasRadiantFinale: false,
                wasRadiantFinaleLastAction: false),
            "Battle Voice should wait when Radiant Finale is available but not applied");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
                radiantFinaleEnoughLevel: false,
                radiantFinaleCanUse: false,
                hasRadiantFinale: false,
                wasRadiantFinaleLastAction: false),
            "Battle Voice should not wait for Radiant Finale below Radiant Finale level");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
                radiantFinaleEnoughLevel: true,
                radiantFinaleCanUse: false,
                hasRadiantFinale: false,
                wasRadiantFinaleLastAction: false),
            "Battle Voice should not wait when Radiant Finale is unlocked but unavailable");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
                radiantFinaleEnoughLevel: true,
                radiantFinaleCanUse: true,
                hasRadiantFinale: true,
                wasRadiantFinaleLastAction: false),
            "Battle Voice should not wait after Radiant Finale status is active");
        AssertFalse(
            BardAscendedDecisionPolicy.ShouldWaitForRadiantFinaleBeforeBattleVoice(
                radiantFinaleEnoughLevel: true,
                radiantFinaleCanUse: true,
                hasRadiantFinale: false,
                wasRadiantFinaleLastAction: true),
            "Battle Voice should not wait immediately after Radiant Finale was used");
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
        AssertSequenceEqual(
            [300f],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, [300f, 0f, 0f]),
            "custom potion timing should filter unused zero timing slots");
        AssertSequenceEqual(
            [300f],
            BardAscendedDecisionPolicy.GetPotionTimings(BardAscendedPotionTiming.Custom, [0f, 300f]),
            "custom potion timing should treat zero as an unused custom timing slot");
    }

    static bool ShouldSpendApex(
        BardAscendedSongPhase songPhase,
        byte soulVoice,
        bool isInBurst = false,
        bool wouldUseIronJaws = false,
        bool canEnterBurst = true,
        float songSecondsRemaining = 45f,
        float targetSecondsRemaining = float.PositiveInfinity,
        float weaponTotalSeconds = 2.48f,
        bool wouldUseEnhancedFiller = false,
        bool noFutureBlastPossible = false)
    {
        var input = new BardAscendedApexDecisionInput(
            SongPhase: songPhase,
            SoulVoice: soulVoice,
            IsInBurst: isInBurst,
            WouldUseIronJaws: wouldUseIronJaws,
            CanEnterBurst: canEnterBurst,
            SongSecondsRemaining: songSecondsRemaining,
            TargetSecondsRemaining: targetSecondsRemaining,
            WeaponTotalSeconds: weaponTotalSeconds,
            WouldUseEnhancedFiller: wouldUseEnhancedFiller,
            NoFutureBlastPossible: noFutureBlastPossible);

        return BardAscendedDecisionPolicy.ShouldSpendApex(input);
    }

    static string RepositoryPath(params string[] parts)
    {
        var root = FindRepositoryRoot();
        var segments = new string[parts.Length + 1];
        segments[0] = root;
        Array.Copy(parts, 0, segments, 1, parts.Length);
        return Path.Combine(segments);
    }

    static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var gitPath = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root");
    }

    static void AssertSourceMatches(string source, string pattern, string message)
    {
        AssertTrue(SourcePattern(pattern).IsMatch(source), message);
    }

    static void AssertSourceDoesNotMatch(string source, string pattern, string message)
    {
        AssertFalse(SourcePattern(pattern).IsMatch(source), message);
    }

    static Regex SourcePattern(string pattern)
    {
        return new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.Singleline);
    }

    static string ExtractMethodBody(string source, string methodName)
    {
        var methodStart = source.IndexOf($"{methodName}(", StringComparison.Ordinal);
        if (methodStart < 0)
        {
            throw new InvalidOperationException($"Could not locate method {methodName}");
        }

        var bodyStart = source.IndexOf('{', methodStart);
        if (bodyStart < 0)
        {
            throw new InvalidOperationException($"Could not locate method body for {methodName}");
        }

        var depth = 0;
        for (var index = bodyStart; index < source.Length; index++)
        {
            if (source[index] == '{') depth++;
            if (source[index] != '}') continue;

            depth--;
            if (depth == 0)
            {
                return source[bodyStart..(index + 1)];
            }
        }

        throw new InvalidOperationException($"Could not locate method end for {methodName}");
    }

    static string StripSourceComments(string source)
    {
        return Regex.Replace(
            source,
            @"//.*?$|/\*.*?\*/",
            string.Empty,
            RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Singleline);
    }
}
