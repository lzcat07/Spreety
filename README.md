# Spreety · v1

Tiny desktop company for Windows. Luffy, Senku, and Rei rest above your taskbar, greet you, wander across visible window tops, and travel with their own effects.

![Control room](docs/control-room.png)

## Run

Extract **Spreety-v1-win-x64.zip** to a normal folder and open **Spreety.exe**. Keep the included `assets` folder beside the executable. Requires Windows 10/11 x64 and .NET Framework 4.8. No Node, npm, administrator rights, or network connection is needed by the app.

The app is an unsigned local prototype. It does not install a service, register startup, modify desktop shortcuts, or send telemetry.

## Controls

- **Show** includes or hides that character.
- **Roam** allows walking and travel. Turn it off to keep that companion at its home perch; it can still idle and greet you.
- **Pause crew** freezes animation; resume uses the same button.
- **Bring everyone home** returns the crew to the primary screen's work-area bottom.
- **Little greetings**, **Explore window tops**, **Hide in fullscreen**, and three sprite sizes are configurable.
- Closing the panel keeps companions running. Double-click the tray icon to reopen it, or right-click for pause, home and quit.

Companion overlays pass mouse input through to the windows underneath. Preferences and error logs live in `%LOCALAPPDATA%\Spreety`.

## Build and test

Run from PowerShell:

```powershell
.\build.ps1
$test = Start-Process .\bin\Spreety.exe '--self-test' -PassThru -Wait
Get-Content .\bin\test-results.txt
.\package.ps1
```

The build uses Windows' .NET Framework C# compiler and WPF assemblies. `--preview` renders the panel into `bin/preview.png`, then exits. `--self-test` writes its result file and exits with code 1 on failure, code 0 on success; its temporary preferences live in `bin/test-settings`, not your real settings.

## Scope and limitations

This is a functioning v1 prototype with two poses and procedural animation, rather than a fully rigged animated character system. It currently explores horizontal window tops and the work-area bottom. Hiding behind shortcuts, vertical wall climbing, a Rei outfit transformation, and exact mixed-DPI multi-monitor placement are future work. Fullscreen detection is based on a foreground window covering a monitor; some borderless apps may trigger it. Perch visibility is sampled once per second; a window moving or closing can briefly leave a companion between updates.

The taskbar is represented by the bottom of each monitor's work area. Auto-hidden taskbars and taskbars on the top/sides need more work. Standard bottom-taskbar placement is the primary target. Companions can overlap each other. The app has no installer, code signature or automatic updates yet.

See [IDEA.md](IDEA.md) for the product vision, [DEVELOPMENT.md](DEVELOPMENT.md) for implementation history, and [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for maintenance notes.
