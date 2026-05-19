using System.Collections.Frozen;

namespace RotationSolver.Basic.Data;

/// <summary>
/// Classifies PvP duty modes from territory metadata so live state owners do not
/// duplicate mode name checks.
/// </summary>
public static class PvPModeClassifier
{
	private const string CrystallineConflictContentFinderName = "Crystalline Conflict";
	private const string BorderlandRuinsFrontlineName = "The Borderland Ruins (Secure)";
	private const string SealRockFrontlineName = "Seal Rock (Seize)";
	private const string FieldsOfGloryFrontlineName = "The Fields of Glory (Shatter)";
	private const string OnsalHakairFrontlineName = "Onsal Hakair (Danshig Naadam)";
	private const string WorqorChirtehFrontlineName = "Worqor Chirteh (Triumph)";

	/// <summary>
	/// English content finder names currently treated as Frontline duties.
	/// </summary>
	public static IReadOnlySet<string> FrontlineContentFinderNames { get; } =
		new[]
		{
			BorderlandRuinsFrontlineName,
			SealRockFrontlineName,
			FieldsOfGloryFrontlineName,
			OnsalHakairFrontlineName,
			WorqorChirtehFrontlineName,
		}.ToFrozenSet(StringComparer.Ordinal);

	/// <summary>
	/// Returns true for the current English Crystalline Conflict content finder name.
	/// </summary>
	public static bool IsCrystallineConflict(string? contentFinderName)
	{
		return string.Equals(contentFinderName, CrystallineConflictContentFinderName, StringComparison.Ordinal);
	}

	/// <summary>
	/// Returns true for the current English Frontline content finder names.
	/// </summary>
	public static bool IsFrontline(string? contentFinderName)
	{
		if (string.IsNullOrEmpty(contentFinderName))
		{
			return false;
		}

		return FrontlineContentFinderNames.Contains(contentFinderName);
	}
}
