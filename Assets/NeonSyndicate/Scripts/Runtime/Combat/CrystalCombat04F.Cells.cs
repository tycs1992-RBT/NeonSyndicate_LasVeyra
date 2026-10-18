using UnityEngine;

namespace NeonSyndicate
{
    public sealed partial class CrystalCombat04F
    {
        private void PlaceCells()
        {
            PlaceCell("dispatch.reserve", "Venue:dispatch.receiver_a");
            PlaceCell("pressure.reserve", "Venue:pressure.nest");
            PlaceCell("forge.reserve", "Venue:forge.activity");
        }
        private void PlaceCell(string id, string at)
        {
            string key = "cell:" + id;
            if (_placed.Contains(key)) return;
            Vector3 pos;
            if (!Resolve(at, out pos)) return;
            Interactable cell = null;
            cell = Interactable.Create("Finite crystal reserve " + id, pos, 1.5f, "Take reserve cell: +30 Resonance (once)", () =>
            {
                if (cell == null || !SafeToRecharge || !cell.Available(Game.PlayerPosition, false)) return;
                if (Physics.Linecast(Game.Player.ChestPosition, cell.transform.position + Vector3.up * 0.95f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) return;
                float gain = _charge.ClaimCell(id);
                if (gain > 0f) { Tell("Reserve cell +" + Mathf.RoundToInt(gain) + " Resonance. This cell is spent."); CrystalSounds.PlayChargeReady(CrystalBehavior.Shardwake, pos); }
            }, transform);
            cell.Priority = 1;
            cell.Condition = () => Game.IsPlaying && SafeToRecharge && Charge < MaxCharge && !_charge.CellCollected(id);
            cell.HeightTolerance = 1.5f;
            _placed.Add(key);
            MeshBuilder b = new MeshBuilder(1);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f, c = (i + 1) * Mathf.PI / 3f;
                Vector3 u = new Vector3(Mathf.Cos(a) * 0.075f, 0.98f, Mathf.Sin(a) * 0.075f);
                Vector3 v = new Vector3(Mathf.Cos(c) * 0.075f, 0.98f, Mathf.Sin(c) * 0.075f);
                b.Triangle(0, u, v, Vector3.up * 1.12f, (u + v - Vector3.up * 1.9f).normalized, Vector2.zero, Vector2.right, Vector2.up);
                b.Triangle(0, v, u, Vector3.up * 0.87f, (u + v - Vector3.up * 2.02f).normalized, Vector2.right, Vector2.zero, Vector2.up);
            }
            GameObject art = Shapes.MeshObject("Finite reserve crystal", b.ToMesh("Reserve cell 04F"), new[] { Mats.Solid(new Color(0.46f, 0.82f, 0.72f), 0.55f) }, cell.transform, false);
            ReserveCellVisual04F state = art.AddComponent<ReserveCellVisual04F>();
            state.Owner = this; state.Id = id;
        }
        public bool CellCollected(string id) { return _charge.CellCollected(id); }
    }

    /// <summary>Collected cells disappear on load and reappear only for a genuinely new playthrough.</summary>
    public sealed class ReserveCellVisual04F : MonoBehaviour
    {
        public CrystalCombat04F Owner;
        public string Id;
        private Renderer _renderer;
        private void Update()
        {
            if (_renderer == null) _renderer = GetComponent<Renderer>();
            if (_renderer != null) _renderer.enabled = Owner != null && !Owner.CellCollected(Id);
        }
    }
}
