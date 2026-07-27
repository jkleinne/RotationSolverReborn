using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.DutyState;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using Lumina.Excel.Sheets;
using RotationSolver.ActionTimeline;
using RotationSolver.Basic.Actions.PvPTargetSelection;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;
using RotationSolver.Data;
using RotationSolver.IPC;
//using KamiToolKit;
using RotationSolver.UI;
using RotationSolver.UI.HighlightTeachingMode;
using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;
using RotationSolver.Updaters;
using Player = ECommons.GameHelpers.Player;

namespace RotationSolver;

public sealed class RotationSolverPlugin : IAsyncDalamudPlugin
{
	private readonly WindowSystem windowSystem;

	private static RotationConfigWindow? _rotationConfigWindow;
	private static ControlWindow? _controlWindow;
	private static NextActionWindow? _nextActionWindow;
	private static InterceptedActionWindow? _interceptedActionWindow;
	private static CooldownWindow? _cooldownWindow;
	private static ActionTimelineWindow? _actionTimelineWindow;
	private static OverlayWindow? _overlayWindow;
	//private static NativeControlWindow? _nativeControlWindow;
	private static EasterEggWindow? _easterEggWindow;
	private static FirstStartTutorialWindow? _firstStartTutorialWindow;
	private static PvPSmartDebugOverlay? _pvpSmartDebugOverlay;

	private static readonly List<IDisposable> _dis = [];
	public static string Name => "Ascended Rotation Solver Reborn";
	internal static readonly List<DrawingHighlightHotbarBase> _drawingElements = [];

	public static DalamudLinkPayload OpenLinkPayload { get; private set; } = null!;
	public static DalamudLinkPayload? HideWarningLinkPayload { get; private set; }
	private static readonly Random _random = new();

	internal IPCProvider IPCProvider;
	public RotationSolverPlugin(IDalamudPluginInterface pluginInterface)
	{
		ECommonsMain.Init(pluginInterface, this, ECommons.Module.DalamudReflector, ECommons.Module.ObjectFunctions);
		//KamiToolKitLibrary.Initialize(pluginInterface);
		IconSet.Init();

		_dis.Add(new Service());

		ActionTracer.Init();

		IPCProvider = new();

		_rotationConfigWindow = new();
		_controlWindow = new();
		_nextActionWindow = new();
		_interceptedActionWindow = new();
		_cooldownWindow = new();
		_actionTimelineWindow = new();
		_overlayWindow = new();
		//_nativeControlWindow = new();
		_easterEggWindow = new();
		_firstStartTutorialWindow = new();
		_pvpSmartDebugOverlay = new();

		// Start cactbot bridge if enabled
		//try
		//{
		//    if (Service.Config.EnableCactbotTimeline)
		//    {
		//        var cactbotBridge = new Helpers.CactbotTimelineBridge();
		//        _dis.Add(cactbotBridge);
		//    }
		//}
		//catch (Exception ex)
		//{
		//    PluginLog.Warning($"Failed to start CactbotTimelineBridge: {ex.Message}");
		//}

		windowSystem = new WindowSystem(Name);
		windowSystem.AddWindow(_rotationConfigWindow);
		windowSystem.AddWindow(_controlWindow);
		windowSystem.AddWindow(_nextActionWindow);
		windowSystem.AddWindow(_interceptedActionWindow);
		windowSystem.AddWindow(_cooldownWindow);
		windowSystem.AddWindow(_actionTimelineWindow);
		windowSystem.AddWindow(_overlayWindow);
		windowSystem.AddWindow(_easterEggWindow);
		windowSystem.AddWindow(_firstStartTutorialWindow);
		windowSystem.AddWindow(_pvpSmartDebugOverlay);

		//Notify.Success("Overlay Window was added!");

		Svc.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfigUi;
		Svc.PluginInterface.UiBuilder.OpenMainUi += OnOpenConfigUi;
		Svc.PluginInterface.UiBuilder.Draw += OnDraw;
	}

