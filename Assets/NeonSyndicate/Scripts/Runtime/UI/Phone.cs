using System.Collections.Generic;
using UnityEngine;

namespace NeonSyndicate
{
    /// <summary>
    /// Malik's phone (P): call contacts (Jax delivers cars, Officer Valez makes warrants disappear, Iris sends
    /// contracts), check side jobs, have a courier bring your hover bike, and take photos. Arrow keys, the D-pad or the
    /// mouse navigate; Backspace, B/Circle or P closes.
    /// </summary>
    public sealed class Phone : MonoBehaviour
    {
        private enum App { Home, Contacts, Jobs, Camera }

        public bool IsOpen { get; private set; }
        private App _app = App.Home;
        private int _sel;
        private float _anim;
        private string _message;
        private float _messageTime;
        private float _callCooldown;
        /// <summary>The home screen, in order (Activate maps the index: 2 is Bring my bike).</summary>
        private static readonly string[] HomeApps = { "Contacts", "Jobs", "Bring my bike", "Camera" };

        private sealed class Contact
        {
            public string Name;
            public int UnlockAt;
            public System.Action Call;
        }

        private List<Contact> Contacts()
        {
            int sp = Game.Missions != null ? Game.Missions.StoryProgress : 0;
            if (Game.Root != null && Game.Root.FreeRoam) sp = 99;
            List<Contact> all = new List<Contact>
            {
                new Contact { Name = "Luma", UnlockAt = 0, Call = () => Say("Luma", "Dad, the stones are not bad. They are scared too. Come home soon, okay?") },
                new Contact { Name = "Mara Solane", UnlockAt = 1, Call = () => Say("Mara Solane", "If you're bleeding, the clinic in Sunken Row is free for family. Don't make me come find you.") },
                new Contact { Name = "Ion Solane", UnlockAt = 1, Call = () => Say("Ion Solane", "I can build a little moon if you give me enough broken lights. Also, the radio masts in Grave Frequency hate us.") },
                new Contact { Name = "Jax Calder", UnlockAt = 2, Call = JaxDelivery },
                new Contact { Name = "Nico Ash", UnlockAt = 3, Call = () => Say("Nico Ash", "Street races are open. Find the white F flags on your map and bring something fast.") },
                new Contact { Name = "Dante Rook", UnlockAt = 4, Call = () => Say("Dante Rook", "One last impossible job, I said. I lied. There's always another one.") },
                new Contact { Name = "June Pyre", UnlockAt = 6, Call = () => Say("June Pyre", "Power always costs something, Malik. Make sure you're the one deciding the price.") },
                new Contact { Name = "Iris Moon", UnlockAt = 7, Call = IrisContract },
                new Contact { Name = "Officer Valez", UnlockAt = 8, Call = ValezBribe },
                new Contact { Name = "Sable Knox", UnlockAt = 10, Call = () => Say("Sable Knox", "No hard feelings about the garage. Okay, some hard feelings.") },
                new Contact { Name = "The Collector", UnlockAt = 1, Call = CollectorCall },
            };
            all.RemoveAll(c => c.UnlockAt > sp);
            return all;
        }

        private void Say(string who, string line)
        {
            if (Game.Hud != null) Game.Hud.Subtitle(who, line, 5f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.PhoneRing, 0.3f, 1.3f);
            Close();
        }

        private void JaxDelivery()
        {
            if (Game.Missions != null && Game.Missions.Active != null) { Say("Jax Calder", "You're working. Call me when the heat's off."); return; }
            if (Game.Player.InVehicle) { Say("Jax Calder", "You've already got wheels, genius."); return; }
            if (!Game.Economy.Spend(1000, "Delivery")) { Close(); return; }
            VehicleDef[] picks = { VehicleDb.Find("Vanta Arrow"), VehicleDb.Find("Torque Falcon"), VehicleDb.Find("Mistral Comet"), VehicleDb.Find("Anvil Ridge") };
            VehicleDef d = picks[Random.Range(0, picks.Length)] ?? VehicleDb.All[0];
            if (Game.Cheats != null) Game.Cheats.SpawnVehicle(d);
            Say("Jax Calder", "One " + d.Name + ", parked right by you. Try to bring it back in one piece. You won't.");
        }

