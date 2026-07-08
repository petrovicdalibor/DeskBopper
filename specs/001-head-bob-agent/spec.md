# Feature Specification: Head-Bobbing Desktop Music Agent

**Feature Branch**: `001-head-bob-agent`

**Created**: 2026-07-08

**Status**: Draft

**Input**: User description: "A .NET desktop agent that just stands around and bobs its head to music playing in the moment on my PC."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Character bobs its head to currently playing music (Priority: P1)

The user launches DeskBopper and a small character appears on the desktop. Whenever
music (or any audio) is playing through the PC's speakers/headphones, the character
bobs its head roughly in time with the sound — bigger, faster motion for louder,
punchier music; gentler motion for quiet passages. This is the entire reason the app
exists.

**Why this priority**: This is the core promise. Without it there is no product. It is
also a complete, demonstrable MVP on its own.

**Independent Test**: Launch the app, play a song, and observe the character's head
moving in visible correlation with the music; pause the song and observe the motion
subside. No other feature needs to exist for this to be valuable.

**Acceptance Scenarios**:

1. **Given** the app is running and no audio is playing, **When** the user starts a
   music track, **Then** the character begins bobbing its head within ~150 ms of sound
   starting.
2. **Given** music is playing, **When** the track gets louder/more energetic, **Then**
   the head-bob becomes visibly larger and/or faster.
3. **Given** the character is bobbing, **When** the user pauses/stops the audio, **Then**
   the character stops bobbing and settles within ~1 second.
4. **Given** the app is running, **When** audio plays, **Then** the animation remains
   smooth (no visible stutter) and the character stays on top of other windows.

---

### User Story 2 - Gentle idle behavior when nothing is playing (Priority: P2)

When no audio is detected, the character does not freeze into a lifeless statue. It
performs a subtle idle animation (slow breathing/sway) so it always feels "alive" and
ready.

**Why this priority**: Turns a functional gimmick into a charming companion. Not
required for the core demo, but greatly improves the "stands around" feel the user
asked for.

**Independent Test**: With no audio playing, observe the character gently idling rather
than being perfectly still; confirm it transitions smoothly into bobbing when audio
starts.

**Acceptance Scenarios**:

1. **Given** no audio has played for >1 second, **When** the user watches the character,
   **Then** it shows a subtle looping idle motion.
2. **Given** the character is idling, **When** audio starts, **Then** it transitions to
   bobbing without a jarring jump.

---

### User Story 3 - Position, control, and persistence (Priority: P3)

The user can move the character anywhere on screen by dragging it, access basic actions
(sensitivity adjust, start-with-Windows toggle, quit) from a right-click / tray menu,
and have their chosen position and settings remembered next launch.

**Why this priority**: Quality-of-life. Makes it usable day-to-day rather than a
run-once novelty. Depends on nothing in US1/US2 conceptually but layers onto them.

**Independent Test**: Drag the character to a new spot, change sensitivity, enable
autostart, quit, relaunch — confirm the character returns to the saved spot with saved
settings.

**Acceptance Scenarios**:

1. **Given** the app is running, **When** the user drags the character, **Then** it
   follows the cursor and stays where released.
2. **Given** the tray/context menu is open, **When** the user adjusts sensitivity,
   **Then** the bob responsiveness changes accordingly and live.
3. **Given** settings were changed, **When** the user quits and relaunches, **Then**
   position, sensitivity, and autostart preference are restored.
4. **Given** autostart is enabled, **When** the user logs into Windows, **Then**
   DeskBopper launches automatically.

---

### Edge Cases

- **No/blocked audio device**: If the default playback device is unavailable or the
  user has no output device, the app runs and idles gracefully, showing a tray tooltip
  explaining that no audio device was found rather than crashing.
- **Playback device changes** (headphones plugged in, default device switched): the app
  re-attaches to the new default device without a restart.
