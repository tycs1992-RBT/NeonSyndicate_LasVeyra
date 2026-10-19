using System;
using UnityEngine;

namespace NeonSyndicate
{
    public enum SagaEmotion04L { Resolve, Wonder, Concern, Warmth, Defiance, Grief }
    public enum SagaShot04L { TwoShot, Shoulder, Close, Pullback, Reveal, Tracking }

    /// <summary>Authored film and acting data, sampled by the existing mission and pose owners. No saved state.</summary>
    public static class SagaDirection04L
    {
        public static int Chapter(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length < 4 || id[0] != 'm' || id[3] != '_') return -1;
            int n = (id[1] - '0') * 10 + id[2] - '0';
            return n >= 0 && n < 12 ? n : -1;
        }
        public static bool Remote(string speaker)
        {
            return string.IsNullOrEmpty(speaker) || speaker.IndexOf('(') >= 0 || speaker == "Third Eye";
        }
        public static SagaEmotion04L Emotion(string speaker, int chapter)
        {
            if (speaker == "Luma") return chapter == 10 ? SagaEmotion04L.Warmth : chapter == 11 ? SagaEmotion04L.Wonder : SagaEmotion04L.Concern;
            if (speaker == "Shard Warden" || speaker == "The False King") return SagaEmotion04L.Defiance;
            if (speaker == "Mara Solane" || speaker == "Corin Pell") return SagaEmotion04L.Warmth;
            if (speaker == "Ion Solane" || speaker == "Iris Moon") return SagaEmotion04L.Wonder;
            if (speaker == "June Pyre") return chapter >= 5 ? SagaEmotion04L.Grief : SagaEmotion04L.Resolve;
            if (speaker == "Officer Valez" || speaker == "Dante Rook") return SagaEmotion04L.Concern;
            return SagaEmotion04L.Resolve;
        }
        public static SagaShot04L Shot(int chapter, int line)
        {
            // A common 180-degree axis, different rhythm for the three acts; never cross the cast axis.
            if (line == 0) return chapter == 0 || chapter == 11 ? SagaShot04L.Reveal : SagaShot04L.TwoShot;
            if (chapter == 9 || chapter == 10) return line % 2 == 0 ? SagaShot04L.Close : SagaShot04L.Shoulder;
            if (chapter == 4 || chapter == 5 || chapter == 8) return line % 3 == 0 ? SagaShot04L.Pullback : SagaShot04L.Shoulder;
            return line % 3 == 0 ? SagaShot04L.Tracking : line % 2 == 0 ? SagaShot04L.Close : SagaShot04L.Shoulder;
        }
        public static float ReadSeconds(string text)
        {
            // Long lines retain enough reading time; the existing confirm/skip actions remain available.
            return Mathf.Clamp(1.6f + (text == null ? 0 : text.Length) * .06f, 2.5f, 14f);
        }
        public static float Envelope(float t, float duration)
        {
            if (duration <= 0f || float.IsNaN(t) || t < 0f || t >= duration) return 0f;
            return Math.Min(1f, Math.Min(t / .3f, (duration - t) / .35f));
        }
        public static void Acting(SagaEmotion04L mood, float t, bool speaking, bool child,
            out Vector3 chest, out Vector3 head, out Vector3 upper, out Vector3 fore)
        {
            float beat = speaking ? (float)Math.Sin(t * 3.1) : (float)Math.Sin(t * 1.2) * .25f;
            float hand = speaking ? (float)Math.Sin(Math.Min(1f, Math.Max(0f, t / .7f)) * Math.PI * .5) : .15f;
            chest = new Vector3(mood == SagaEmotion04L.Grief ? 4f : -1.5f, beat * 1.6f, beat * .7f);
            head = new Vector3(mood == SagaEmotion04L.Wonder ? -7f : mood == SagaEmotion04L.Grief ? 9f : beat * 2f, beat * 2.5f, 0f);
            upper = new Vector3(-28f - hand * 17f, 0f, 12f + hand * 8f);
            fore = new Vector3(-45f - hand * 25f + beat * 6f, 0f, 0f);
            if (mood == SagaEmotion04L.Concern) { upper.x -= 12f; fore.x -= 28f; }
            if (mood == SagaEmotion04L.Defiance) { chest.x = -4f; upper.z += 14f; fore.x += 12f; }
            // Luma's sheltered, close-to-body gestures never borrow an attack or adult stance.
            if (child) { upper *= .55f; fore.x = -82f + beat * 4f; chest *= .5f; }
        }
        public static void Frame(Vector3 playerHead, Vector3 actorHead, Vector3 axis, SagaShot04L shot,
            float elapsed, bool reduced, out Vector3 position, out Vector3 look, out float fov, bool playerSpeaking = false)
        {
            Vector3 along = MathUtil.Flat(axis);
            if (along.sqrMagnitude < .01f) along = Vector3.forward;
            along = along.normalized;
            Vector3 side = new Vector3(along.z, 0f, -along.x);
            Vector3 middle = (playerHead + actorHead) * .5f;
            float move = reduced ? 0f : Math.Min(1f, elapsed * .12f);
            fov = shot == SagaShot04L.Close ? 43f : 50f;
            look = middle - Vector3.up * .12f;
            position = middle + side * 3.6f + Vector3.up * .25f;
            Vector3 subject = playerSpeaking ? playerHead : actorHead;
            Vector3 listener = playerSpeaking ? actorHead : playerHead;
            Vector3 facing = playerSpeaking ? -along : along;
            switch (shot)
            {
                case SagaShot04L.Shoulder: look = subject; position = listener - facing * .9f + side * .75f; break;
                case SagaShot04L.Close: look = subject - Vector3.up * .08f; position = subject + side * 1.7f - facing * .65f; break;
                case SagaShot04L.Pullback: position += side * (1f + move * .65f); break;
                case SagaShot04L.Reveal: position += side * (1.4f - move * .7f) + Vector3.up * .5f; break;
                case SagaShot04L.Tracking: position += along * (move - .5f) * .5f; break;
            }
        }
        public static string Acknowledgement(string speaker, int progress)
        {
            // Derived exclusively from original MissionManager.StoryProgress: no extra saved story flags.
            if (speaker == "Luma") return progress >= 11 ? "You came back for me. Whatever the Manor asks, we answer together."
                : progress >= 1 ? "The stones are quieter here, Dad. I can hear you again." : "Stay close, Dad. The wall is listening.";
            if (speaker == "Mara Solane") return progress >= 11 ? "Luma is safe. Let that matter before the next fight."
                : progress >= 5 ? "Those couriers are breathing because you went back." : progress >= 1 ? "You both made it. This clinic keeps its door open." : null;
            if (speaker == "Ion Solane" && progress >= 9) return "Three transmitters silent. Nobody here misses that signal.";
            if (speaker == "Jax Calder" && progress >= 2) return "Still got the Arrow's paint in the drain. You earned that cut.";
            if (speaker == "June Pyre" && progress >= 6) return "The treaty failed. Remember the people, not the ashes.";
            if (speaker == "Iris Moon" && progress >= 7) return "A signal is a trail. Your family is more than a signal.";
            return null;
        }
    }
}
