# BIKE//RUSH

An arcade bicycle racer for **GitHub Game Off 2026** (theme: **SHIFT**) by **Razorbill Studio**. Working title.

Race down mountain roads and forest trails. Off every jump you can spin or flip; press **Shift** right as you land for a **PERFECT** landing and a burst of boost. Some jumps have a glowing **SHIFT portal** over them: fly through and you're sent to another world for a few seconds, then dropped back into the race ahead of where you left.

**Pedal → Launch → Trick → Land → Boost → Portal → back in the race.**

## Controls

| Key | Action |
|---|---|
| W / Right trigger | Pedal |
| S / Left trigger | Brake, then reverse |
| A D / Left stick | Steer and lean |
| Space (hold, then release) | Bunny hop |
| Left Shift | Boost, and the landing press |
| In the air: steer / pedal / brake | Spin, flip forward, flip back |
| R | Reset |
| Esc | Pause |

All controls can be rebound in Settings → Controls.

## Tech

- Unity **6000.3.10f1**, Universal Render Pipeline, Input System (Active Input Handling: Both)
- Builds for Web (itch.io) and Windows
- Game code lives in `Assets/_ShiftRush/` (shared with our earlier game, SHIFT//RUSH)

## Third-party packs (not in this repo)

The game uses these paid packs. Their licences don't allow the source files to be shared publicly, so their folders are git-ignored. To open and build the project, buy and import each one into `Assets/` at its default location:

| Pack | Store | Used for |
|---|---|---|
| Simple Bicycle Physics | Unity Asset Store | Bikes, riders, bike physics |
| Heat – Complete Modern UI (Michsky) | Unity Asset Store | Menus and settings UI |
| AMS UI Soft Mask | Unity Asset Store | Soft UI masks |
| UI SFX Mega Pack | Unity Asset Store | Menu sounds |

Also required from Unity's registry: **Animation Rigging** (installed through `Packages/manifest.json`).

### One change to Simple Bicycle Physics

After importing Simple Bicycle Physics, `Scripts/BicycleController.cs` needs three small edits so the game can drive the bike (they're marked `BIKE//RUSH patch` in our copy):

1. Make `sprint` public: replace `bool isRaw, sprint;` with `bool isRaw;` and `[HideInInspector] public bool sprint;`
2. Add a switch next to it: `[HideInInspector] public bool externalInput;`
3. First line of `ApplyCustomInput()`: `if (externalInput) return;`

And add a read-only view of the hop state under `bool isBunnyHopping;`:
`public bool IsBunnyHopping { get { return isBunnyHopping; } }`

Without the packs, the project's own code is still public and readable, but scenes will show missing references.

## Credits

- **Razorbill Studio**: design, code, level design
- **Concept & Inspiration:** Jennifer Ugwoke
- Third-party packs as listed above
- **Music** (Pixabay Content License; the audio files themselves are not in this repo):
  - Music by [OctoSound](https://pixabay.com/users/octosound-39205072/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=178816) from [Pixabay](https://pixabay.com/music//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=178816)
  - Music by [Andrii Poradovskyi](https://pixabay.com/users/lnplusmusic-47631836/?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=628850) from [Pixabay](https://pixabay.com//?utm_source=link-attribution&utm_medium=referral&utm_campaign=music&utm_content=628850)
  - "Trap Dynamic Brass Thunder Power" by alexzavesa from Pixabay
  - "Dance Funk" by roybushband from Pixabay

Any AI-generated art or audio used in the game will be listed here.
