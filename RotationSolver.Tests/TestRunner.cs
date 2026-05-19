namespace RotationSolver.Tests;

internal static class TestRunner
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The console test harness isolates each test failure and continues running the catalog.")]
	internal static int Run(IReadOnlyList<TestCase> tests)
	{
		var failures = new List<string>();

		foreach (var test in tests)
		{
			try
			{
				test.Test();
			}
			catch (Exception ex)
			{
				failures.Add($"{test.Name}: {ex.Message}");
			}
		}

		if (failures.Count > 0)
		{
			foreach (var failure in failures)
			{
				Console.Error.WriteLine(failure);
			}

			return 1;
		}

		Console.WriteLine($"Passed {tests.Count} tests.");
		return 0;
	}
}
