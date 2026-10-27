# Development log

## 2026-10-27 · DEV Version 1.0.7--->Release Version 1.0 · Permanent Fullscreen Surface, Window Occlusion Clipping, Taskbar Dangling Legs & Standing Landing

### Scope and updates
- Taskbar visible dangling legs (Pic 1):
  - With permanent screen-sized topmost windows, sitting companions render their full dangling legs (from pelvis downward) directly over the top of the taskbar surface, matching window edge seating.
- Elimination of flight clipping curtains and buffering flicker (Pics 3 & 4):
  - Converted `Buddy` windows to permanent virtual-screen-sized transparent viewports.
  - Eliminated runtime window resizing and `SetWindowPos` geometry changes at flight start/finish.
  - Eradicated 1-frame backbuffer flashing, split-second teleport glitches, and offscreen coordinate clipping.
  - Rubber arm stretches anywhere across screens without boundary truncation.
- True window occlusion when new windows hover over companions (Pic 4):
  - When perched on an edge, queried `Native.WindowFromPoint` at companion's body coordinate.
  - If an overlapping window covers the perch, applied `GeometryCombineMode.Exclude` clipping to render the companion naturally behind the hovering window.
- Natural standing arrival pose (Item 5):
  - Companions land firmly on their feet in standing stance (`Frame = 0`) upon flight arrival.
  - Companions pause for ~1.5–2.5 seconds before naturally deciding whether to stroll, sit, or wave.
- Built clean binaries, verified automated self-tests pass, and updated release packages.

## 2026-10-27 · DEV Version 1.0.6 · Proportionate 2.5D Rubber Arm, Elimination of Invisible Clipping Curtains & Flight Teleport Fixes

### Scope and updates
- Fixed invisible curtain and taskbar vanishing bugs (Images 2 & 3):
  - Removed legacy `Activity.Peek` state and its artificial 175-pixel viewport clip (`PushClip(Rect(0, 0, 220, 175))`).
  - Companions no longer sink 72% behind perch baselines or get sliced in half on desktop wallpapers.
  - Companions always stay fully rendered and firmly perched directly ON TOP of the taskbar and window edges.
- Fixed random teleportation between window tops and taskbar:
  - Fixed root cause in `Buddy.Step`: during flight, destination coordinates `ToX` and `ToY` were accidentally overwritten by the departure perch (`current.Y`) instead of tracking `TargetPerch`.
  - Replaced abrupt `Home()` snapping when a window disappears with smooth flight down to the taskbar.
  - Guaranteed continuous, smooth spatial interpolation from launch to landing without coordinate snaps.
- Redesigned Luffy's Gum-Gum stretch animation (Image 1):
  - Fixed arm origin: anchored shoulder at `y + h * 0.56` (red vest chest/shoulder level), completely fixing the "snake from mouth" bug.
  - Scaled arm thickness down from 7.0px to a slim, proportionate 2.0–2.8px matching Luffy's 2.5D pixel art body.
  - Phase 1 (stretch to grab): arm shoots straight to target edge with zero wobble and grabs the edge firmly.
  - Phase 2 (slingshot pull): arm contracts as Luffy flies toward the edge, with a subtle 3px rubber bounce when short.
  - Rendered a neat 4×4 pixel grabbing fist at the target edge.
- Verified all unit checks pass with `--self-test`, repackaged distribution zips, and launched live instance.

## 2026-10-27 · DEV Version 1.0.5 · Natural Flight Mechanics, Rubber Arm Slingshot, Dynamic Taskbar Tracking & Window Relocation Escape

### Scope and updates
- Slower natural flight speeds and directional anticipation:
  - Added directional anticipation (`Activity.WindUp`): all companions face their flight destination first and wait ~500ms before taking off.
  - Slowed flight durations to observable, natural speeds (3.0s for Luffy, 3.4s for Senku, 3.6s for Rei).
- Authentic Luffy Gum-Gum Rubber Arm & Slingshot Physics:
  - Implemented 2-phase travel mechanics: Luffy stays anchored at his launch point during the initial 35% of travel while his rubber arm stretches across the screen to the target edge and firmly clamps onto it.
  - Dynamically sized the travel window bounds (`minX - 150` to `maxX + 150`, etc.) during flight so Luffy's arm and companion flight paths never get clipped by the standard 220×210 window box.
  - Simulated real rubber physics with traveling sine wave oscillation (`Math.Sin(s * π * 4 - elapsed * 26)`) and tension amplitude modulation, producing a juicy wobbly arm ripple with grabbing fist/knuckle clamping and tension sparkles.
  - Once the target edge is grabbed, Luffy's body slingshots forward to the destination, contracting the wobbly arm until landing firmly on the edge.
  - Applied the same stretch-and-grab mechanic when Luffy travels between random positions on the taskbar or along window edges.
- Dynamic taskbar auto-hide tracking:
  - Added per-tick taskbar monitoring (`UpdateTaskbarPerches`): continually reads `Shell_TrayWnd` window bounds every 33ms.
  - Companions sitting, walking, waving, or peeking on the taskbar slide down smoothly as the taskbar hides, and ride back up in real time when the user hovers near the screen edge.