        /// <summary>Bring my bike: the courier drops your last bike (or a courier bike) 20 to 40 m away (TrafficManager.BringPlayerBike).</summary>
        private void BringBike()
        {
            string line = Game.Traffic != null ? Game.Traffic.BringPlayerBike() : "No courier in this city.";
            Say("Bike courier", line);
        }

        /// <summary>The Collector (Phase 5 loot world): rewards for hidden satchels found, and the day's bounty board.</summary>
        private void CollectorCall()
        {
            string line = Game.LootWorld != null ? Game.LootWorld.CallCollector() : "Not now. Call me when the city settles.";
            if (Game.Hud != null) Game.Hud.Subtitle("The Collector", line, 9f);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.PhoneRing, 0.3f, 1.1f);
            Close();
        }

        private void IrisContract()
        {
            if (Game.Activities == null) return;
            bool ok = Game.Activities.RequestContract();
            Say("Iris Moon", ok ? "A payphone near you is about to ring. Every secret is currency, Malik. Spend wisely." : "No contracts right now. Finish what you're doing.");
        }

        private void ValezBribe()
        {
            int stars = Game.Wanted != null ? Game.Wanted.Stars : 0;
            if (stars == 0) { Say("Officer Valez", "Your record's clean. For now."); return; }
            int price = stars * 1000;
            if (!Game.Economy.Spend(price, "Bribe")) { Close(); return; }
            Game.Wanted.ClearAll();
            if (Game.Police != null) Game.Police.ClearAll();
            Say("Officer Valez", "Dispatch just lost your file. That's " + MathUtil.Money(price) + " you never gave me.");
        }

        // ------------------------------------------------------------------ input

        private void Update()
        {
            _callCooldown -= Time.unscaledDeltaTime;
            _messageTime -= Time.unscaledDeltaTime;
            _anim = Mathf.MoveTowards(_anim, IsOpen ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            if (!Game.IsPlaying && !IsOpen) return;
            bool back = IsOpen && (GInput.Down(KeyCode.Backspace) || Pad.Down(PadButton.East));
            if (Act.Phone || (back && _app == App.Home))
            {
                if (IsOpen) Close();
                else if (Game.IsPlaying && (Game.Hud == null || !Game.Hud.WheelOpen) && (Game.Menus == null || !Game.Menus.IsOpen) && Game.Player != null && !Game.Player.IsDead) Open();
                return;
            }
            if (!IsOpen) return;
            if (!Game.IsPlaying || Game.Player == null || Game.Player.IsDead) { Close(); return; }
            int count = ItemCount();
            int nav = GInput.NavDirection();
            if (nav == 1) _sel = (_sel - 1 + count) % Mathf.Max(1, count);
            if (nav == 2) _sel = (_sel + 1) % Mathf.Max(1, count);
            if (GInput.UiConfirm) Activate(_sel);
            if (back && _app != App.Home) { _app = App.Home; _sel = 0; }
        }

        private int ItemCount()
        {
            switch (_app)
            {
                case App.Home: return HomeApps.Length;
                case App.Contacts: return Contacts().Count;
                default: return 1;
            }
        }

        private void Open()
        {
            IsOpen = true;
            _app = App.Home;
            _sel = 0;
            GInput.GameplayBlocked = true;
            GInput.SetCursorLocked(false);
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.PhoneOpen, 0.6f);
            if (Game.Player != null) Game.Player.Anim.Phone = true;
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            GInput.GameplayBlocked = false;
            GInput.LatchMouse();
            if (Game.IsPlaying) GInput.SetCursorLocked(true);
            if (Game.Player != null) Game.Player.Anim.Phone = false;
        }

