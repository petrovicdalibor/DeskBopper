# DeskBopper Constitution

## Core Principles

### I. Ambient & Unobtrusive
DeskBopper is a desktop toy, not an application that demands attention. It MUST stay
out of the user's way: borderless, always-on-top but click-through by default, no
taskbar clutter beyond a single tray icon, and no window chrome. It never steals
focus, never pops dialogs during normal operation, and consumes negligible resources
while idle. If a feature makes the toy feel like "software you have to manage," it is
out of scope.

### II. Real-Time Responsiveness (NON-NEGOTIABLE)
The whole point is that the character reacts to sound *now*. Perceived audio-to-motion
latency MUST stay under ~120 ms and animation MUST render at 60 fps. Audio capture and
analysis run off the UI thread; only lightweight animation state crosses to the UI
thread. Any work that would stall the render loop is rejected or moved off-thread.

### III. Zero-Config Delight
It works the moment it launches: it captures the default playback device, detects
sound, and bobs — no setup, no driver install, no account. Configuration (sensitivity,
position, autostart) is optional polish layered on top of a working default, never a
prerequisite.

### IV. Privacy by Construction
Audio is analyzed only to derive motion (amplitude/beat envelopes) and is NEVER
recorded, persisted, or transmitted. No network calls are made for core functionality.
Raw audio buffers live only in memory for the duration of analysis and are discarded.

### V. Simplicity / YAGNI
Prefer the smallest thing that produces a charming result. Start with an amplitude
envelope before reaching for FFT beat detection; start with a vector character before
a sprite pipeline. Add complexity only when a concrete, observed shortcoming demands it,
and record the justification in the plan's Complexity Tracking table.

## Technology Constraints

- Platform: Windows 10/11 desktop only.
- Stack: .NET 10, C#, WPF. No cross-platform abstraction layer for v1.
- Audio: WASAPI loopback capture via NAudio (system playback mix), not a microphone.
- No external services, telemetry, or cloud dependencies.
- Character art for v1 is WPF vector geometry (no bundled image assets).

## Development Workflow

- Spec-Driven Development: spec.md -> plan.md -> tasks.md -> implementation, in that order.
- Each user story is an independently shippable increment; US1 alone is a valid MVP.
- Manual verification against Success Criteria is the acceptance gate for v1
  (automated tests are optional and limited to the pure analysis code).
- Any deviation from these principles must be justified in plan.md.

## Governance

This constitution supersedes ad-hoc preferences. Amendments require updating this file
and noting the change in the affected plan. Complexity that violates Principle V must be
justified in the Complexity Tracking section of plan.md or be removed.

**Version**: 1.0.0 | **Ratified**: 2026-07-08 | **Last Amended**: 2026-07-08
