# Rec Screen

A tiny, one-click screen recorder for Windows that lives on your taskbar.

- **Click** the pinned icon → recording starts immediately (a red dot appears on the icon).
- **Click again** → recording stops, the MP4 is saved and the app closes.
- **Right-click → Settings** → choose the monitor, audio input (system audio, microphone, or both) and the output folder.

Your choices are remembered for every following recording.

## Features

- Records any monitor to MP4 (H.264 + AAC)
- System audio and/or microphone, mixed into one track
- Shortcut to the Windows Sound panel to manage devices
- Files named by date and time, e.g. `2026-11-11_14-30-05.mp4`
- No tray icon, no background service: it only runs while recording

## Requirements

- Windows 10/11 (x64)
- [.NET 10 SDK](https://dotnet.microsoft.com/download) to build (the .NET 10 Desktop Runtime is enough to run)

## Build

```bash
dotnet publish -c Release -o dist
```

## Pin to the taskbar

Windows does not allow apps to pin themselves, so do it once by hand:
right-click `dist\RecScreen.exe` → **Pin to taskbar** (on Windows 11 it may be under *Show more options*).

## Where things are stored

- Recordings: `Videos\Rec Screen` by default
- Settings: `%APPDATA%\RecScreen\settings.json`

## Built with

- WPF on .NET 10
- [ScreenRecorderLib](https://github.com/sskodje/ScreenRecorderLib) for capture and encoding