        private void Activate(int i)
        {
            if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIClick, 0.5f);
            switch (_app)
            {
                case App.Home:
                    if (i == 2)
                    {
                        BringBike();
                        break;
                    }
                    _app = i == 0 ? App.Contacts : (i == 1 ? App.Jobs : App.Camera);
                    _sel = 0;
                    break;
                case App.Contacts:
                {
                    List<Contact> list = Contacts();
                    if (i >= 0 && i < list.Count && _callCooldown <= 0f)
                    {
                        _callCooldown = 1f;
                        list[i].Call();
                    }
                    break;
                }
                case App.Camera:
                    TakePhoto();
                    break;
                default:
                    _app = App.Home;
                    break;
            }
        }

        public static void TakePhoto()
        {
            try
            {
                string dir = System.IO.Path.Combine(Application.persistentDataPath, "Photos");
                System.IO.Directory.CreateDirectory(dir);
                string file = System.IO.Path.Combine(dir, "LasVeyra_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                ScreenCapture.CaptureScreenshot(file);
                if (Game.Hud != null) Game.Hud.Notify("Photo saved to " + file, 4f);
                if (Game.Audio != null) Game.Audio.Play2D(SfxId.UIClick, 0.8f, 0.6f);
            }
            catch (System.Exception e)
            {
                Log.Exception("Photo", e);
            }
        }

        // ------------------------------------------------------------------ drawing

        private void OnGUI()
        {
            if (_anim <= 0.01f) return;
            GUI.depth = 5;
            UIKit.BeginScaled();
            float W = UIKit.Width, H = UIKit.Height;
            float pw = 360f, ph = 620f;
            float x = W - pw - 60f;
            float y = H - ph * _anim - 20f;
            Rect body = new Rect(x, y, pw, ph);
            UIKit.Rect(new Rect(x - 10f, y - 10f, pw + 20f, ph + 20f), new Color(0.05f, 0.05f, 0.06f, 0.98f));
            UIKit.Rect(body, new Color(0.08f, 0.1f, 0.16f, 1f));
            UIKit.Text(new Rect(x + 16f, y + 10f, pw - 32f, 24f), Game.Atmo != null ? Game.Atmo.ClockText : "", 18, Color.white, TextAnchor.UpperLeft, FontStyle.Bold, false);
            UIKit.Text(new Rect(x + 16f, y + 10f, pw - 32f, 24f), "LVNet  ||||", 18, Color.white, TextAnchor.UpperRight, FontStyle.Bold, false);
            string title = _app == App.Home ? "VEYRA OS" : _app.ToString().ToUpper();
            UIKit.Text(new Rect(x, y + 44f, pw, 40f), title, 30, UIKit.Cyan, TextAnchor.UpperCenter, FontStyle.Bold, false);
            float iy = y + 100f;
            Event e = Event.current;
            switch (_app)
            {
                case App.Home:
                {
                    string[] apps = HomeApps;
                    for (int i = 0; i < apps.Length; i++)
                    {
                        if (Item(new Rect(x + 20f, iy, pw - 40f, 64f), apps[i], i == _sel, e)) Activate(i);
                        iy += 74f;
                    }
                    UIKit.Text(new Rect(x + 20f, y + ph - 70f, pw - 40f, 60f), "Arrows + Enter or click. Backspace: back. P: close.", 16, new Color(0.7f, 0.7f, 0.7f), TextAnchor.UpperCenter, FontStyle.Normal, false, true);
                    break;
                }
                case App.Contacts:
                {
                    List<Contact> list = Contacts();
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (Item(new Rect(x + 20f, iy, pw - 40f, 44f), list[i].Name, i == _sel, e)) Activate(i);
                        iy += 50f;
                    }
                    break;
                }
                case App.Jobs:
                {
                    string s = Game.Activities != null ? Game.Activities.Summary() : "";
                    UIKit.Text(new Rect(x + 20f, iy, pw - 40f, ph - 160f), s, 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                    break;
                }
                case App.Camera:
                {
                    UIKit.Text(new Rect(x + 20f, iy, pw - 40f, 120f), "Press Enter (or click) to take a photo. Photos are saved next to your save games. F12 also works any time.", 18, Color.white, TextAnchor.UpperLeft, FontStyle.Normal, false, true);
                    if (Item(new Rect(x + 20f, iy + 140f, pw - 40f, 64f), "Take photo", true, e)) TakePhoto();
                    break;
                }
            }
        }

        private static bool Item(Rect r, string label, bool selected, Event e)
        {
            bool hover = r.Contains(e.mousePosition);
            UIKit.Rect(r, selected ? new Color(0.3f, 0.95f, 1f, 0.9f) : (hover ? new Color(1f, 1f, 1f, 0.15f) : new Color(1f, 1f, 1f, 0.06f)));
            UIKit.Text(new Rect(r.x + 16f, r.y, r.width - 32f, r.height), label, 22, selected ? Color.black : Color.white, TextAnchor.MiddleLeft, FontStyle.Bold, false);
            if (e.type == EventType.MouseDown && e.button == 0 && hover)
            {
                e.Use();
                return true;
            }
            return false;
        }
    }
}
