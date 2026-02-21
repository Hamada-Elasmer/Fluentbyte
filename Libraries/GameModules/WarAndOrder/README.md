# WarAndOrder Game Module (GameModules/WarAndOrder)

This project contains **everything that is specific to the game** "War and Order".

## Design Goals
- Core depends ONLY on `GameContracts` (interfaces + DTOs).
- WarAndOrder module implements the `IGameModule` contract.
- No UI code here. UI belongs to App project.
- Use adapters (ports) for ADB / emulator so you can plug in `AdbLib` and `EmulatorLib` cleanly.

## What you implement here
- Lifecycle: Install / Launch / WaitReady / Shutdown
- Detection: IsInstalled / IsMainScreenReady / IsTutorialCompleted / HasBlockingDialogs
- Tasks: any game tasks (collect resources, upgrade building, etc.)
- HealthChecks: optional self-healing checks (ADB connection, package presence, etc.)

> NOTE: Most methods are scaffolded with clear TODOs so you can connect your real ADB/emulator logic later.