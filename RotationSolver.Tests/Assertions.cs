namespace RotationSolver.Tests;

internal static partial class PvPTestSuite
{
	static void AssertTrue(bool actual, string message)
	{
		if (!actual)
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

	static void AssertFalse(bool actual, string message)
	{
		if (actual)
		{
			throw new InvalidOperationException(message);
		}
	}
}
