# Game art assets

321 individual transparent PNG sprites are organized alongside the six original reference images. The full `Environments/arena-background.png` is intentional scenery and remains opaque. Original reference sheets are preserved.

- `Characters/Wizard`: 44 idle, walk, attack, hurt, death, spell-cast and directional idle frames.
- `Enemies`: 60 frames across blue slime, goblin, skeleton warrior, cave bat and rocky golem.
- `Items`: 38 potions, materials, utility items and rare drops.
- `Environments`: 177 terrain pieces, props, portal states, triggers, puzzle objects, hazards and rewards.
- `Effects/Projectiles`: two directional arcane projectiles.

Names use lowercase snake_case; animated character frames follow `subject_action_direction_01.png`, for example `wizard_walk_right_01.png`. Opposite facing variants are mirrored when the reference provides only one facing. Separately drawn wizard directional idle poses retain their original artwork. Some enemy poses are frontal or three-quarter views; the direction identifies their animation set.

Sprites exclude sheet headings and labels. Existing alpha is retained with near-invisible export noise removed. The two originally opaque monster and item sheets were cleaned using the built-in imagegen background-extraction tool; their transparent intermediate sheets are saved next to the originals. Generative cleanup is not guaranteed pixel-identical to the source.

Wizard animation canvases are 384 × 256 with the body horizontally anchored; enemy animation canvases are 240 × 224. Other objects are trimmed with four transparent pixels around them. Terrain tiles and walls have no added padding. Source tiles have varying dimensions and are not a uniform tile grid. Spell casts retain effects attached to the caster; the detached arcane projectile is also exported separately. The overlapping wooden crates are available as a named stack plus the visible small crate.

## Unity import

The repository currently contains art and game rules, without a Unity project or scenes to wire up. Import the separated PNGs as **Sprite (2D and UI)**, **Single**, **Point (no filter)**, **Compression: None**, **Mip Maps: Off**, **Alpha Is Transparency: On**. Use a consistent pixels-per-unit value; no world-unit scale is established yet. `asset_inventory.json` records dimensions, normalized bottom pivots, source rectangles and mirrored variants. Tile sprites can use a centered pivot for a Tilemap. Keep source sheets out of runtime sprite atlases.

## Review and reproduce

Open `gallery.html` to browse every sprite on a checkerboard and search by name.

From the repository root on macOS:

```sh
python3 Tools/Art/build_manifest.py
swift -module-cache-path /tmp/ns-art-module-cache Tools/Art/extract_assets.swift
swift -module-cache-path /tmp/ns-art-module-cache Tools/Art/preview_assets.swift
python3 Tools/Art/verify_assets.py
```

The Python manifest builder only writes coordinates and names; AppKit performs extraction. Extraction uses the checked-in transparent intermediates, so regeneration does not call imagegen. Review sheets go to `tmp/art-review`.

Background extraction prompts used with the built-in tool:

- Monsters: “Edit this existing sprite sheet for background extraction. Remove only the near-black background and all text labels, making those pixels genuinely transparent alpha. Preserve every monster sprite, all 30 poses, exact pixel art, colors, shadows, outlines, effects, positions and grid arrangement. Do not invent, redraw, rearrange or omit any sprite. Output same square full sheet, with genuine transparent background. This is a game production asset extraction, pixel fidelity matters.”
- Items: “Background extraction edit of this exact pixel art game item sheet. Replace the near-black backdrop and text labels with genuine transparent alpha. Preserve all 38 individual items exactly, pixel art designs, outlines, highlights, shadows, glows, original positions, sizes and arrangement. No missing items, no additions, no redesign, no repositioning. Keep square full sheet composition. Output transparent PNG.”