	public async Task LoadAsync(CancellationToken cancellationToken)
	{
		// Warm up texture cache on framework thread
		await Svc.Framework.Run(() =>
		{
			_ = ThreadLoadImageHandler.TryGetIconTextureWrap(0, true, out _);
		}, cancellationToken);

		// Load main config asynchronously (off main thread)
		try
		{
			if (File.Exists(Svc.PluginInterface.ConfigFile.FullName))
			{
				var json = await File.ReadAllTextAsync(Svc.PluginInterface.ConfigFile.FullName, cancellationToken);
				var oldConfigs = JsonConvert.DeserializeObject<Configs>(json) ?? new Configs();

				var newConfigs = Configs.Migrate(oldConfigs);
				if (newConfigs.Version != Configs.CurrentVersion)
				{
					newConfigs = new Configs();
				}
				Service.Config = newConfigs;
			}
			else
			{
				Service.Config = new Configs();
			}
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to load config: {ex.Message}");
			Service.Config = new Configs();
		}

		// Load OtherConfiguration files
		await OtherConfiguration.InitAsync(cancellationToken);

		// Phase 2: load PvP target-selection databases (embedded JSON, one-time)
		PvPMitigationDatabaseProvider.Initialize();
		PvPLBDatabaseProvider.Initialize();

		// The following must run on the main/framework thread
		await Svc.Framework.Run(() =>
		{
			//HotbarHighlightDrawerManager.Init();

			MajorUpdater.Enable();
			AutoAttackUpdater.Enable();
			Watcher.Enable();
			ActionQueueManager.Enable();
			ActionContextMenu.Init();
			HotbarHighlightManager.Init();

			Svc.DutyState.DutyStarted += DutyState_DutyStarted;
			Svc.DutyState.DutyWiped += DutyState_DutyWiped;
			Svc.DutyState.DutyCompleted += DutyState_DutyCompleted;
			Svc.ClientState.TerritoryChanged += ClientState_TerritoryChanged;
			ClientState_TerritoryChanged(Svc.ClientState.TerritoryType);

			ChangeUITranslation();

			OpenLinkPayload = Svc.Chat.AddChatLinkHandler(0, (guid, seString) =>
			{
				if (guid == 0)
				{
					OpenConfigWindow();
				}
			});
			HideWarningLinkPayload = Svc.Chat.AddChatLinkHandler(1, (guid, seString) =>
			{
				if (guid == 0)
				{
					Service.Config.HideWarning.Value = true;
					Svc.Chat.Print("Warning has been hidden.");
				}
			});
		}, cancellationToken);
	}

	private static void DutyState_DutyCompleted(IDutyStateEventArgs e)
	{
		var delay = TimeSpan.FromSeconds(_random.Next(4, 6));
		_ = Svc.Framework.RunOnTick(() =>
		{
			_ = Service.Config.DutyEnd.AddMacro();

			// In PvP, the duty's natural end (match win/lose) is the canonical "match end" signal.
			// Territory-leave is kept as a fallback for users who exit before completion.
			if (DataCenter.IsPvP && Service.Config.AutoOffPvPMatchEnd && DataCenter.State)
			{
				RSCommands.CancelState();
				return;
			}

			if (Service.Config.AutoOffWhenDutyCompleted)
			{
				RSCommands.CancelState();
			}
		}, delay);
	}

	private static bool? _previousTerritoryIsPvP;
	private const int PvPAutoOnPollIntervalTicks = 30;
	private const int PvPAutoOnMaxPollAttempts = 120;

	private static void ClientState_TerritoryChanged(uint id)
	{
		DataCenter.ResetAllRecords();
		RSCommands.ClearAutoOffTracking();

		if (id == 0)
		{
			PluginLog.Information("Invalid territory id: 0");
			return;
		}

		var territory = Service.GetSheet<TerritoryType>().GetRow(id);
		var newTerritory = new TerritoryInfo(territory);
		var previousIsPvP = _previousTerritoryIsPvP;
		DataCenter.Territory = newTerritory;
		DataCenter.PvPGuardCooldownTracker.Reset();
		_previousTerritoryIsPvP = newTerritory.IsPvP;

		try
		{
			DataCenter.CurrentRotation?.OnTerritoryChanged();
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed on Territory changed: {ex.Message}");
		}

		// PvP -> non-PvP exit: safety net for /leaveduty before DutyCompleted fires.
		if (previousIsPvP == true && newTerritory.IsPvP == false
			&& Service.Config.AutoOffPvPMatchEnd && DataCenter.State)
		{
			RSCommands.CancelState();
		}

		// Entering a PvP territory: arm a poll that fires AutoOn once we're actually bound to the
		// duty and the loading screen has cleared. Dalamud's DutyState.DutyStarted event is driven
		// by a server "Duty Commenced" actor-control packet that is not reliably emitted for CC;
		// its framework fallback only updates the internal IsDutyStarted bool when
		// BoundByDuty && InCombat without raising the event. Mirror the same BoundByDuty signal
		// here so we no longer depend on the packet being delivered.
		if (newTerritory.IsPvP && Service.Config.AutoOnPvPMatchStart)
		{
			SchedulePvPAutoOnAttempt(territoryId: id, attempt: 0);
		}
	}