- Window-to-window movement & window relocation escape:
  - Enhanced perch selection (`Engine.Pick(current)`): companions now frequently move between different open window tops randomly.
  - Added window drag/move detection (`Native.Bounds` tracking on perches): moving a window triggers an immediate escape flight for perched companions.
  - If multiple windows are open, companions fly to another window edge (Rei with EVA wings, Senku with jetpack thruster sparks, Luffy with Gum-Gum stretch).
  - If only one window is open, companions fly safely down to the taskbar.
  - Relocated windows remain eligible in future roaming perches so companions can return whenever they want.
- Expanded `--self-test`:
  - Verified windup anticipation states, continuous stationary-leg flight poses, dynamic taskbar tracking up/down, multi-window escape, single-window escape to taskbar, and Luffy's stretch-versus-slingshot body anchoring.
- Built clean binaries, verified self-tests pass, updated release packages, and verified clean packaging.

## 2026-10-27 · DEV Version 1.0.4 · Firm Ground Contact & Natural Sitting Surface Alignment

### Scope and updates
- Fixed sitting pose floating gap on window title bars and taskbar:
  - Discovered root cause: sprite atlas sitting frames include dangling legs below the pelvis (`cy = 208` for Luffy, `214` for Senku, `213` for Rei). Previously, `y = 175 - h` placed the dangling feet tip at 175, leaving the character's buttocks and palms floating 8–10 DIP pixels in mid-air above the window frame.
  - Implemented character-specific seating pelvic baseline alignment (`Art.GetSittingTorsoHeight`): the companion's buttocks and hands now firmly sit directly ON the solid surface path (`baselineY = 175 + contactSink`), while their dangling legs hang naturally downward in front of the window title bar or taskbar.
  - Updated perch clipping: peeking (`Activity.Peek`) cleanly clips at 175 so characters hide behind the edge, while sitting and walking allow full rendering of dangling legs and grounded soles down to the window border without truncation.
- Fixed walking and standing surface floating gap:
  - Eliminated upward jump bobbing during knee bend frames (previously `bob = 1.5` lifted the companion into the air during passing steps); replaced with a subtle downward step compression (0.5px dip).
  - Anchored all standing and walking cycles for Senku and Rei to consistent ground baselines (`247` for Senku, `241` for Rei, `255` for Luffy), preventing vertical jitter and body drops during knee bends.
  - Added 1.5 DIP solid ground contact sink (`baselineY = 176.5`) to eliminate subpixel rounding daylight on high-DPI displays.
  - Added primary taskbar edge detection via `Shell_TrayWnd` to ensure desktop perches align perfectly to the true taskbar top edge even with custom taskbar auto-hide or DPI settings.
- Expanded automated `--self-test` to verify ground baselines, uniform standing heights across all walk cycle frames, and pelvic seating alignment for all 3 companions.
- Built clean binaries, verified all unit tests pass, repackaged root and `dist/Spreety-v1-win-x64.zip`, and launched the updated executable.

## 2026-10-27 · DEV Version 1.0.3 · Balanced Stride Cycles & Static Flight Poses

### Scope and updates
- Corrected Senku and Rei's walking strides: eliminated uneven half-strides by expanding both forward contact steps to equal, matching stride extensions (matching Luffy's balanced stride dynamics).
- Fixed mid-air flight animation: characters now maintain a steady, static aerodynamic flight pose while flying (`Activity.Travel`), stopping the walking leg animation and leg-cycling during flight.
- Updated automated `--self-test` to verify that companions maintain stationary legs during travel.
- Rebuilt binaries, updated release packages, and launched latest Spreety build.

## 2026-10-27 ·DEV  Version 1.0.2 · Lifelike Knee-Bending Walk Cycles & Correct Walking Direction

### Scope and updates
- Fixed walking direction inversion: characters naturally face right (+1) and are mirrored horizontally (`ScaleTransform(-1, 1)`) when walking or traveling left (-1), ensuring sprites always face forward in their direction of movement and immediately turn around when changing direction.
- Engineered true 4-step walk cycles with authentic knee-bending passing frames for Luffy, Senku, and Rei: `Step A (contact) -> Knee Bend A (lift & pass) -> Step B (contact) -> Knee Bend B (lift & pass)`.
- Preserved the user's beloved 2.5D pixel art character designs with pixel-perfect fidelity across all frames and poses.
- Expanded sprite atlas to 6 columns × 3 rows (1440×768), standardizing cells to 240×256 pixels centered around x = 120 with symmetrical bounding box cropping to eliminate torso/head jitter.
- Added natural vertical bobbing on passing frames (1.5px dip during weight shift and knee flexion).
- Moved Senku's jetpack rendering inside the character transform group so it stays correctly anchored on his back when facing either direction.
- Updated `--self-test` to validate all 18 frames (3 companions × 6 poses) across transparency, easing, walking, and settings persistence.
- Re-rendered control room preview and updated release package `Spreety-v1-win-x64.zip`.

