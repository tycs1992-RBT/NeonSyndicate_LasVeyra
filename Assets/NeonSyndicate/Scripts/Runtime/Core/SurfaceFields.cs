namespace NeonSyndicate
{
    /// <summary>
    /// The procedural PBR surface families (Phase 2 of the visual upgrade). Each kind has a pure height-field
    /// generator (SurfaceHeight), a pure colouring (SurfaceColor) and the pure map derivations (SurfaceMaps); Tex turns
    /// them into textures and Mats puts them on the Standard or URP Lit shader. Large kinds cover ground, walls and
    /// floors and get the full preset size; the others are small-object or micro surfaces and get half of it.
    /// </summary>
    public enum SurfaceKind
    {
        Asphalt,
        Pavers,
        Concrete,
        Grass,
        Sand,
        Dirt,
        Rock,
        Gravel,
        RoofTiles,
        Corrugated,
        Wood,
        Bark,
        SnowCrust,
        SnowThin,
        Ice,
        Ripples,
        Brushed,
        Grain,
        Pores,
        PaintPeel,
        Fibre,
        Leaf,
        UhRock,
        UhConcrete,
        UhTile,
        UhTerrazzo,
        UhGrating,
        UhBrick,
        UhPlate,
        UhMarble,
        UhAsh,
        UhSoil,
        UhLava,
        UhWeave
    }

    /// <summary>
    /// The pure, engine-free output of one surface generator: a tileable relief (Height), a slow albedo variation
    /// (Tone), the kind's main feature mask (Mask: cracks, grout, rust, veins, seams) and a secondary feature (Aux:
    /// puddles, specks, lichen, scuffs). All n by n in [0,1]. Colouring, normals, occlusion and gloss are derived from
    /// these so albedo, relief and shading always agree.
    /// </summary>
    public sealed class SurfaceFields
    {
        public readonly int Size;
        public readonly float[] Height;
        public readonly float[] Tone;
        public readonly float[] Mask;
        public readonly float[] Aux;

        public SurfaceFields(int size, float[] height, float[] tone, float[] mask, float[] aux)
        {
            Size = size;
            Height = height;
            Tone = tone;
            Mask = mask;
            Aux = aux;
        }

        public int Length { get { return Size * Size; } }
    }
}
