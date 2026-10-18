using System.Collections;
using UnityEngine;

namespace NeonSyndicate
{
    public static partial class QuestSteps
    {
        /// <summary>A physical, recoverable parcel. The existing runner owns checkpoint replay, death, abandon and rewards.</summary>
        private static IEnumerator Courier04G(QuestRun r, QuestStep s)
        {
            Vector3 source = r.Place(s.At, s.OffsetX, s.OffsetZ);
            Vector3 destination = r.Place(s.To, s.ToOffsetX, s.ToOffsetZ);
            QuestProp parcel = QuestProps.Build("Crystal", source, 0f, r.C.M.transform, Accent(r));
            r.C.Objects.Add(parcel.gameObject);
            parcel.transform.localScale *= 0.38f;
            // The original prop only supplies presentation. This step controls its position and lifetime.
            parcel.enabled = false;
            foreach (Collider collider in parcel.GetComponentsInChildren<Collider>()) collider.enabled = false;
            bool carrying = false, delivered = false;
            float held = 0f, lastDamage = -1f;
            Vector3 routed = Vector3.zero;
            bool routeSet = false;
            int objectivePhase = -1;
            PlayerController carrier = Game.Player;
            // The runner's Release path is authoritative even if a nested coroutine is stopped without disposal.
            r.Cleanups.Add(() => { if (carrier != null && carrier.Anim != null) carrier.Anim.ParcelCarry04G = false; });
            Vector3 pickup = source, previous = Game.PlayerPosition;
            WorldMarker marker = ExactMarker(r, pickup, MarkerColor, 0.6f);
            Blip blip = r.C.BlipAt(pickup, MarkerColor, "Sealed reserve parcel");
            Interactable take = QuestPrompt("Recover sealed reserve", pickup, 2f, "Interact to carry the sealed reserve", () =>
            {
                if (Game.Player == null || Game.Player.IsDead || Game.Player.InVehicle) return;
                carrying = true;
                lastDamage = Game.Player.Health.LastDamageTime;
                previous = Game.PlayerPosition;
            }, r.C.M.transform);
            take.Condition = () => !carrying;
            r.C.Objects.Add(take.gameObject);
            Interactable fit = QuestPrompt("Fit sealed reserve", destination, 2f, "Interact to fit the carried reserve", () =>
            { if (carrying) held = 0.001f; }, r.C.M.transform);
            fit.Condition = () => carrying && held <= 0f;
            r.C.Objects.Add(fit.gameObject);
            try
            {
                while (!delivered)
                {
                    PlayerController player = Game.Player;
                    if (parcel == null) throw new System.InvalidOperationException("The delivery parcel was lost. Restart this checkpoint to recover it.");
                    if (player == null || player.IsDead) { yield return null; continue; }
                    if (!Game.IsPlaying || GameTime.Paused) { yield return null; continue; }
                    Vector3 now = player.transform.position;
                    if (carrying && CourierRules04G.ShouldDrop(player.InVehicle, player.Health.LastDamageTime > lastDamage, Vector3.Distance(previous, now)))
                    {
                        carrying = false;
                        held = 0f;
                        // Put the parcel at the last verified foot position, never on an airborne vehicle or across a warp.
                        RaycastHit floor;
                        pickup = previous;
                        if (Physics.Raycast(previous + Vector3.up * 0.4f, Vector3.down, out floor, 1.5f, Layers.WorldMask, QueryTriggerInteraction.Ignore)) pickup = floor.point;
                        else pickup = source;
                        take.transform.position = pickup;
                        if (Game.Hud != null) Game.Hud.Notify("The reserve slipped loose. Recover it at the parcel marker; the job is still active.", 4f);
                    }
                    previous = now;
                    Vector3 target = carrying ? destination : pickup;
                    if (carrying)
                    {
                        parcel.transform.position = player.ChestPosition - player.transform.right * 0.35f + player.transform.forward * 0.12f;
                        parcel.transform.rotation = player.transform.rotation;
                    }
                    else parcel.transform.position = pickup + Vector3.up * 0.35f;
                    marker.transform.position = target;
                    blip.Position = target;
                    player.Anim.ParcelCarry04G = carrying;
                    if (!routeSet || (routed - target).sqrMagnitude > 0.01f)
                    { r.C.Target(target); routed = target; routeSet = true; }
                    if (held > 0f)
                    {
                        bool near = !player.InVehicle && AtExact(destination, 2.2f) &&
                            !Physics.Linecast(player.ChestPosition, destination + Vector3.up * 0.6f, Layers.WorldMask, QueryTriggerInteraction.Ignore);
                        held = near ? held + Time.deltaTime : 0f;
                        delivered = held >= Mathf.Clamp(s.Seconds > 0f ? s.Seconds : 3f, 1f, 8f);
                    }
                    int phase = held > 0f ? 2 : carrying ? 1 : 0;
                    if (phase != objectivePhase)
                    {
                        objectivePhase = phase;
                        r.C.Objective(phase == 2 ? "Keep the reserve steady in the fitting until it locks." : phase == 1
                            ? Obj(s, "Carry the sealed reserve to the marked fitting. Recover it if dropped.")
                            : "Collect the physical reserve at the parcel marker. It can be recovered if dropped.");
                    }
                    yield return null;
                }
            }
            finally
            {
                if (carrier != null && carrier.Anim != null) carrier.Anim.ParcelCarry04G = false;
                if (parcel != null) Object.Destroy(parcel.gameObject);
                if (take != null) Object.Destroy(take.gameObject);
                if (fit != null) Object.Destroy(fit.gameObject);
                if (marker != null) Object.Destroy(marker.gameObject);
                r.C.RemoveBlip(blip);
                r.C.ClearTarget();
            }
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.ItemPickup, 0.6f);
        }
    }

    public static class CourierRules04G
    {
        public static void ResetStatics() { }
        public static bool ShouldDrop(bool inVehicle, bool hurt, float frameDistance)
        { return inVehicle || hurt || float.IsNaN(frameDistance) || frameDistance > 9f; }
    }
}