- **Silent-but-playing audio** (a track at zero volume, or system muted): treated as "no
  audio" — the character idles.
- **Sudden loud transient** (notification ping, game explosion): the bob reacts but is
  clamped so the head never flies off-model; motion is smoothed to avoid seizure-like
  jitter.
- **Multi-monitor / DPI changes**: the character stays on a valid monitor and scales
  correctly per-monitor DPI; a saved position on a now-disconnected monitor falls back
  to the primary monitor.
- **Very quiet music**: sensitivity/auto-gain ensures gentle music still produces
  visible (if small) motion.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST capture the audio currently being played through the system's
  default playback device (a loopback of the output mix), without requiring the user to
  select a source.
- **FR-002**: System MUST derive a real-time motion signal from the captured audio
  (loudness envelope, and optionally beat emphasis) updated frequently enough to feel
  live (target >= 30 updates/sec).
- **FR-003**: System MUST animate a character's head-bob whose magnitude and/or rate is
  driven by the motion signal.
- **FR-004**: System MUST distinguish "audio playing" from "silence" and stop bobbing
  when audio is effectively silent.
- **FR-005**: System MUST render the character as a borderless, transparent,
  always-on-top window with no taskbar button (tray presence only).
- **FR-006**: System MUST keep audio processing off the UI thread so animation stays at
  ~60 fps.
- **FR-007**: System MUST clamp and smooth motion so output never exceeds a natural
  range regardless of input spikes.
- **FR-008**: System MUST show a gentle idle animation when no audio is detected. *(US2)*
- **FR-009**: Users MUST be able to reposition the character by dragging it. *(US3)*
- **FR-010**: Users MUST be able to adjust bob sensitivity, toggle start-with-Windows,
  and quit from a context/tray menu. *(US3)*
- **FR-011**: System MUST persist and restore character position and user settings
  across launches. *(US3)*
- **FR-012**: System MUST handle audio-device absence/change without crashing (see Edge
  Cases).
- **FR-013**: System MUST NOT record, persist, or transmit captured audio; buffers are
  used transiently for analysis only.

### Key Entities

- **Audio Frame**: A short buffer of playback samples captured from the loopback device.
  Transient; consumed by analysis and discarded.
- **Motion Signal**: The derived, smoothed, normalized value(s) (e.g., current energy
  0..1, optional beat pulse) that drive animation. The single contract between the audio
  layer and the UI layer.
- **Character State**: Which behavior is active (idle vs. bobbing) plus current animated
  transform values.
- **User Settings**: Persisted preferences — screen position, sensitivity, autostart
  flag.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With music playing, an observer agrees the character is "moving to the
  music" — head motion visibly correlates with the beat/energy of the track.
- **SC-002**: Time from audio starting to first visible bob is under 150 ms; time from
  audio stopping to motion settling is under 1 second.
- **SC-003**: Animation holds ~60 fps with no perceptible stutter while analyzing audio.
- **SC-004**: Idle CPU usage is low enough to be unnoticeable on a typical PC (target
  < ~3% of one core while idling; modest while active).
- **SC-005**: A first-time user gets a bobbing character with zero configuration steps
  beyond launching the app.
- **SC-006**: The app survives unplugging/switching the audio device and muting without
  crashing or requiring a restart.

## Assumptions

- Target environment is Windows 10/11 with .NET 10 available; single primary user.
- "Music playing on my PC" means audio going to the default playback device — captured
  via WASAPI loopback — not a specific music app's API. This captures Spotify, YouTube,
  games, anything.
- v1 character is a single WPF vector mascot; no per-song artwork or lip-sync.
- Beat detection can start as a simple energy envelope; frequency-band/tempo analysis is
  a later enhancement, not a v1 requirement.
- Automated UI testing is out of scope for v1; verification is manual against Success
  Criteria, with optional unit tests around the pure analysis math.
- Single-instance app; running a second copy just focuses/no-ops rather than stacking.
