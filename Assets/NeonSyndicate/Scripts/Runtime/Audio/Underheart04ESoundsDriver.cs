using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Ticks the 04E audio every frame in real time (bank uploads, zone crossfade, loop voices, the score), so fades
    /// requested while the game is paused or in slow motion still complete. Lives on the "Underheart Sounds 04E" object
    /// that Underheart04ESounds creates under the AudioManager.
    /// </summary>
    public sealed class Underheart04ESoundsDriver : MonoBehaviour
    {
        private void Update()
        {
            Underheart04ESounds.Tick(Mathf.Min(Time.unscaledDeltaTime, 0.1f));
        }
    }
}
