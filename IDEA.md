# Spreety

Spreety is a decorative Windows desktop app. It has no productivity goal: it makes a desktop feel alive with tiny anime companions in a vinyl collectible / chibi sprite style.

## The initial crew

| Character | Personality | Travel idea |
| --- | --- | --- |
| Monkey D. Luffy | Playful, friendly, adventurous | Uses stretching powers between edges |
| Senku Ishigami | Curious scientist | Uses a handmade jetpack |
| Rei Ayanami | Quiet and calm | Changes into an EVA suit and flies |

## Intended behavior

- Characters act independently: sit, greet, stroll, hide, and occasionally travel.
- They spend most of their time at the screen edges and taskbar, with occasional visits to the top and sides of open applications.
- They should hide behind desktop shortcuts, feel small, and avoid disrupting work.
- Controls let the user choose which companions are visible and which can roam.
- Travel is character specific rather than a generic teleport.

## Version 1 implementation

A native Windows WPF prototype with two transparent poses per character. It includes taskbar-top resting, visible window-top perches, greetings, pose bobbing/walking, edge peeking, distinct travel effects, per-character Show/Roam, global pause/home, size options, fullscreen hiding, persistent preferences, and tray controls.

## Still part of the product vision

True hiding behind individual Explorer icons; vertical wall perches; fully articulated walk/wave animation; a separate Rei outfit transformation and EVA machinery; sophisticated rubber-arm attachment for Luffy; interaction between companions; dragging; exact per-monitor DPI handling; taskbars on non-bottom edges; signed installer; richer pose libraries.

These remain explicit follow-up work, not completed v1 features. The v1 Rei sprite already wears her plugsuit, and her flight uses stylized energy wings. Edge peeking simulates hiding by lowering the sprite behind its perch line.
