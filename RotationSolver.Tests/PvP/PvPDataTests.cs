using System.Text.Json;
using Dalamud.Game.ClientState.Statuses;
using RotationSolver.Basic.Actions.PvPTargetSelection;

namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	static void PvpLbJsonContainsVerifiedEntries()
	{
		using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("RotationSolver.Basic", "Data", "PvPLBs.json")));
		var root = document.RootElement;
		var expectedEntries = new Dictionary<uint, (string Category, string Description)>
		{
			[29069] = ("Utility", "PLD Phalanx"),
			[29083] = ("Utility", "WAR Primal Scream"),
			[29097] = ("Offensive", "DRK Eventide"),
			[29130] = ("Offensive", "GNB Relentless Rush"),
			[29485] = ("Offensive", "MNK Meteodrive"),
			[29497] = ("Offensive", "DRG Sky High"),
			[29515] = ("Offensive", "NIN Seiton Tenchu"),
			[29537] = ("Offensive", "SAM Zantetsuken"),
			[29553] = ("Utility", "RPR Tenebrae Lemurum"),
			[39190] = ("Offensive", "VPR World-swallower"),
			[29401] = ("Utility", "BRD Final Fantasia"),
			[29415] = ("Offensive", "MCH Marksman's Spite"),
			[29432] = ("Utility", "DNC Contradance"),
			[29662] = ("Utility", "BLM Soul Resonance"),
			[29673] = ("Offensive", "SMN Summon Bahamut"),
			[41498] = ("Offensive", "RDM Southern Cross"),
			[39215] = ("Utility", "PCT Advent of Chocobastion"),
			[29230] = ("Healing", "WHM Afflatus Purgation"),
			[41502] = ("Healing", "SCH Seraphism"),
			[29255] = ("Healing", "AST Celestial River"),
			[29266] = ("Healing", "SGE Mesotes"),
		};
		var seenActionIds = new HashSet<uint>();

		AssertEqual(JsonValueKind.Array, root.ValueKind, "PvPLBs.json should be an array");
		AssertEqual(expectedEntries.Count, root.GetArrayLength(), "PvPLBs.json should contain the verified PvP LB entries");

		foreach (var entry in root.EnumerateArray())
		{
			var actionId = GetRequiredUInt(entry, "ActionId");
			AssertTrue(seenActionIds.Add(actionId), $"PvP LB action id {actionId} should be unique");
			AssertTrue(expectedEntries.TryGetValue(actionId, out var expected), $"PvP LB action id {actionId} should be verified");
			AssertEqual(expected.Category, GetRequiredString(entry, "Category"), $"PvP LB action id {actionId} should have verified category");
			AssertEqual(expected.Description, GetRequiredString(entry, "Description"), $"PvP LB action id {actionId} should have verified description");
		}
	}

	static void PvpMitigationJsonContainsResilience()
	{
		using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("RotationSolver.Basic", "Data", "PvPMitigations.json")));
		var root = document.RootElement;
		var seenIds = new HashSet<string>();

		AssertEqual(JsonValueKind.Array, root.ValueKind, "PvPMitigations.json should be an array");

		foreach (var entry in root.EnumerateArray())
		{
			var id = GetRequiredString(entry, "Id");
			AssertTrue(seenIds.Add(id), $"PvP mitigation id {id} should be unique");

			if (id != "Resilience")
			{
				continue;
			}

			AssertEqual("HeavyDR", GetRequiredString(entry, "Kind"), "Resilience should be modeled as control protection");
			AssertEqual(0.0, GetRequiredDouble(entry, "DamageReductionPercent"), "Resilience should not add damage reduction");
			return;
		}

		throw new InvalidOperationException("PvPMitigations.json should represent Resilience as non-invulnerability control protection");
	}

	static void PvpMitigationJsonContainsRankedCcDefensiveCoverage()
	{
		using var document = JsonDocument.Parse(File.ReadAllText(RepositoryPath("RotationSolver.Basic", "Data", "PvPMitigations.json")));
		var root = document.RootElement;
		var entries = new Dictionary<string, (string Kind, double DamageReductionPercent)>();

		foreach (var entry in root.EnumerateArray())
		{
			entries[GetRequiredString(entry, "Id")] = (
				GetRequiredString(entry, "Kind"),
				GetRequiredDouble(entry, "DamageReductionPercent"));
		}

		var expected = new Dictionary<string, (string Kind, double DamageReductionPercent)>
		{
			["Guard"] = ("Invuln", 0.0),
			["HallowedGround_1302"] = ("Invuln", 0.0),
			["GuardiansWill"] = ("Invuln", 0.0),
			["Phalanx"] = ("HeavyDR", 0.33),
			["UndeadRedemption"] = ("Invuln", 0.0),
			["Hidden_1316"] = ("Invuln", 0.0),
			["HardenedScales"] = ("HeavyDR", 0.50),
			["Forte"] = ("HeavyDR", 0.50),
			["WardensGrace"] = ("HeavyDR", 0.25),
			["RelentlessRush"] = ("HeavyDR", 0.25),
			["RadiantAegis_3224"] = ("HeavyDR", 0.25),
			["FanDance"] = ("HeavyDR", 0.20),
			["SaltedEarth_3037"] = ("HeavyDR", 0.20),
			["ClarityOfCorundum"] = ("HeavyDR", 0.10),
			["Catalyze"] = ("HeavyDR", 0.10),
		};

		foreach (var (id, expectedEntry) in expected)
		{
			AssertTrue(entries.TryGetValue(id, out var actual), $"PvPMitigations.json should include ranked CC defensive status {id}");
			AssertEqual(expectedEntry.Kind, actual.Kind, $"PvPMitigations.json should classify {id}");
			AssertEqual(expectedEntry.DamageReductionPercent, actual.DamageReductionPercent, $"PvPMitigations.json should set DR for {id}");
		}
	}

	static void EffectiveHpIgnoringGuardKeepsDamageReduction()
	{
		var database = new TestMitigationDatabase(
			new MitigationEntry(StatusID.Guard, MitigationKind.Invuln, 0.0, "Guard"),
			new MitigationEntry(StatusID.Forte, MitigationKind.HeavyDR, 0.50, "Forte"));
		var target = new MitigatedBattleChara(
			currentHp: 1_000,
			maxHp: 10_000,
			StatusID.Guard,
			StatusID.Forte);

		var effectiveHp = EffectiveHpCalculator.ComputeIgnoringGuard(target, database);

		AssertEqual(2_000.0, effectiveHp, "guard should be ignored while damage reduction still applies");
	}

	static void EffectiveHpIgnoringGuardKeepsNonGuardInvulnerability()
	{
		var database = new TestMitigationDatabase(
			new MitigationEntry(StatusID.Guard, MitigationKind.Invuln, 0.0, "Guard"),
			new MitigationEntry(StatusID.HallowedGround_1302, MitigationKind.Invuln, 0.0, "Hallowed Ground"));
		var target = new MitigatedBattleChara(
			currentHp: 1_000,
			maxHp: 10_000,
			StatusID.Guard,
			StatusID.HallowedGround_1302);

		var effectiveHp = EffectiveHpCalculator.ComputeIgnoringGuard(target, database);

		AssertTrue(double.IsPositiveInfinity(effectiveHp), "non guard invulnerability should still block damage");
	}

	static void LiveTargetFactsUseCallerHealthProvider()
	{
		var target = new MitigatedBattleChara(currentHp: 1_000, maxHp: 10_000);
		var context = CreateLiveFactsContext(
			healthRatioProvider: combatant => combatant.GameObjectId == target.GameObjectId ? 0.73f : 0.0f);

		var facts = PvPLiveTargetFactsBuilder.Create(target, context);

		AssertEqual(0.73f, facts.HealthRatio, "live target facts should preserve caller health ratio semantics");
	}

	static void LiveTargetFactsUseCallerStatusDelegate()
	{
		var target = new MitigatedBattleChara(currentHp: 1_000, maxHp: 10_000);
		var context = CreateLiveFactsContext(
			hasStatus: (combatant, statusId) =>
				combatant.GameObjectId == target.GameObjectId
				&& (statusId == StatusID.Guard || statusId == StatusID.Resilience));

		var facts = PvPLiveTargetFactsBuilder.Create(target, context);

		AssertTrue(facts.HasGuard, "live target facts should preserve caller Guard status semantics");
		AssertTrue(facts.HasResilience, "live target facts should preserve caller Resilience status semantics");
		AssertFalse(facts.IsExposed, "caller Guard status should drive exposure");
	}

	static void LiveTargetFactsUseCallerDistanceProvider()
	{
		var target = new MitigatedBattleChara(currentHp: 1_000, maxHp: 10_000);
		var context = CreateLiveFactsContext(
			distanceToPlayerProvider: combatant => combatant.GameObjectId == target.GameObjectId ? 10f : 999f);

		var facts = PvPLiveTargetFactsBuilder.Create(target, context);

		AssertTrue(facts.IsInNormalRange, "live target facts should preserve caller range semantics");
		AssertTrue(facts.IsExposed, "caller range semantics should drive exposure");
	}

	static void LiveTargetFactsExposeAllyFocusCount()
	{
		var target = new MitigatedBattleChara(currentHp: 1_000, maxHp: 10_000, objectId: 99);
		var allies = new[]
		{
			new PvPCombatantSnapshot(1, 1.0f, 1, target.GameObjectId, default, 0f),
			new PvPCombatantSnapshot(2, 1.0f, 1, target.GameObjectId, default, 0f),
			new PvPCombatantSnapshot(3, 1.0f, 1, 42UL, default, 0f),
		};
		var context = CreateLiveFactsContext(allies: allies);

		var facts = PvPLiveTargetFactsBuilder.Create(target, context);

		AssertEqual(2, facts.AllyFocusCount, "live target facts should preserve the exact ally focus count");
		AssertTrue(facts.HasAllyFocus, "live target facts should derive ally focus from the count");
	}

	static void LiveCombatantSnapshotUsesCallerHealthProvider()
	{
		var combatant = new MitigatedBattleChara(currentHp: 1_000, maxHp: 10_000);
		float HealthRatioProvider(IBattleChara candidate)
		{
			return candidate.GameObjectId == combatant.GameObjectId ? 0.42f : 0.0f;
		}

		var snapshot = PvPLiveTargetFactsBuilder.ToCombatantSnapshot(combatant, HealthRatioProvider);

		AssertEqual(0.42f, snapshot.HealthRatio, "combatant snapshot should preserve caller health ratio semantics");
	}

	static void LiveCombatantSnapshotsSkipNullsAndUseCallerHealthProvider()
	{
		var firstCombatant = new MitigatedBattleChara(currentHp: 1_000, maxHp: 10_000, objectId: 10);
		var secondCombatant = new MitigatedBattleChara(currentHp: 1_000, maxHp: 10_000, objectId: 20);
		var combatants = new IBattleChara?[]
		{
			firstCombatant,
			null,
			secondCombatant,
		};

		var snapshots = PvPLiveTargetFactsBuilder.ToCombatantSnapshots(
			combatants,
			combatant => combatant.GameObjectId == firstCombatant.GameObjectId ? 0.25f : 0.75f);

		AssertEqual(2, snapshots.Count, "combatant snapshot list should ignore null live entries");
		AssertEqual(10UL, snapshots[0].ObjectId, "combatant snapshot list should preserve the first live object id");
		AssertEqual(0.25f, snapshots[0].HealthRatio, "combatant snapshot list should use caller health ratio for first live object");
		AssertEqual(20UL, snapshots[1].ObjectId, "combatant snapshot list should preserve the second live object id");
		AssertEqual(0.75f, snapshots[1].HealthRatio, "combatant snapshot list should use caller health ratio for second live object");
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

	static string GetRequiredString(JsonElement element, string propertyName)
	{
		if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
		{
			throw new InvalidOperationException($"JSON entry should contain string property {propertyName}");
		}

		return property.GetString() ?? string.Empty;
	}

	static uint GetRequiredUInt(JsonElement element, string propertyName)
	{
		if (!element.TryGetProperty(propertyName, out var property) || !property.TryGetUInt32(out var value))
		{
			throw new InvalidOperationException($"JSON entry should contain unsigned integer property {propertyName}");
		}

		return value;
	}

	static double GetRequiredDouble(JsonElement element, string propertyName)
	{
		if (!element.TryGetProperty(propertyName, out var property) || !property.TryGetDouble(out var value))
		{
			throw new InvalidOperationException($"JSON entry should contain numeric property {propertyName}");
		}

		return value;
	}

	private sealed class TestMitigationDatabase(params MitigationEntry[] entries) : IMitigationDatabase
	{
		private readonly Dictionary<StatusID, MitigationEntry> entriesById = entries.ToDictionary(entry => entry.Id);

		public bool TryGet(StatusID id, out MitigationEntry entry)
		{
			return entriesById.TryGetValue(id, out entry);
		}
	}

	private static PvPLiveTargetFactsContext CreateLiveFactsContext(
		Func<IBattleChara, float>? healthRatioProvider = null,
		Func<IBattleChara, float>? distanceToPlayerProvider = null,
		Func<IBattleChara, StatusID, bool>? hasStatus = null,
		IReadOnlyList<PvPCombatantSnapshot>? allies = null)
	{
		return new PvPLiveTargetFactsContext(
			MitigationDatabase: new TestMitigationDatabase(),
			ObjectiveRelevantTargetIds: new HashSet<ulong>(),
			Allies: allies ?? [],
			CurrentTime: TimeSpan.Zero,
			GuardCooldownTracker: new PvPGuardCooldownTracker(),
			GuardReactionWindow: TimeSpan.Zero,
			ActionRange: 25f,
			DistanceToPlayerProvider: distanceToPlayerProvider ?? (_ => 0.0f),
			HealthRatioProvider: healthRatioProvider ?? (_ => 0.0f),
			HasStatus: hasStatus ?? ((_, _) => false));
	}

	private sealed class MitigatedBattleChara : IBattleChara
	{
		public MitigatedBattleChara(
			uint currentHp,
			uint maxHp,
			params StatusID[] statuses)
			: this(currentHp, maxHp, objectId: 1, statuses)
		{
		}

		public MitigatedBattleChara(
			uint currentHp,
			uint maxHp,
			ulong objectId,
			params StatusID[] statuses)
		{
			GameObjectId = objectId;
			CurrentHp = currentHp;
			MaxHp = maxHp;
			StatusList = statuses.Select(status => new TestStatus((uint)status)).ToArray();
		}

		public ulong GameObjectId { get; }

		public uint CurrentHp { get; }

		public uint MaxHp { get; }

		public IReadOnlyList<IStatus> StatusList { get; }
	}

	private sealed class TestStatus(uint statusId) : IStatus
	{
		public uint StatusId { get; } = statusId;
	}
}
