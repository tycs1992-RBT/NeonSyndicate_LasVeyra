namespace NeonSyndicate
{
    public sealed partial class ForgeScreen
    {
        internal bool DanteStationInUse { get { return _open && _station == ForgeStation.Forgewright; } }
    }
}
