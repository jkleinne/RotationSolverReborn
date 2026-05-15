
# [![](https://raw.githubusercontent.com/FFXIV-CombatReborn/RebornAssets/main/IconAssets/RSR_Icon.png)](https://github.com/FFXIV-CombatReborn/RotationSolverReborn)

**Ascended Rotation Solver Reborn, personal fork of RSR**

![Github License](https://img.shields.io/github/license/FFXIV-CombatReborn/RotationSolverReborn.svg?label=License&style=for-the-badge)

This is a personal fork of [FFXIV-CombatReborn/RotationSolverReborn](https://github.com/FFXIV-CombatReborn/RotationSolverReborn). It tracks upstream selectively and keeps a separate package identity, plugin manifest, and Dalamud repository entry for the `ascended-*` plugin set.

The fork focuses on PvP automation quality. Its main additions are `PvPSmart` hostile targeting, PvP burst conservation, Bard control support, and match-aware auto on/off behavior. PvE rotation behavior is intended to stay aligned with upstream.

If you do not specifically want the PvP changes, install upstream RSR instead.

## What this fork adds

A new `TargetingType.PvPSmart` mode that replaces the role-blind `Auto(LowHP)` cycle in PvP with a scoring-based selector. For each candidate hostile, the scorer composes a weighted scalar over pure factors and picks the argmax:

- **Invuln short-circuit:** Guard, Hallowed Ground, Living Dead, Holmgang, Superbolide, and PvP-specific invulnerability states such as Undead Redemption and Hidden are skipped outright
- **Role value:** Healer / Ranged DPS weighted above Melee / Tank
- **Effective HP & finish:** current HP scaled by active mitigation statuses, with a finish-kill bias when a candidate is within burst range
- **Mitigation penalty:** heavy DR cooldowns deprioritize a target during the window they're active
- **Distance penalty:** soft falloff as targets approach the effective range edge
- **Hysteresis:** small sticky bonus for the previous target to prevent GCD-to-GCD oscillation between near-equal candidates
- **Crystal carrier awareness** *(Crystalline Conflict)*: the hostile holding the crystal gains a bonus
- **LB cast awareness:** hostiles mid-cast on a Limit Break gain a bonus (interrupt priority)
- **Isolation factor:** sigmoid bonus the further a hostile is from its nearest ally (catches stragglers)
- **Threat factor:** bonus when a hostile is targeting a low-HP ally or a party healer (peel priority)
- **Burst conservation:** high-impact PvP burst actions are held through unclear windows, blocked during active invulnerability, and spent on valuable, vulnerable, or kill-secure targets
- **Bard support logic:** Warden's Paean prioritizes cleanse, peel, and engage targets, while Repelling Shot and Silent Nocturne check target value before firing
- **PvP state handling:** optional auto on/off behavior follows PvP match start, match end, death, countdown, and duty transition signals

Two preset weight profiles (Casual, Ranked) are bundled, plus a Custom preset for hand-tuned weights. Ranked is the default preset. A toggleable debug overlay renders the full per-target score breakdown in real time for tuning.

The existing `PvPHealers` / `PvPDPS` / `PvPTanks` modes remain as explicit role overrides.

### Testing PvP behavior

The PvP changes are experimental and should be validated in live PvP after updates, especially Bard support decisions and burst conservation.

Burst conservation is enabled by default after installing this fork. To toggle it, open RSR settings and go to `Duty` > `PvP`, then use:

```
Conserve burst in PvP unless the target is valuable, vulnerable, or killable.
```

For the intended Ranked Crystalline Conflict behavior, also make sure `Target` > `Hostile` has `PvPSmart` in the PvP hostile targeting list, preferably first. The same settings area exposes the PvP scoring preset and the debug overlay toggle.

Some high-impact actions still spend before a charge cap or ready timer would otherwise be wasted, but not while the selected target has active invulnerability or effective invulnerability.

This setting may lower raw total damage because it avoids spending burst into Guard, heavy mitigation, tanks, or low-value targets that are unlikely to die. The goal is higher kill conversion, better secure-kill timing, and fewer wasted burst windows rather than higher scoreboard damage. When testing, compare kill participation, burst held for healer or ranged DPS windows, and missed opportunities where the gate felt too conservative.

### Status & caveats

- Scoring weights still need empirical tuning across Ranked CC matches.
- Crystal-carrier `StatusID` is still unverified. The carrier factor evaluates to zero until that status is populated.
- The PvP Limit Break database is populated, but match behavior should still be checked against live casts after game updates.

## Upstream features

Everything below is inherited unchanged from upstream RSR:

- **Dynamic Rotation Guidance (Training Mode):** real-time rotation suggestions tailored to the in-game situation
- **Customizable Settings:** adjust rotations per preference, encounter, and boss mechanics
- **Comprehensive Database:** extensive class ability coverage for accurate rotation
- **User-Friendly Interface:** clean ImGui surface
- **Regular Updates:** upstream tracks game patches and class changes; this fork periodically pulls from it

## Installing

This plugin is distributed through the [ascended-plugins](https://github.com/jkleinne/ascended-plugins) Dalamud repository, which aggregates all `ascended-*` plugin forks under a single URL. Add it to Dalamud once and any future plugin in that namespace becomes available without extra repository entries.

- Open `/xlsettings` in chat and switch to the Experimental tab
- Scroll past DevPlugins to the Custom Plugin Repositories section
- Paste this URL into a free text input:

```
https://raw.githubusercontent.com/jkleinne/ascended-plugins/main/pluginmaster.json
```

- Click `+`, tick the new entry's checkbox, and save
- Reopen Dalamud's plugin installer; "Ascended Rotation Solver Reborn" appears under Available Plugins

**Coexistence with upstream RSR:** this fork uses a distinct `InternalName`, so Dalamud loads it as a separate plugin. It does, however, register the same `/rotation` and `/rsr` chat commands as upstream, so the two cannot run simultaneously without command-registration conflicts. Uninstall upstream RSR before installing this fork.

If you'd rather use the official upstream binary distribution:

```
https://raw.githubusercontent.com/FFXIV-CombatReborn/CombatRebornRepo/main/pluginmaster.json
```

## Contributing

PvP targeting work goes here. Anything else should be contributed upstream:

- For PvP scoring changes (factors, weights, debug overlay): fork this repo, branch from `main`, open a PR against `jkleinne/ascended-rotationsolverreborn:main`
- For everything else (rotations, PvE behavior, core engine): contribute to [upstream RSR](https://github.com/FFXIV-CombatReborn/RotationSolverReborn) instead. Changes there flow into this fork on the next sync

Combat rotation changes should be validated against [Stone, Sky, Sea](https://ffxiv.consolegameswiki.com/wiki/Stone,_Sky,_Sea) per expansion before submission.

## Links

- Upstream rotation definitions: [`RotationSolver/RebornRotations`](https://github.com/FFXIV-CombatReborn/RotationSolverReborn/tree/main/RotationSolver/RebornRotations)
- Upstream Discord: [https://discord.gg/p54TZMPnC9](https://discord.gg/p54TZMPnC9)
