# Maintenance map

All runtime code is in `src/Spreety.cs` to keep the first release easy to compile with the Windows compiler.

| Component | Responsibility |
| --- | --- |
| Program | Single-instance mutex, startup, preview/test entry points, exception reporting |
| Preferences | JSON settings, schema sanity checks, clamping, atomic file replacement, recovery |
| Art | Cached WPF bitmap loading, alpha-bounds cropping and atlas cells |
| Native | Win32 / DWM declarations |
| Engine | 30 Hz animation timer, 1 Hz surface refresh, monitor/work-area coordinates, foreground fullscreen detection, tray lifetime |
| Buddy | Independent activity state, home position, weighted decisions and travel interpolation |
| BuddyVisual | Transparent per-character drawing, two poses, greetings and travel effects |
| ControlRoom | Native WPF control panel |
| Tests | Offline settings, image and state-transition regression checks |

## Runtime choices

WPF transparent tool windows allow alpha sprites without a full-screen window. WS_EX_TRANSPARENT and WS_EX_NOACTIVATE keep mouse clicks and focus with the underlying app. Each companion has its own small window; effects are clipped to that local window. No global input hook is used.

Window-top detection uses EnumWindows, excludes the app's own windows, shell windows, tool windows, minimized windows and DWM-cloaked windows. Visible bounds use DwmGetWindowAttribute(DWMWA_EXTENDED_FRAME_BOUNDS), falling back to GetWindowRect. WindowFromPoint samples top-edge segments to reject occluded perches. Perches are identified by window handle and segment index, allowing repositioning on refresh. When an active perch disappears, the companion returns home.

References: [GetWindowRect](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect), [DWM attributes](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute).

Activity decisions favor sitting. A travel decision selects a home/taskbar surface 78% of the time when eligible window surfaces exist. This is a destination probability, not a precise measurement of time spent on the taskbar. Most activity cycles last 5–20 seconds; walking and greetings are shorter. Paused time is not accumulated because the engine updates its clock on every tick.

## Artwork

`assets/characters.png` is the final built-in imagegen output with alpha transparency. It is 1536×1024, with three columns, standing poses above and sitting poses below. The row split is at 55% of image height because the generated standing figures extend past the midpoint. Senku's column has a 40-pixel left inset to reject a disconnected sliver of Luffy's hat. Alpha-bounds cropping removes cell padding at load time. Any replacement atlas must preserve this layout or update Art.Get. The unmodified generated atlas is stored in Git.

## Known technical debt

- Native monitor bounds are mapped using the panel's current device-to-DIP transform. Uniform-DPI screens are the target; mixed DPI needs per-monitor transforms and DPI-change handling.
- UI previews currently use an older FormattedText constructor, producing one build warning. No compile errors.
- Surface refresh currently runs on the UI thread; profile on systems with many open windows before adding more companions.
- State transitions and rendering share the same assembly. Split models and introduce a deterministic clock/random source before expanding the simulation.
- Tests cover local state and settings, not every native edge case. See the manual QA checklist.
