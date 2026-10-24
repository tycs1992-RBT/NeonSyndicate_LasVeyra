# Live characters: your Blender people in the game

Your Blender Malik (the H9 refinement) is the player in ordinary play: on foot, in combat, with travel powers, on the
bike, in ragdoll and death, in cutscenes and in first person (where only his shadow shows, like before). Dante Rook (H9)
stands at the Forge. The game itself does not change: movement, combat, weapons, dialogue, quests and saves are the same
original systems; only the look of the person is your model. If anything about a character cannot load, that one person
quietly keeps the old procedural look and the Console says why (one warning).

You can add more humanoid people yourself, with no code: export from Blender, drop the files in a folder, open Unity.

The ordinary people of the city (civilians, clerks, police, gang members, mission enemies) are drawn with the two CC0
crowd bodies through the same system; that part, its budget, its switches, crowd drop-ins, the fitted hair and headwear
and the named cast on the study bodies are in `Docs/LIVE_CROWD.md`.

## 1. See Malik live (first time on a computer)

1. Pull the branch (with Git LFS, as usual).
2. Open the project in Unity 6000.3.16f1 and wait for the import to finish.
3. Unity then prepares the live characters by itself, once (a progress bar "Neon Syndicate: live characters" may show
   for about a minute). Wait for this line in the Console:
   `Live characters ready (editor load): Malik Veyr H9, Dante Rook H9. Prepared 2 now, registry written.`
4. Press Play (Neon Syndicate > Play) and start the game (New Game, or the Gothic Civic H5 world).
5. In the Console you should see `Live character: Malik Veyr H9 now draws the player`. Walk to the Forge and you should see
   `Live character: Dante Rook H9 now draws npc:Dante Rook`.

On later openings Unity only checks the files (no waiting) and prepares again only when the art changed. Unity rewrites
the `.meta` files of the H9 models and textures on the first import; that is normal.

What to look at: Malik's face, hair, coat and boots while running, sword and gun poses, dodge, first person (no body,
only the shadow), a fall into ragdoll, death and respawn, riding the bike. Dante at the Forge: apron and belt come from
the model (the old procedural apron is hidden), the hammer stays in his hand, talking (E or Y) and the Forge still work.

## 2. Make your own character in Blender

Start from Malik so the skeleton and the attachment points are right.

1. Open `ArtSource/AscensionH9/MalikVeyrH9.blend` and immediately use File > Save As to save a copy with a new name (for
   example `ArtSource/MyCharacters/CorinPell.blend`). Never overwrite the original.
2. Keep the armature `MalikRig` exactly as it is: do not rename, delete or move bones. You may add extra bones (a cape,
   a ponytail) as children of existing body bones; they follow their parent.
3. Keep the empties `review_forward` and the ten `socket_...` empties (they tell the game which way the character faces
   and where hands, head and chest are). If they are missing, Unity adds the sockets itself.
4. Replace the body and the costume: delete the Malik meshes you do not want and add your own. Every mesh needs an
   Armature modifier pointing at `MalikRig` (Ctrl+P > With Automatic Weights, then fix the weights).
5. Weights: in Weight Paint mode, for each mesh, use Weights > Limit Total (Limit 8), then Weights > Normalize All. Every
   vertex must follow at least one bone.
6. Materials: give each material a clear name, for example `Skin`, `Coat`, `Hair`, `Boots`. Hair, brows and lashes with
   alpha-cut textures work best with names that contain hair, brow, lash or groom.
7. Pose: put the armature back in its rest pose (Pose mode, select all, Pose > Clear Transform > All). Keep the character
   at the origin, standing on the ground, at real size (about 1.8 m), facing the front view.
8. Optional, for speed: join meshes that share a material (select them, Ctrl+J). Fewer pieces draw faster.

### Export settings (File > Export > FBX)

These are the settings the project's own exporter uses (`tools/export_selected_malik_h9.py`):

| Setting | Value |
|---|---|
| Limit to Selected Objects | on (select the armature, your meshes and the empties first) |
| Object Types | Armature, Mesh and Empty only |
| Scale | 1.00 |
| Apply Scalings | FBX All |
| Forward | -Z Forward |
| Up | Y Up |
| Apply Unit | on |
| Smoothing | Face |
| Add Leaf Bones | off |
| Bake Animation | off |
| Path Mode | Relative (textures are taken from the folder by name anyway) |

Name the file after the character, for example `CorinPell.fbx`. For lower detail versions at distance, save decimated
copies with the same armature as `CorinPell_LOD1.fbx` and `CorinPell_LOD2.fbx` (optional).

### Textures

Save each texture as PNG (TGA, JPG and PSD also work) named after its material plus a role:

