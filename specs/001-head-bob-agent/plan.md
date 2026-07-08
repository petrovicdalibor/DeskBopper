# Implementation Plan: Head-Bobbing Desktop Music Agent

**Branch**: `001-head-bob-agent` | **Date**: 2026-07-08 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-head-bob-agent/spec.md`

## Summary

Build a Windows desktop toy: a small WPF vector character in a borderless,
transparent, always-on-top window that bobs its head in real time to whatever audio is
playing on the PC. Audio is captured from the default playback device via WASAPI
loopback (NAudio), reduced on a background thread to a smoothed 0..1 energy envelope
(the "Motion Signal"), and consumed by the WPF render loop (`CompositionTarget.Rendering`)
to drive a head transform. Idle animation, dragging, a tray menu, and persisted settings
layer on as P2/P3 increments.

## Technical Context

**Language/Version**: C# 13 on .NET 10

**Primary Dependencies**: WPF (`net10.0-windows`, `UseWPF=true`); NAudio (WASAPI loopback
capture); H.NotifyIcon.Wpf (tray icon) — introduced only for US3.

**Storage**: A single JSON settings file under `%APPDATA%\DeskBopper\settings.json`
(introduced in US3). No database.

**Testing**: Optional xUnit unit tests for the pure analysis/DSP code (envelope,
smoothing, normalization). UI verified manually against Success Criteria.

**Target Platform**: Windows 10/11 desktop, x64.

**Project Type**: Single desktop application (WPF), optionally with a sibling test
project and a pure-logic class library.

**Performance Goals**: 60 fps render loop; audio->motion latency < 120 ms; motion
signal updated >= 30 Hz; idle CPU < ~3% of one core.

**Constraints**: Audio analysis strictly off the UI thread; no network; captured audio
never persisted; motion clamped/smoothed so no seizure-like jitter.

**Scale/Scope**: Single user, single instance, one on-screen character.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **I. Ambient & Unobtrusive** — PASS. Borderless transparent always-on-top window,
  `ShowInTaskbar=false`, tray-only presence, click-through except on the character body.
- **II. Real-Time Responsiveness** — PASS. Capture + DSP on NAudio's background thread;
  UI thread only reads the latest smoothed value each frame. No blocking calls on UI.
- **III. Zero-Config Delight** — PASS. On launch it auto-binds the default render device
  and starts; settings are optional (US3).
- **IV. Privacy by Construction** — PASS. Loopback buffers consumed in-place to compute a
  scalar; nothing written or sent. No `System.Net` usage in core.
- **V. Simplicity / YAGNI** — PASS for v1. Start with RMS/peak energy envelope (no FFT).
  FFT/tempo tracking deferred; see Complexity Tracking (empty — no violations).

Re-check after design: no new violations introduced. Tray + settings (US3) add one
dependency each, justified by their user stories, not core.

## Project Structure

### Documentation (this feature)

```text
specs/001-head-bob-agent/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 decisions & rationale
├── quickstart.md        # How to build/run/verify
└── tasks.md             # Phase 2 task breakdown (/speckit-tasks)
```

### Source Code (repository root)

```text
DeskBopper.sln
src/
└── DeskBopper.App/                 # WPF application (net10.0-windows)
    ├── DeskBopper.App.csproj
    ├── App.xaml / App.xaml.cs      # startup, single-instance, tray wiring
    ├── MainWindow.xaml / .cs       # transparent click-through character window
    ├── Audio/
    │   ├── ILoopbackCapture.cs     # abstraction over capture (testable seam)
    │   ├── WasapiLoopbackSource.cs # NAudio WASAPI loopback + device-change handling
    │   └── AudioEngine.cs          # owns capture thread, emits MotionSignal
    ├── Analysis/
    │   ├── EnvelopeFollower.cs     # RMS/peak -> attack/decay smoothing (PURE)
    │   ├── MotionMapper.cs         # envelope -> normalized 0..1 + beat pulse (PURE)
    │   └── MotionSignal.cs         # struct: Energy, BeatPulse, IsSilent, Timestamp
    ├── View/
    │   ├── Character.xaml          # vector mascot; named "Head" group for transform
    │   └── CharacterAnimator.cs    # per-frame transform from MotionSignal (idle+bob)
    ├── Interaction/
    │   ├── WindowStyles.cs         # WS_EX_LAYERED/TRANSPARENT/TOOLWINDOW interop
    │   ├── DragBehavior.cs         # drag-to-move (US3)
    │   └── TrayMenu.cs             # tray icon + context menu (US3)
    └── Settings/
        └── UserSettings.cs         # load/save JSON, autostart registry (US3)

src/DeskBopper.Core/                # OPTIONAL pure-logic lib (extract if tests added)
tests/
└── DeskBopper.Tests/               # OPTIONAL xUnit tests for Analysis/* (pure)
```

**Structure Decision**: Single WPF app project is the default and sufficient for v1.
`Analysis/*` is written as pure, dependency-free C# so it can be unit-tested and, if
desired, hoisted into `DeskBopper.Core`. The `Audio` layer hides NAudio behind
`ILoopbackCapture` so the UI and analysis never reference the capture library directly.

## Data / Contract Notes

The single cross-layer contract is **`MotionSignal`**:

```csharp
public readonly record struct MotionSignal(
    float Energy,     // 0..1 smoothed loudness driving bob magnitude
    float BeatPulse,  // 0..1 short-lived spike on detected onset (optional in v1)
    bool  IsSilent,   // true when below silence threshold -> idle
    long  TimestampMs // capture time for latency/debug
);
```

`AudioEngine` computes it on the capture thread and publishes the latest value to a
lock-free single-slot holder (`volatile` reference / `Interlocked.Exchange`). The render
loop reads the latest each frame — no queue, no backpressure, newest-wins (a dropped
intermediate value is irrelevant for animation).

## Complexity Tracking

> No constitutional violations. Table intentionally empty.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| (none)    | —          | —                                    |