## 2026-10-27 · DEV Version 1.0.1 · 2.5D Pixel Redesign & True Walk Cycles

### Scope and updates
- Maintained standalone `Spreety.exe` directly in the root repository folder outside the zip for instant double-click execution, keeping `Spreety-v1-win-x64.zip` in root and `dist/` for final distribution.
- Replaced the sharp vinyl 3D figures with authentic 2.5D isometric pixel art for Luffy, Senku, and Rei.
- Replaced the previous single-pose procedural wobbling with true multi-frame walking cycles (contact left, passing, contact right, passing).
- Added horizontal directional flipping (`ScaleTransform(-1, 1)`) so characters realistically face left or right depending on their walking or travel direction.
- Scaled down sprite sizes to compact, desktop-friendly dimensions: Tiny (36px), Cozy (48px, default), Classic (64px).
- Enabled `BitmapScalingMode.NearestNeighbor` and `EdgeMode.Aliased` in WPF to ensure pixel art remains razor-sharp without blur.
- Resolved obsolete `FormattedText` constructor warning with PixelsPerDip DPI overload.
- Updated automated `--self-test` to validate all 15 frames (3 characters × 5 poses: idle stand, walk 1, walk pass, walk 2, seated).
- Re-rendered Control Room preview screenshot and repackaged distribution zip.

## 2026-10-27 · DEV Version 1.0.0

### Scope and setup

- Recorded the original request separately in IDEA.md, including future behavior that is not yet implemented.
- Chose native C# / WPF and .NET Framework to build and run on the available Windows installation without downloading a framework or shipping Electron.
- Added build.ps1, package.ps1, x64 manifest, Git ignores, maintenance notes and QA checklist.

### Artwork

- Used the imagegen skill and built-in generation to produce Luffy, Senku and Rei in standing and seated poses.
- Requested an extraction/layout edit after inspecting the first atlas.
- Verified final alpha pixels through the app's image decoder. RGB previews retain background color data, but transparent areas render correctly in the app.
- Adapted atlas loading to its actual 55% row split so standing figures retain their feet. Added a 40-pixel inset on Senku's column to discard a disconnected hat sliver.
- Saved final artwork and exact prompts in the project.

### App implementation

- Added native control room with independent Show and Roam switches, size, greetings, window exploration, fullscreen hiding, pause, home and quit.
- Added individual click-through transparent windows for the companions and a shared 30 Hz timer.
- Added sitting, walking/bobbing, greeting, edge-peeking and travel states; each character makes independent decisions.
- Added Luffy's stretchy-arm line, Senku's jetpack/flame, Rei's halo/energy wings and arcing flight paths.
- Added visible window-top sampling, monitor work-area homes, shell exclusions and recovery when a perch disappears.
- Added tray menu and reopen behavior, single-instance guard, bounded delta time and exception log.
- Added atomic settings persistence, validation, corrupt-file recovery and save-failure status.

### Fixes during verification

- Fixed compiler references: WindowsBase.dll is in the Framework WPF folder.
- Prevented Explorer's desktop window from triggering fullscreen suppression.
- Increased panel height and added scrolling after preview review exposed clipped bottom controls.
- Made window perch identities independent of screen position so moving a window can update a perch.
- Clamped eligible window tops to the monitor work area to avoid choosing offscreen locations.
- Confirmed all six atlas crops contain transparent and visible pixels; corrected cut-off standing poses and stray artwork.
- Corrected repeat-build asset copying: copying a directory onto an existing assets directory had nested the updated atlas, leaving an old atlas at runtime. Builds now copy the directory contents. Re-ran checks and re-rendered the preview against the final atlas.
- Kept packaging inputs explicit so test preferences, test reports and build previews cannot leak into the portable app ZIP.

### Verification

- Build succeeds with one documented obsolete FormattedText constructor warning.
- `--self-test` passes; saved output is tests/verification.txt.
- `--preview` starts the native engine, renders the control panel and cleanly exits; inspected screenshot is docs/control-room.png.
- The self-test exercises easing/clamping, six image crops, walking/stay/pause/hide transitions, JSON persistence and corrupt settings recovery.
- Additional interactive / hardware scenarios remain unchecked in tests/MANUAL-QA.md.

### Known limits

- True Explorer-icon hiding, vertical perches, articulated animation, a Rei outfit change, precise mixed-DPI placement and a signed installer remain future work. V1 uses edge-peeking, horizontal surfaces and procedural movement of two poses.

### Repository / packaging

- GitHub connector became connected during the task. The connector cannot create repositories, so opened GitHub in the browser. User signed in and provided an empty Spreety repository at https://github.com/on-no303/Spreety. Confirmed the repository is private in its access settings. Network permission was granted, but Git's Windows credential store could not persist credentials and the connector returned 404 for the new private repository. Used the authorized browser upload workflow to publish the source, assets, documentation, verification results and portable ZIP in several commits. A local Git snapshot is created separately; its commit history differs from the browser-created remote history. Do not force-push it over the remote. Once CLI authentication is available, clone the remote into a new folder to get a normal synchronized checkout.
