using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>Scenic navigation only. Does not register a quest, capture site, resource or interactable.</summary>
    public sealed class VisualRebirth04ACourtMarker : MonoBehaviour
    {
        private Blip _blip;
        public void Init(string title)
        {
            if (_blip != null) Blip.Remove(_blip);
            _blip = Blip.Add(BlipKind.Activity, transform, transform.position, new Color(.54f,.76f,.61f), "G", title, false);
        }
        private void OnDestroy() { if (_blip != null) { Blip.Remove(_blip); _blip = null; } }
    }
}