	private static void SchedulePvPAutoOnAttempt(uint territoryId, int attempt)
	{
		_ = Svc.Framework.RunOnTick(() =>
		{
			if (DataCenter.Territory == null
				|| !DataCenter.Territory.IsPvP
				|| DataCenter.Territory.Id != territoryId)
			{
				return;
			}

			if (DataCenter.State)
			{
				return;
			}

			var loadingScreenActive = Svc.Condition[ConditionFlag.BetweenAreas]
				|| Svc.Condition[ConditionFlag.BetweenAreas51];
			var boundByDuty = Svc.Condition[ConditionFlag.BoundByDuty]
				|| Svc.Condition[ConditionFlag.BoundByDuty56]
				|| Svc.Condition[ConditionFlag.BoundByDuty95];

			if (Player.Available && !loadingScreenActive && boundByDuty)
			{
				RSCommands.DoStateCommandType(StateCommandType.Auto);
				if (!DataCenter.State)
				{
					PluginLog.Warning("[PvP] AutoOn fired but DataCenter.State stayed false. DoStateCommandType returned without flipping state.");
				}
				return;
			}

			if (attempt + 1 < PvPAutoOnMaxPollAttempts)
			{
				SchedulePvPAutoOnAttempt(territoryId, attempt + 1);
			}
			else
			{
				PluginLog.Warning($"[PvP] AutoOn poll timed out after {PvPAutoOnMaxPollAttempts} attempts. "
					+ $"Player.Available={Player.Available} loadingScreenActive={loadingScreenActive} "
					+ $"boundByDuty={boundByDuty} State={DataCenter.State} IsPvP={DataCenter.IsPvP}");
			}
		}, delayTicks: PvPAutoOnPollIntervalTicks);
	}

	private static void DutyState_DutyStarted(IDutyStateEventArgs e)
	{
		if (!Player.Available)
		{
			return;
		}

		// Faster AutoOn path for content where the server emits the "Duty Commenced" actor-control
		// packet. CC frequently doesn't, so the territory-driven poll is the primary trigger and
		// this is a best-effort early fire. Idempotent against the poll via the !State guard.
		if (DataCenter.IsPvP && Service.Config.AutoOnPvPMatchStart && !DataCenter.State)
		{
			RSCommands.DoStateCommandType(StateCommandType.Auto);
		}

		if (!TargetFilter.PlayerJobCategory(JobRole.Tank) && !TargetFilter.PlayerJobCategory(JobRole.Healer))
		{
			return;
		}

		if (DataCenter.Territory?.IsHighEndDuty ?? false)
		{
			var warning = string.Format(UiString.HighEndWarning.GetDescription(), DataCenter.Territory.ContentFinderName);
			BasicWarningHelper.AddSystemWarning(warning);
		}
	}

	private static void DutyState_DutyWiped(IDutyStateEventArgs e)
	{
		if (!Player.Available)
		{
			return;
		}

		DataCenter.ResetAllRecords();
	}

	private void OnDraw()
	{
		if (Svc.GameGui.GameUiHidden)
		{
			return;
		}

		windowSystem.Draw();
	}

	internal static void ChangeUITranslation()
	{
		_rotationConfigWindow!.WindowName = UiString.ConfigWindowHeader.GetDescription()
			+ (typeof(RotationConfigWindow).Assembly.GetName().Version?.ToString() ?? "?.?.?") + "###rsrConfigWindow";

		RSCommands.Disable();
		RSCommands.Enable();
	}

