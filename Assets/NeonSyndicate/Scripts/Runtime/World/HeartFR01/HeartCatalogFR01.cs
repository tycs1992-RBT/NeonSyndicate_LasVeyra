using System;
using UnityEngine;
namespace NeonSyndicate
{
    [Serializable] public sealed class HeartModuleFR01
    { public string Id=""; public GameObject[] Lods; public Vector3 Size; }
    /// <summary>Passive prefabs prepared by the existing LiveWorld build lifecycle. No runtime filesystem.</summary>
    public sealed class HeartCatalogFR01 : ScriptableObject
    {
        public HeartModuleFR01[] Modules;
        public string Fingerprint="",Reason="Not prepared.";
        public HeartModuleFR01 Find(string id)
        { if(Modules!=null)foreach(HeartModuleFR01 m in Modules)if(m!=null && m.Id==id)return m;return null; }
        public bool Ready { get { return Find("HeartGate")!=null && Find("HeartHouseA")!=null && Find("HeartHouseB")!=null; } }
    }
}
