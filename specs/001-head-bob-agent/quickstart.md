# Quickstart: DeskBopper

## Prerequisites

- Windows 10/11 (x64)
- .NET 10 SDK (`dotnet --version` >= 10.0)
- A default audio playback device

## Build & run (from repo root)

```powershell
dotnet build DeskBopper.sln
dotnet run --project src/DeskBopper.App
```

A small character appears on the desktop, always on top.

## Verify the MVP (User Story 1)

1. Play any music (Spotify, YouTube, a local file — anything going to your speakers).
2. Watch the character bob its head in time with the sound.
3. Turn the music up — the bob should get bigger/faster.
4. Pause the music — the character should settle within ~1 second.

Maps to Success Criteria SC-001, SC-002, SC-003.

## Verify idle (User Story 2)

- With nothing playing, the character gently sways/breathes instead of freezing.

## Verify controls & persistence (User Story 3)

- Drag the character to a new spot.
- Right-click (or use the tray icon) → adjust **Sensitivity**, toggle **Start with
  Windows**, or **Quit**.
- Relaunch — the character returns to the saved spot with your settings.

## Publish a standalone exe

```powershell
dotnet publish src/DeskBopper.App -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -o publish
```

Run `publish/DeskBopper.App.exe`.

## Troubleshooting

- **No movement**: confirm audio is actually playing to the *default* device; check the
  tray tooltip for a "no audio device" message.
- **Character blocks clicks**: only the character body is clickable; everything else is
  click-through. Drag from the body.
- **Choppy animation**: ensure a Release build; Debug + debugger attached lowers fps.