| File name | What it is |
|---|---|
| `Skin_Albedo.png` (or `_BaseColor`, `_Diffuse`, `_Color`) | the colour |
| `Skin_Normal.png` | the normal map |
| `Gem_Emission.png` | glow colour (optional) |
| `Armor_Metallic.png` | metallic in red, smoothness in alpha (optional) |

Spaces, dashes and underscores do not matter: material `Worn Leather` matches `Worn_Leather_Albedo.png`.

## 3. Drop it in

1. In Unity choose Neon Syndicate > Characters > Open drop-in folder. It opens `Assets/NeonSyndicate/Art/LiveCast`.
2. Make a folder named after the character and put the FBX files and textures inside (subfolders for textures are fine):
   - `Malik` (or `Player`) replaces the player's look with your version.
   - Any other name is a person's exact in-game name, for example `Corin Pell`. A campaign cast name (Nessa Vale, Corin
     Pell, Tamsin Reyes, Odile Marsh, Ilse Varro, Bram Achterberg, Ansel Whit, Ion Solane, Mara Solane, Jax Calder,
     Dante Rook, Iris Moon, June Pyre) goes only on that real cast member, never on a lookalike.
3. Switch back to Unity. It notices the new files and prepares the character (or choose Neon Syndicate > Characters >
   Rebuild live characters). The Console line `Live characters ready` now lists `drop-in Corin Pell`.
4. Press Play.

A drop-in folder wins over the built-in character with the same name, so a `Malik` folder replaces the H9 Malik (if
your version cannot load, the built-in H9 Malik stays). Unity writes a `Prepared` folder inside each character folder;
it is generated, never committed, and rebuilt on any computer. To switch a drop-in off, put `_` at the start of its folder
name.

### Optional settings file

You never need it, but a `livecast.json` in the folder can change details:

```json
{
  "identity": "npc:Corin Pell",
  "creator": "Tyler",
  "license": "Owner original",
  "hideCostume": ["Costume Torso 04E", "Costume Waist 04E"],
  "materials": [
    { "material": "Coat", "albedo": "CoatPainted.png", "smoothness": 0.25 },
    { "material": "Visor", "mode": "transparent", "color": "#AACCFF20" },
    { "material": "Crystal", "emissionColor": "#7A3BFF" }
  ]
}
```

- `identity`: who it draws (`malik`, or `npc:` plus the in-game name) when the folder name is not enough.
- `hideCostume`: which old procedural costume pieces to hide on a named person (head, torso, waist; the held tool never
  hides). By default a drop-in hides head, torso and waist pieces.
- `materials`: per material, a texture file by name (`albedo`, `normal`, `emission`, `metallic`), `mode` (`opaque`,
  `cutout`, `transparent`), `smoothness` and `metalness` (0 to 1), `bump` (normal strength), `color` and
  `emissionColor` (hex).
- `"enabled": false` switches the folder off.

Only drop in characters you made or have the right to use commercially.

## 4. When a character keeps the old look

Read the one Console warning. On opening Unity it starts with `Live character not prepared:`; in Play mode it starts with
`Live character fallback:`. Common reasons:

| The warning says | Do this |
|---|---|
| the armature lacks ... H9 bones | keep `MalikRig` and its bone names |
| more than eight influences or weights not normalized | Weights > Limit Total (8), then Normalize All |
| left hand is not on the character's left | export with Forward -Z and Up Y |
| cannot tell which way the character faces | include the `review_forward` empty (tick Empty in Object Types) |
| units or up axis do not match | Apply Scalings FBX All, Apply Unit on, real size |
| reduced 17-bone body (Low graphics preset) | use Medium or higher graphics; named people then respawn with the full body |
| cannot draw under the URP pipeline | after switching pipelines choose Rebuild live characters |
| the prepared prefab is missing | choose Neon Syndicate > Characters > Rebuild live characters |
| a vertex ... / unmapped weighted bone | parent extra bones under body bones; weight every vertex |

## 5. Good to know

- Equipped armour and clothing-store outfits do not change the imported model (its costume is what you modelled).
- The original procedural pose drives the model (walk, aim, sword, dodge, travel, riding). The authored H8 motion set is
  still an Editor review only.
- The Editor art reviews (Neon Syndicate > 04OH) still work: while one runs on Malik or Dante, the live model steps
  aside and comes back when the review ends.
- Creatures and companions use other skeletons; bringing those in is a later step.
- Generated files (`Prepared` folders and `Assets/NeonSyndicate/Resources/LiveCast`) are git-ignored and rebuilt on every
  computer; only your FBX, textures and `livecast.json` are committed.

Where it lives, for the curious: `Scripts/Runtime/Characters/LiveCastVisual*.cs` draws the characters,
`LiveCastRegistry.cs`, `LiveCastRules.cs` and `LiveCastFolderRules.cs` hold the data and rules, and
`Scripts/Editor/LiveCastSetup*.cs` prepares them.
