using RotationSolver.Commands;

var tests = new (string Name, Action Test)[]
{
	("after-combat cancel is cleared by territory change", AfterCombatCancelIsClearedByTerritoryChange),
	("after-combat cancel does not fire after combat restarts", AfterCombatCancelDoesNotFireAfterCombatRestarts),
	("after-combat cancel fires only after expiry in same context", AfterCombatCancelFiresOnlyAfterExpiryInSameContext),
	("countdown-owned state continues into combat", CountdownOwnedStateContinuesIntoCombat),
	("countdown cleanup does not cancel user-owned state", CountdownCleanupDoesNotCancelUserOwnedState),
	("countdown cleanup cancels owned state without combat", CountdownCleanupCancelsOwnedStateWithoutCombat),
	("disabled countdown clears ownership", DisabledCountdownClearsOwnership),
};

var failures = new List<string>();
foreach (var (name, test) in tests)
{
	try
	{
		test();
	}
	catch (Exception ex)
	{
		failures.Add($"{name}: {ex.Message}");
	}
}

if (failures.Count > 0)
{
	foreach (var failure in failures)
	{
		Console.Error.WriteLine(failure);
	}

	Environment.Exit(1);
}

Console.WriteLine($"Passed {tests.Length} auto-off policy tests.");

static void AfterCombatCancelIsClearedByTerritoryChange()
{
	var now = new DateTime(2026, 5, 13, 12, 0, 0, DateTimeKind.Utc);
	var pendingCancelTime = now.AddSeconds(30);

	var shouldClear = AutoOffPolicy.ShouldClearPendingAfterCombatCancel(
		pendingCancelTime,
		isStateEnabled: true,
		isInCombat: false,
		didTerritoryChange: true);

	AssertTrue(shouldClear, "territory changes must invalidate pending after-combat cancels");
}

static void AfterCombatCancelDoesNotFireAfterCombatRestarts()
{
	var now = new DateTime(2026, 5, 13, 12, 0, 0, DateTimeKind.Utc);
	var expiredCancelTime = now.AddSeconds(-1);

	var shouldCancel = AutoOffPolicy.ShouldCancelForPendingAfterCombat(
		expiredCancelTime,
		now,
		isStateEnabled: true,
		isInCombat: true);

	AssertFalse(shouldCancel, "combat restart must prevent stale after-combat cancellation");
}

static void AfterCombatCancelFiresOnlyAfterExpiryInSameContext()
{
	var now = new DateTime(2026, 5, 13, 12, 0, 0, DateTimeKind.Utc);
	var futureCancelTime = now.AddSeconds(1);
	var expiredCancelTime = now.AddSeconds(-1);

	AssertFalse(
		AutoOffPolicy.ShouldCancelForPendingAfterCombat(
			futureCancelTime,
			now,
			isStateEnabled: true,
			isInCombat: false),
		"pending after-combat cancel must wait until its expiry");

	AssertTrue(
		AutoOffPolicy.ShouldCancelForPendingAfterCombat(
			expiredCancelTime,
			now,
			isStateEnabled: true,
			isInCombat: false),
		"expired after-combat cancel should fire when context still matches");
}

static void CountdownOwnedStateContinuesIntoCombat()
{
	var state = AutoOffPolicy.CountdownAutoState.None;
	var started = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: true,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 10f,
		isStateEnabled: false,
		isInCombat: false,
		countdownStartsManualMode: false,
		state);

	AssertTrue(started.ShouldStartState, "countdown should start rotation when state is off");
	AssertTrue(started.NextState.OwnsActiveState, "countdown should own state it started");

	var completed = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: true,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 0f,
		isStateEnabled: true,
		isInCombat: true,
		countdownStartsManualMode: false,
		started.NextState);

	AssertFalse(completed.ShouldCancelState, "countdown completion must not cancel state after combat starts");
	AssertFalse(completed.NextState.OwnsActiveState, "countdown ownership should be released after pull starts");
}

static void CountdownCleanupDoesNotCancelUserOwnedState()
{
	var userOwnedState = new AutoOffPolicy.CountdownAutoState(
		LastCountdownTime: 10f,
		OwnsActiveState: false);

	var decision = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: true,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 0f,
		isStateEnabled: true,
		isInCombat: false,
		countdownStartsManualMode: false,
		userOwnedState);

	AssertFalse(decision.ShouldCancelState, "countdown cleanup must not cancel user-owned Auto mode");
}

static void CountdownCleanupCancelsOwnedStateWithoutCombat()
{
	var countdownOwnedState = new AutoOffPolicy.CountdownAutoState(
		LastCountdownTime: 10f,
		OwnsActiveState: true);

	var decision = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: true,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 0f,
		isStateEnabled: true,
		isInCombat: false,
		countdownStartsManualMode: false,
		countdownOwnedState);

	AssertTrue(decision.ShouldCancelState, "countdown cleanup should cancel state it started if combat never begins");
}

static void DisabledCountdownClearsOwnership()
{
	var countdownOwnedState = new AutoOffPolicy.CountdownAutoState(
		LastCountdownTime: 10f,
		OwnsActiveState: true);

	var decision = AutoOffPolicy.EvaluateCountdown(
		isStartOnCountdownEnabled: false,
		isInDutyReplay: false,
		isPvP: false,
		countdownTime: 0f,
		isStateEnabled: true,
		isInCombat: false,
		countdownStartsManualMode: false,
		countdownOwnedState);

	AssertFalse(decision.ShouldCancelState, "disabled countdown handling must not cancel state");
	AssertFalse(decision.NextState.OwnsActiveState, "disabled countdown handling should clear ownership");
}

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
