# Native integration QA

Automated checks and a rendered control-panel review were completed on the development machine. The checklist below tracks additional interactive acceptance testing; unchecked entries are not claimed as tested.

- [x] Compile the x64 app with the bundled Windows .NET Framework compiler.
- [x] Render and inspect the control panel; all controls are reachable, with scrolling on short windows.
- [x] Decode all six character poses and verify meaningful alpha transparency.
- [x] Test walking, disabling roam, pausing, and hiding through the activity state machine.
- [x] Round-trip settings in an isolated directory and recover from invalid JSON.
- [ ] Observe the crew for 15 minutes and check idle/travel pacing.
- [ ] Toggle every control interactively; restart and confirm persistence.
- [ ] Click and type into windows underneath a sprite; confirm focus stays there.
- [ ] Move, resize, cover and minimize windows while a companion is perched.
- [ ] Enter and exit a fullscreen game/video; ensure companions disappear and return.
- [ ] Close the panel, reopen from tray, pause from tray and quit from tray.
- [ ] Hot-plug displays; check multiple monitors including negative coordinates.
- [ ] Verify 100%, 125%, 150% DPI and mixed-DPI displays (known limitation).
- [ ] Test taskbar auto-hide and alternate taskbar positions (known limitation).
