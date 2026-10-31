# Live characters: drop-in folder

Put each of your own Blender characters in its own folder here, then open Unity. The full step-by-step guide is
`Docs/LIVE_CHARACTERS.md` in the project folder.

- Folder name = the character. `Malik` is the player. Any other name is a person's exact in-game name, for example
  `Corin Pell`.
- Inside: one exported `.fbx` (optional `_LOD1.fbx` and `_LOD2.fbx` next to it) and the textures, named after the
  material: `Skin_Albedo.png`, `Skin_Normal.png`.
- Unity prepares it by itself (the `Prepared` folder it makes is never committed). The Console line that starts with
  `Live characters ready` lists who is live; a line that starts with `Live character not prepared` says what to fix.
- Menu: Neon Syndicate > Characters > Rebuild live characters.
