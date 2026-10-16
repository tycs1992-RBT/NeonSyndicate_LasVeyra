using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Ticks the crystal loop fades and pitch glides every frame in real time (so a stop requested while the game is paused
    /// or in slow motion still completes). Lives on the "Crystal Sounds" object CrystalSounds creates.
    /// </summary>
    public sealed class CrystalSoundsDriver : MonoBehaviour
    {
        private void Update()
        {
            CrystalSounds.Tick(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
        }
    }
}
