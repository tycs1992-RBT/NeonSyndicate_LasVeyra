using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// The things the player presses E (pad: interact) on underground: the six rest points (heal, set the return point,
    /// autosave when allowed), the three lifts (restore at the deep end, then ride either way; a ride is a short fade, a
    /// disclosed load, with the lift motor loop running for its length) and the canal return gate (unbar it from the valve
    /// room; from the stair side it is barred). Opening a shortcut is persistent and raises ShortcutOpened.
    /// </summary>
    public sealed partial class UnderheartWorld
    {
        private const float RestHealFraction = 0.35f;
        private const float RestCooldown = 20f;

        private Transform _interactRoot;
        private float _restReady;
        private readonly List<Interactable> _interactables = new List<Interactable>();

        private void CreateInteractions()
        {
            if (_builder == null || _builder.Root == null) return;
            GameObject root = new GameObject("Interactions (always on)");
            root.transform.SetParent(_builder.Root, false);
            _interactRoot = root.transform;
            foreach (string id in UnderheartIds.RestPoints)
            {
                UhAnchor a = _layout.Anchor(id);
                if (a == null || a.Cell < 0) continue;
                string anchorId = id;
                Interactable it = Make("Rest " + id, a.Pos, 2.4f, "Press E to rest", () => Rest(anchorId));
                it.DynamicPrompt = () => RestPrompt(anchorId);
            }
            foreach (UhShortcut s in _layout.Shortcuts)
            {
                UhShortcut sc = s;
                if (s.Kind == UhShortcutKind.Lift)
                {
                    UhAnchor deep = _layout.Anchor(s.DeepAnchor);
                    if (deep != null && deep.Cell >= 0)
                    {
                        Interactable it = Make("Lift " + s.Id + " deep", deep.Pos, 2.6f, "Press E to restore the lift", () => UseLift(sc, true));
                        it.DynamicPrompt = () => LiftPrompt(sc, true);
                    }
                    if (s.FarCell >= 0)
                    {
                        Interactable it = Make("Lift " + s.Id + " far", s.Far, 2.6f, "Press E to call the lift", () => UseLift(sc, false));
                        it.DynamicPrompt = () => LiftPrompt(sc, false);
                    }
                    continue;
                }
                if (s.Portal < 0) continue;
                UhAnchor control = _layout.Anchor(s.DeepAnchor);
                if (control != null && control.Cell >= 0)
                {
                    Interactable it = Make("Gate " + s.Id, control.Pos, 2.4f, "Press E to unbar the return gate", () => UnlockShortcut(sc.Id, true));
                    it.Condition = () => !ShortcutOpen(sc.Id);
                }
                // The barred side: a readable reason instead of a silent wall.
                UhPortal gate = _layout.Portals[s.Portal];
                int farCell = control != null && gate.A == control.Cell ? gate.B : gate.A;
                UhCell fc = _layout.Cells[farCell];
                Vector3 barred = gate.Center;
                if (gate.FaceAlongX) barred.z += fc.CenterZ > gate.Z ? 1.1f : -1.1f;
                else barred.x += fc.CenterX > gate.X ? 1.1f : -1.1f;
                Interactable bar = Make("Gate " + s.Id + " barred", barred, 1.8f, "Press E to try the gate", () =>
                {
                    Underheart04ESounds.Play(UhSfx.UiDenied, AnchorPoint(barred), 0.7f);
                    if (Game.Hud != null) Game.Hud.Notify("Barred from the far side. Unbar it in the " + UnderheartIds.RegionTitle(fc.Region) + " valve room to open a shortcut back to the refuge.", 4.5f);
                });
                bar.Condition = () => !ShortcutOpen(sc.Id);
            }
        }

        private Interactable Make(string name, Vector3 local, float radius, string prompt, System.Action use)
        {
            Vector3 world = _frame.ToWorld(local) + Vector3.up * 0.9f;
            Interactable it = Interactable.Create(name, world, radius, prompt, use, _interactRoot);
            it.HeightTolerance = 2.2f;
            it.OnFoot = true;
            it.InVehicle = false;
            _interactables.Add(it);
            return it;
        }

        private Vector3 AnchorPoint(Vector3 local) { return _frame.ToWorld(local); }

        // ================================================================== rest points

        private string RestPrompt(string anchorId)
        {
            float wait = _restReady - Time.unscaledTime;
            if (wait > 0f) return "Rested. Ready again in " + Mathf.CeilToInt(wait) + " s";
            return anchorId == _restAnchor ? "Press E to rest (your return point)" : "Press E to rest and make this your return point";
        }

        private void Rest(string anchorId)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            Vector3 pos;
            float yaw;
            if (!TryAnchor(anchorId, out pos, out yaw)) return;
            if (Time.unscaledTime < _restReady)
            {
                if (Game.Hud != null) Game.Hud.Notify("Catch your breath first (" + Mathf.CeilToInt(_restReady - Time.unscaledTime) + " s).", 2f);
                return;
            }
            _restReady = Time.unscaledTime + RestCooldown;
            p.Health.Heal(p.Health.MaxHealth * RestHealFraction);
            p.Health.Burning = 0f;
            _cooled = 0f;
            _restAnchor = anchorId;
            if (Game.Root != null) Game.Root.LastSafehouse = pos;
            Underheart04ESounds.Play(UhSfx.RestChime, pos, 0.9f);
            string region = UnderheartIds.RegionOfAnchor(anchorId);
            if (Game.Hud != null) Game.Hud.Notify("Rested at the " + UnderheartIds.RegionTitle(region) + " rest point. If you fall, you wake here.", 4f);
            if (Game.Root != null) Game.Root.Autosave();
        }

        // ================================================================== lifts

        private string LiftPrompt(UhShortcut s, bool deepEnd)
        {
            string other = deepEnd ? FarTitle(s) : UnderheartIds.RegionTitle(UnderheartIds.RegionOfAnchor(s.DeepAnchor));
            if (ShortcutOpen(s.Id)) return "Press E to ride the " + s.Title.ToLowerInvariant() + " to the " + other;
            return deepEnd ? "Press E to restore the " + s.Title.ToLowerInvariant() : "Press E to call the " + s.Title.ToLowerInvariant();
        }

        private string FarTitle(UhShortcut s)
        {
            return s.FarCell >= 0 ? UnderheartIds.RegionTitle(_layout.Cells[s.FarCell].Region) : "surface";
        }

        private void UseLift(UhShortcut s, bool deepEnd)
        {
            PlayerController p = Game.Player;
            if (p == null || p.IsDead) return;
            if (!ShortcutOpen(s.Id))
            {
                if (deepEnd)
                {
                    UnlockShortcut(s.Id, true);
                    return;
                }
                Underheart04ESounds.Play(UhSfx.UiDenied, p.transform.position, 0.7f);
                if (Game.Hud != null)
                    Game.Hud.Notify("The " + s.Title.ToLowerInvariant() + " is locked out below. Restore it from the " + UnderheartIds.RegionTitle(UnderheartIds.RegionOfAnchor(s.DeepAnchor)) + " end.", 4.5f);
                return;
            }
            // A ride already under way (or another fade) ignores the press, so the clank and motor never play for nothing.
            if (Game.Root == null || Game.Root.InTransition) return;
            if (UnderAttack())
            {
                if (Game.Hud != null) Game.Hud.Notify("The lift will not run while you are under attack.", 3f);
                return;
            }
            Vector3 local;
            float localYaw;
            if (deepEnd) { local = s.Far; localYaw = s.FarYaw; }
            else
            {
                UhAnchor deep = _layout.Anchor(s.DeepAnchor);
                if (deep == null) return;
                local = deep.Pos;
                localYaw = deep.Yaw;
            }
            // Turn round on arrival so the player steps out of the cage, not into its back wall.
            Vector3 world = _frame.ToWorld(local) + Vector3.up * 0.05f;
            float yaw = Mathf.Repeat(_frame.YawToWorld(localYaw) + 180f, 360f);
            Underheart04ESounds.Play(UhSfx.LiftClank, p.transform.position, 0.9f);
            if (Game.Hud != null) Game.Hud.Notify("The " + s.Title.ToLowerInvariant() + " carries you to the " + (deepEnd ? FarTitle(s) : UnderheartIds.RegionTitle(UnderheartIds.RegionOfAnchor(s.DeepAnchor))) + ".", 2.5f);
            _snapAtmosphere = true;
            StartLiftLoop(p);
            Game.Root.TravelTo(world, yaw);
        }

        private static bool UnderAttack()
        {
            PlayerController p = Game.Player;
            return p != null && p.Health != null && Time.time - p.Health.LastDamageTime < 3f && p.Health.LastDamageType != DamageType.Fall;
        }

        // ================================================================== shortcuts

        /// <summary>Opens a shortcut (persistent). False when unknown or already open.</summary>
        public bool UnlockShortcut(string id, bool announce)
        {
            if (string.IsNullOrEmpty(id)) return false;
            UhShortcut s = _layout != null ? _layout.Shortcut(id) : null;
            if (s == null && !UnderheartIds.HasShortcut(id)) return false;
            if (_shortcuts.Contains(id)) return false;
            _shortcuts.Add(id);
            Vector3 at = Game.Player != null ? Game.PlayerPosition : SurfaceEntrance;
            if (s != null && _prepared)
            {
                UhAnchor deep = _layout.Anchor(s.DeepAnchor);
                if (deep != null) at = _frame.ToWorld(deep.Pos);
            }
            Underheart04ESounds.Play(s != null && s.Kind == UhShortcutKind.Gate ? UhSfx.GateOpen : UhSfx.ShortcutOpen, at, 1f);
            if (announce && Game.Hud != null)
            {
                string title = s != null ? s.Title : id;
                string where = s == null ? "" : (s.Kind == UhShortcutKind.Gate ? "The way back to the Lantern Refuge is open." : "It now runs between the " + UnderheartIds.RegionTitle(UnderheartIds.RegionOfAnchor(s.DeepAnchor)) + " and the " + FarTitle(s) + ".");
                Game.Hud.Banner("SHORTCUT OPENED", title + ". " + where, UIKit.Cyan, 4.5f);
            }
            Log.Info("Underheart shortcut opened: " + id);
            RaiseShortcutOpened(id);
            return true;
        }

        /// <summary>Debug and reset only: closes a shortcut again.</summary>
        private void CloseShortcut(string id)
        {
            _shortcuts.Remove(id);
            ApplyShortcutStates(false);
        }
    }
}
