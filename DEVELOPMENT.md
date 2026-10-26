# Development log

## 2026-10-27 · Version 1

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

True Explorer-icon hiding, vertical perches, articulated animation, a Rei outfit change, precise mixed-DPI placement and a signed installer remain future work. V1 uses edge-peeking, horizontal surfaces and procedural movement of two poses.

### Repository / packaging

GitHub connector became connected during the task. The connector cannot create repositories, so opened GitHub in the browser. User signed in and provided an empty Spreety repository at https://github.com/on-no303/Spreety. Confirmed the repository is private in its access settings. Network permission was granted, but Git's Windows credential store could not persist credentials and the connector returned 404 for the new private repository. Used the authorized browser upload workflow to publish the source, assets, documentation, verification results and portable ZIP in several commits. A local Git snapshot is created separately; its commit history differs from the browser-created remote history. Do not force-push it over the remote. Once CLI authentication is available, clone the remote into a new folder to get a normal synchronized checkout.
