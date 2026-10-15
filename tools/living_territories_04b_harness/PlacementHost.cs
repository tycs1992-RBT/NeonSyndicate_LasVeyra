// Offline exclusion boundary only. A native call throws instead of fabricating terrain or a successful placement.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public struct Quaternion { public static Quaternion identity { get { return new Quaternion(); } } }
    public struct RaycastHit { public Vector3 point, normal; }
    public enum QueryTriggerInteraction { Ignore }
    public static class Physics
    {
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float distance, int mask, QueryTriggerInteraction query)
        { throw new NativeSurveyRequired(); }
        public static bool CheckBox(Vector3 center, Vector3 half, Quaternion rotation, int mask, QueryTriggerInteraction query)
        { throw new NativeSurveyRequired(); }
    }
    public sealed class NativeSurveyRequired : Exception { }
}
namespace NeonSyndicate
{
    public static class Layers { public const int WorldMask = 1; }
    public sealed class Interactable
    {
        public static readonly List<Interactable> All = new List<Interactable>();
        public float Radius;
        public Transform transform = new Transform();
    }
    public sealed class Transform { public UnityEngine.Vector3 position; }
}