	private void OnOpenConfigUi()
	{
		OpenConfigWindow();
	}

	internal static void OpenConfigWindow()
	{
		_rotationConfigWindow?.Toggle();
	}

	internal static void OpenTicTacToe()
	{
		_easterEggWindow?.IsOpen = true;
	}

	internal static void ShowConfigWindow(RotationConfigWindowTab? tab = null)
	{
		if (_rotationConfigWindow == null)
		{
			return;
		}

		_rotationConfigWindow.IsOpen = true;
		if (tab.HasValue)
		{
			_rotationConfigWindow.SetActiveTab(tab.Value);
		}
	}

	internal static void OpenFirstStartTutorial()
	{
		if (_firstStartTutorialWindow?.IsOpen == true)
		{
			return;
		}

		_firstStartTutorialWindow?.Toggle();
	}

	internal static void UpdateDisplayWindow()
	{
		var isValid = MajorUpdater.IsValid && DataCenter.CurrentRotation != null;

		isValid &= !Service.Config.OnlyShowWithHostileOrInDuty
				|| Svc.Condition[ConditionFlag.BoundByDuty]
				|| AnyHostileTargetWithinDistance(25);

		_controlWindow!.IsOpen = isValid && Service.Config.ShowControlWindow;
		//if (isValid && Service.Config.ShowControlWindow)
		//{
		//	if (!(_nativeControlWindow?.IsOpen ?? false))
		//		_nativeControlWindow?.Open();
		//}
		//else
		//{
		//	_nativeControlWindow?.Close();
		//}
		_cooldownWindow!.IsOpen = isValid && Service.Config.ShowCooldownWindow;
		_nextActionWindow!.IsOpen = isValid && Service.Config.ShowNextActionWindow;
		_interceptedActionWindow!.IsOpen = isValid && Service.Config.ShowInterceptedActionWindow;

		// ActionTimeline window with additional checks
		var showActionTimeline = isValid && Service.Config.ShowActionTimelineWindow;

		if (Service.Config.ActionTimelineOnlyWhenActive)
		{
			showActionTimeline &= DataCenter.IsActivated();
		}

		if (Service.Config.ActionTimelineOnlyInCombat)
		{
			showActionTimeline &= DataCenter.InCombat;
		}

		_actionTimelineWindow!.IsOpen = showActionTimeline;

		if (showActionTimeline)
		{
			ActionTimelineManager.Instance.UpdateCombatState();
		}

		_overlayWindow!.IsOpen = isValid && Service.Config.TeachingMode;
	}

	private static bool AnyHostileTargetWithinDistance(float distance)
	{
		foreach (var target in DataCenter.AllHostileTargets)
		{
			if (target.DistanceToPlayer() < distance)
			{
				return true;
			}
		}
		return false;
	}

	public async ValueTask DisposeAsync()
	{
		ActionTracer.Shutdown();

		Service.Config.Save();
		await OtherConfiguration.Save();

		AutoAttackUpdater.Disable();
		RSCommands.Disable();
		Watcher.Disable();
		ActionQueueManager.Disable();
		ActionContextMenu.Dispose();
		Svc.PluginInterface.UiBuilder.OpenConfigUi -= OnOpenConfigUi;
		Svc.PluginInterface.UiBuilder.Draw -= OnDraw;

		Svc.DutyState.DutyStarted -= DutyState_DutyStarted;
		Svc.DutyState.DutyWiped -= DutyState_DutyWiped;
		Svc.DutyState.DutyCompleted -= DutyState_DutyCompleted;
		Svc.ClientState.TerritoryChanged -= ClientState_TerritoryChanged;

		Svc.Chat.RemoveChatLinkHandler();
		OpenLinkPayload = null!;
		HideWarningLinkPayload = null;

		foreach (var item in _dis)
		{
			item.Dispose();
		}
		_dis.Clear();

		//_nativeControlWindow?.Close();
		//KamiToolKitLibrary.Dispose();
		MajorUpdater.Dispose();
		MiscUpdater.Dispose();
		HotbarHighlightManager.Dispose();
		ActionTimelineManager.Instance.Dispose();

		ECommonsMain.Dispose();
	}
}
