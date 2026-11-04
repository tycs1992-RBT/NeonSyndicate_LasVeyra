using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>One-use data carried across the existing world reload. No update, actor, camera or save authority.</summary>
    public sealed class FantasyPreviewIntentV1 : MonoBehaviour
    {
        public long ExpiresUtcTicks;
        public int Revision = 10;
    }
}
