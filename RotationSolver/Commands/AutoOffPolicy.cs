namespace RotationSolver.Commands;

internal static class AutoOffPolicy
{
	private const float CompletedCountdownMinimumSeconds = 0.2f;

	internal readonly record struct CountdownAutoState(float LastCountdownTime, bool OwnsActiveState)
	{
		internal static CountdownAutoState None => new(0f, false);
	}

	internal readonly record struct CountdownAutoDecision(
		CountdownAutoState NextState,
		bool ShouldStartState,
		bool StartManualMode,
		bool ShouldCancelState);

	internal static bool ShouldClearPendingAfterCombatCancel(
		DateTime autoCancelTime,
		bool isStateEnabled,
		bool isInCombat,
		bool didTerritoryChange)
	{
		return autoCancelTime != DateTime.MinValue
			&& (didTerritoryChange || !isStateEnabled || isInCombat);
	}

	internal static bool ShouldCancelForPendingAfterCombat(
		DateTime autoCancelTime,
		DateTime now,
		bool isStateEnabled,
		bool isInCombat)
	{
		return autoCancelTime != DateTime.MinValue
			&& isStateEnabled
			&& !isInCombat
			&& now > autoCancelTime;
	}

	internal static CountdownAutoDecision EvaluateCountdown(
		bool isStartOnCountdownEnabled,
		bool isInDutyReplay,
		bool isPvP,
		float countdownTime,
		bool isStateEnabled,
		bool isInCombat,
		bool countdownStartsManualMode,
		CountdownAutoState state)
	{
		if (!isStartOnCountdownEnabled || isInDutyReplay || isPvP)
		{
			return new(CountdownAutoState.None, false, countdownStartsManualMode, false);
		}

		if (countdownTime > 0)
		{
			var ownsActiveState = state.OwnsActiveState && isStateEnabled;
			var shouldStartState = !isStateEnabled;
			if (shouldStartState)
			{
				ownsActiveState = true;
			}

			return new(new CountdownAutoState(countdownTime, ownsActiveState), shouldStartState, countdownStartsManualMode, false);
		}

		if (countdownTime == 0 && state.LastCountdownTime > CompletedCountdownMinimumSeconds)
		{
			var shouldCancelState = state.OwnsActiveState && isStateEnabled && !isInCombat;
			return new(CountdownAutoState.None, false, countdownStartsManualMode, shouldCancelState);
		}

		return new(CountdownAutoState.None, false, countdownStartsManualMode, false);
	}
}
