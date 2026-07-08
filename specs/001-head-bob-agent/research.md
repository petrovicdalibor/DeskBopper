# Phase 0 Research: Head-Bobbing Desktop Music Agent

Decisions and rationale behind the plan. Each entry: Decision / Why / Alternatives.

## R1. UI framework — WPF

- **Decision**: WPF targeting `net10.0-windows`.
- **Why**: Best-in-class support for per-pixel transparent, borderless, always-on-top
  windows and smooth retained-mode animation via `CompositionTarget.Rendering`. Vector
  character is trivial with XAML shapes/paths + `RenderTransform`.
- **Alternatives**: WinUI 3 — transparent/click-through borderless windows are fiddly and
  less documented. WinForms — weaker transparency/animation. Rejected for v1.

## R2. Capturing "music playing right now" — WASAPI loopback

- **Decision**: Capture the default render (playback) device via WASAPI **loopback** using
  NAudio's `WasapiLoopbackCapture`.
- **Why**: Loopback returns the exact mix being sent to the speakers/headphones —
  Spotify, browser, games, everything — with no per-app integration and no microphone.
  This is precisely "music playing in the moment on my PC." Pure .NET, no native install.
- **Alternatives**:
  - Per-app APIs (Spotify Web API, SMTC/GlobalSystemMediaTransportControls) — only give
    metadata/state, not the actual audio waveform, and miss non-app audio. Rejected.
  - Microphone capture — picks up room noise, not the clean digital mix. Rejected.
  - Raw WASAPI via CsWin32/COM — more control but far more code; NAudio wraps it well.

## R3. Beat/energy analysis — energy envelope first (YAGNI)

- **Decision**: v1 computes a short-window RMS (or peak) loudness per audio buffer, feeds
  an attack/decay **envelope follower**, normalizes to 0..1 with light auto-gain, and
  flags silence below a threshold. Optional simple onset "beat pulse" = positive
  derivative of energy over a running average.
- **Why**: Produces convincing "moves to the music" motion with tiny CPU cost and no FFT.
  Directly satisfies SC-001/SC-002. Matches Constitution Principle V.
- **Alternatives**: FFT + per-band energy + tempo tracking (e.g., spectral flux onset
  detection) — better musical accuracy but more code/CPU and tuning. Deferred to a future
  enhancement; the `MotionMapper` seam lets us swap it in without touching the UI.

## R4. Threading & audio→UI handoff

- **Decision**: NAudio raises `DataAvailable` on its own capture thread; we do DSP there
  and publish the newest `MotionSignal` to a single-slot holder via `Interlocked.Exchange`.
  The WPF render loop reads the latest value each frame on the UI thread.
- **Why**: Keeps the UI thread free for 60 fps (Principle II). Animation only needs the
  *latest* value, so newest-wins with no queue avoids lag/backpressure and is lock-free.
- **Alternatives**: `Dispatcher.Invoke` per buffer (UI thread churn), or a bounded queue
  (adds latency). Rejected.

## R5. Transparent, click-through, tray-only window

- **Decision**: `WindowStyle=None`, `AllowsTransparency=True`, `Background=Transparent`,
  `Topmost=True`, `ShowInTaskbar=False`. Add extended styles `WS_EX_TOOLWINDOW` (no
  alt-tab), and toggle `WS_EX_TRANSPARENT`/`WS_EX_LAYERED` for click-through over
  non-character pixels via a small P/Invoke to `SetWindowLong`.
- **Why**: Satisfies FR-005 (ambient, no chrome, on top). Click-through everywhere except
  the character means it won't block the desktop; dragging is enabled on the body.
- **Alternatives**: Full click-through always (can't drag) or never (blocks clicks around
  the character). Hybrid chosen.

## R6. Idle vs. bob state (US2)

- **Decision**: A simple state machine: `Idle` when `IsSilent` for > ~1 s, else `Active`.
  Idle drives a slow sine sway; Active maps `Energy`/`BeatPulse` to head rotation +
  vertical offset. Cross-fade the target transform so transitions aren't jarring.
- **Why**: Meets FR-008 and the "stands around" feel with minimal logic.

## R7. Persistence & autostart (US3)

- **Decision**: JSON file at `%APPDATA%\DeskBopper\settings.json` (position, sensitivity,
  autostart). Autostart via a `Run` key under
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
- **Why**: Per-user, no admin rights, standard Windows autostart mechanism; JSON is
  trivial with `System.Text.Json`.
- **Alternatives**: Startup-folder shortcut (works, but registry key is simpler to toggle
  programmatically); registry `.NET` settings (opaque). JSON chosen for transparency.

## R8. Packaging / distribution

- **Decision**: v1 ships as a framework-dependent or self-contained single-file `dotnet
  publish` for win-x64. No installer required to run.
- **Why**: Simplicity; the user just runs the exe. Autostart handled in-app (R7).
- **Alternatives**: MSIX packaging — nicer install/update story but heavier; deferred.

## Open Questions / Deferred

- FFT-based multi-band reactions (e.g., "nods on kick, sways on bass") — future.
- Multiple characters / character picker — out of scope.
- Sprite/image characters and per-song artwork — out of scope for v1 (vector only).
