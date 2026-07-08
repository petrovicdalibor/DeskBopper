---
description: "Task list for Head-Bobbing Desktop Music Agent"
---

# Tasks: Head-Bobbing Desktop Music Agent

**Input**: Design documents from `specs/001-head-bob-agent/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md

**Tests**: Optional. Only the pure `Analysis/*` code is unit-testable; UI is verified
manually against Success Criteria. Test tasks below are marked OPTIONAL.

**Organization**: Grouped by user story so each story is an independent increment. US1
alone is a shippable MVP.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- File paths are relative to repo root `C:\Projects\DeskBopper\`

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Create solution and WPF app project: `dotnet new sln -n DeskBopper`; `dotnet new wpf -n DeskBopper.App -o src/DeskBopper.App -f net10.0-windows`; add to sln. Set `<UseWPF>true</UseWPF>`, `<Nullable>enable</Nullable>`, `<Platforms>x64</Platforms>`.
- [ ] T002 Add NAudio package to `src/DeskBopper.App` (`dotnet add package NAudio`).
- [ ] T003 [P] Add `.gitignore` (bin/obj), and add `.claude/` per spec-kit security note. Initialize git repo.
- [ ] T004 [P] Create folder skeleton: `Audio/`, `Analysis/`, `View/`, `Interaction/`, `Settings/` under `src/DeskBopper.App`.

**Checkpoint**: Solution builds and runs an empty WPF window.

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: Blocks all user stories.

- [ ] T005 Define the cross-layer contract `MotionSignal` (readonly record struct) in `src/DeskBopper.App/Analysis/MotionSignal.cs` — fields: `Energy`, `BeatPulse`, `IsSilent`, `TimestampMs`.
- [ ] T006 [P] Implement `EnvelopeFollower` (RMS/peak + attack/decay smoothing, pure, no deps) in `src/DeskBopper.App/Analysis/EnvelopeFollower.cs`.
- [ ] T007 [P] Implement `MotionMapper` (envelope → normalized 0..1 energy, silence flag, optional onset beat pulse; light auto-gain, clamp) in `src/DeskBopper.App/Analysis/MotionMapper.cs`.
- [ ] T008 Define `ILoopbackCapture` abstraction (start/stop, event/callback delivering sample buffers + format) in `src/DeskBopper.App/Audio/ILoopbackCapture.cs`.
- [ ] T009 Configure the transparent, borderless, always-on-top, no-taskbar window shell in `src/DeskBopper.App/MainWindow.xaml` (`WindowStyle=None`, `AllowsTransparency=True`, `Background=Transparent`, `Topmost=True`, `ShowInTaskbar=False`).

**Checkpoint**: Contract + pure analysis + capture seam + transparent window shell exist.

---

## Phase 3: User Story 1 — Bob to music (Priority: P1) 🎯 MVP

**Goal**: Character visibly bobs its head to whatever audio is playing.

**Independent Test**: Play music → head bobs in correlation; pause → settles < 1 s.

### Tests (OPTIONAL) ⚠️

- [ ] T010 [P] [US1] Unit tests for `EnvelopeFollower` and `MotionMapper` (silence detection, monotonic response to louder input, output clamped to 0..1) in `tests/DeskBopper.Tests/AnalysisTests.cs`.

### Implementation

- [ ] T011 [US1] Implement `WasapiLoopbackSource : ILoopbackCapture` wrapping `WasapiLoopbackCapture` in `src/DeskBopper.App/Audio/WasapiLoopbackSource.cs`.
- [ ] T012 [US1] Implement `AudioEngine` in `src/DeskBopper.App/Audio/AudioEngine.cs`: own capture thread, run buffers through `EnvelopeFollower` + `MotionMapper`, publish newest `MotionSignal` via `Interlocked.Exchange` to a single slot.
- [ ] T013 [US1] Build the vector mascot in `src/DeskBopper.App/View/Character.xaml` with a named `Head` group carrying a `TransformGroup` (rotate + translate).
- [ ] T014 [US1] Implement `CharacterAnimator` in `src/DeskBopper.App/View/CharacterAnimator.cs`: subscribe to `CompositionTarget.Rendering`, read latest `MotionSignal`, map `Energy`/`BeatPulse` → head rotation + vertical offset with per-frame smoothing/clamp.
- [ ] T015 [US1] Wire startup in `App.xaml.cs`/`MainWindow.xaml.cs`: create `AudioEngine`, start capture, attach `CharacterAnimator`, size window to character.
- [ ] T016 [US1] Manual verify against SC-001/SC-002/SC-003; tune attack/decay/gain constants for pleasing motion.

**Checkpoint**: MVP complete — deployable, demoable head-bobbing character.

---

## Phase 4: User Story 2 — Idle behavior (Priority: P2)

**Goal**: Gentle looping idle when no audio; smooth transition to/from bobbing.

**Independent Test**: Silence → subtle sway; audio starts → smooth transition to bob.

- [ ] T017 [US2] Add idle/active state machine in `CharacterAnimator` keyed off `MotionSignal.IsSilent` with a ~1 s debounce.
- [ ] T018 [US2] Implement idle motion (slow sine sway/breathe) and cross-fade between idle and active target transforms.
- [ ] T019 [US2] Manual verify: no jarring jump on transition; idle reads as "alive."

**Checkpoint**: US1 + US2 both work.

---

## Phase 5: User Story 3 — Position, control, persistence (Priority: P3)

**Goal**: Drag to move; tray/context menu (sensitivity, autostart, quit); settings persist.

**Independent Test**: Move + change settings + relaunch → restored.

- [ ] T020 [P] [US3] Implement `WindowStyles` interop (`WS_EX_TOOLWINDOW`, layered/transparent click-through toggle) in `src/DeskBopper.App/Interaction/WindowStyles.cs`.
- [ ] T021 [P] [US3] Implement `DragBehavior` (mouse drag on character body moves the window) in `src/DeskBopper.App/Interaction/DragBehavior.cs`.
- [ ] T022 [US3] Implement `UserSettings` load/save JSON at `%APPDATA%\DeskBopper\settings.json` in `src/DeskBopper.App/Settings/UserSettings.cs`.
- [ ] T023 [US3] Add autostart toggle (HKCU `...\Run` key) in `UserSettings`.
- [ ] T024 [US3] Add NAudio-free tray icon via `H.NotifyIcon.Wpf`; build context menu (Sensitivity slider/steps, Start with Windows, Quit) in `src/DeskBopper.App/Interaction/TrayMenu.cs`.
- [ ] T025 [US3] Bind sensitivity to `MotionMapper` gain live; persist/restore position + settings on launch/exit; multi-monitor fallback to primary.
- [ ] T026 [US3] Manual verify against SC-005/SC-006 and US3 acceptance scenarios.

**Checkpoint**: All user stories independently functional.

---

## Phase 6: Polish & Cross-Cutting

- [ ] T027 [P] Handle audio-device absence/change: subscribe to default-device-changed, re-attach capture, tray tooltip on no-device (FR-012, SC-006).
- [ ] T028 [P] Single-instance guard (named mutex) in `App.xaml.cs`.
- [ ] T029 [P] App/tray icon and window title/metadata; per-monitor DPI awareness in app manifest.
- [ ] T030 Verify idle CPU < ~3% (SC-004); confirm no captured audio is persisted/sent (FR-013).
- [ ] T031 [P] `dotnet publish` win-x64 single-file; smoke-test the published exe per quickstart.md.
- [ ] T032 [P] README with screenshot/gif and the quickstart steps.

---

## Dependencies & Execution Order

- **Setup (P1)** → **Foundational (P2)** → user stories.
- **US1 (P1)** depends only on Foundational. **US2 (P2)** builds on `CharacterAnimator`
  from US1. **US3 (P3)** is largely independent (window/interaction/settings) and can be
  built in parallel with US2 after Foundational.
- **Polish (P6)** after desired stories.

### Parallel opportunities

- T003/T004 in Setup.
- T006/T007 (pure analysis) in Foundational.
- T020/T021 within US3.
- Most Polish tasks (T027–T032) are independent.

## Implementation Strategy

1. Setup + Foundational.
2. **US1 → STOP & VALIDATE** (this is the MVP the user asked for).
3. Add US2, then US3, validating each independently.
4. Polish.

## Notes

- [P] = different files, no dependencies.
- Keep `Analysis/*` pure and dependency-free (testable, swappable for FFT later).
- Commit after each task or logical group.
- No captured audio ever leaves memory (Constitution IV / FR-013).
