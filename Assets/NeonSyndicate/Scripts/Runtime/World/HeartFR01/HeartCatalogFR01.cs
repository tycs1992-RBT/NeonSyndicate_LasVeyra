using System;
using UnityEngine;
namespace NeonSyndicate
{
    [Serializable] public sealed class HeartModuleFR01
    { public string Id=""; public GameObject[] Lods; public Vector3 Size,Center; }
    /// <summary>Passive prefabs prepared by the existing LiveWorld build lifecycle. No runtime filesystem.</summary>
    public sealed class HeartCatalogFR01 : ScriptableObject
    {
        public HeartModuleFR01[] Modules;
        public string Fingerprint="",Reason="Not prepared.";
        public HeartModuleFR01 Find(string id)
        { if(Modules!=null)foreach(HeartModuleFR01 m in Modules)if(m!=null && m.Id==id)return m;return null; }
        private bool Complete(string id)
        {HeartModuleFR01 m=Find(id);if(m==null || m.Lods==null || m.Lods.Length!=3)return false;foreach(GameObject p in m.Lods)if(p==null)return false;return true;}
        public bool Ready { get { return Complete("HeartGate") && Complete("HeartHouseA") && Complete("HeartHouseB"); } }
        public bool RewriteReady { get { return Complete("WRArchiveCrown") && Complete("WROfficeCrown") && Complete("WRSkyline809556") && Complete("WRSkyline32874") && Complete("WRSkyline53496"); } }
        public bool LivingReady
        {
            get
            {
                foreach(string id in new[]{"StreetTownhouse","StreetResidence","StreetMerchant","StreetWorkshop","StreetGuild","StreetCourtRow","StreetRuin","StreetCorner","StreetGallery","CourtMarket","CourtWorkyard","CourtGarden","CourtLantern"})if(!Complete(id))return false;
                return true;
            }
        }
    }
}
