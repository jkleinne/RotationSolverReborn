using System.Text.Json;

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
}
