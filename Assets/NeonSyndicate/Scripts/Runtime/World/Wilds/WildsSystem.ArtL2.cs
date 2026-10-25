using UnityEngine;
namespace NeonSyndicate
{
    public sealed partial class WildsSystem
    {
        private const string ArtPreferenceL2="NSLV_redspire_authored_l2";
        private bool _readArtL2,_selectedArtL2,_compareArtL2;
        public bool SelectedArtL2 {get{ReadArtL2();return _selectedArtL2;}}
        public bool UseLiveArtL2 {get{return SelectedArtL2 && !_compareArtL2 && Game.Layout!=null && Game.Layout.GothicCivicH5;}}
        private void ReadArtL2(){if(!_readArtL2){_readArtL2=true;_selectedArtL2=PlayerPrefs.GetInt(ArtPreferenceL2,0)==1;}}
        public void ToggleArtL2()
        {
            ReadArtL2();_selectedArtL2=!_selectedArtL2;PlayerPrefs.SetInt(ArtPreferenceL2,_selectedArtL2?1:0);PlayerPrefs.Save();RefreshArtL2();
        }
        public void CompareArtL2(bool legacy)
        {if(_compareArtL2==legacy)return;_compareArtL2=legacy;RefreshArtL2();}
        private void RefreshArtL2()
        {
            if(_built==null)return;foreach(WildsChunkL1 chunk in _built.LiveChunks)if(chunk!=null)chunk.SetArtL2(UseLiveArtL2);
            Log.Info("LIVE_WILDS "+LiveWildsStateL2.Summary(_built.LiveChunks,_plan!=null?_plan.Pieces.Count:0));
        }
        public string ArtStatusL2
        {
            get
            {
                LiveWildsRegistryL2 kit=LiveWildsRegistryL2.Loaded;
                return (_selectedArtL2?"Owner opt-in ON":"Owner opt-in OFF")+(_compareArtL2?", temporary legacy comparison":"")+". "+(kit!=null?kit.Status:"Prepared kit absent; procedural fallback.");
            }
        }
        public string ArtMetricsL2 {get{return LiveWildsStateL2.Summary(_built!=null?_built.LiveChunks:null,_plan!=null?_plan.Pieces.Count:0);}}
        /// <summary>A real safe adjacent viewpoint near the densest existing Redspire site corridor, no new site/receipt.</summary>
        public bool ArtWalkL2(out Vector3 at,out string label)
        {
            at=Vector3.zero;label="Redspire art walk is not ready.";
            LiveWildsRegistryL2 kit=LiveWildsRegistryL2.Loaded;if(_plan==null || _built==null || kit==null)return false;
            System.Collections.Generic.HashSet<int> excluded=new System.Collections.Generic.HashSet<int>();
            foreach(WildsChunkL1 chunk in _built.LiveChunks)if(chunk!=null)chunk.ExclusionsL2(excluded);
            bool[] installed={kit.Find(WildPieceKind.Boulder)!=null,kit.Find(WildPieceKind.RockSpire)!=null,kit.Find(WildPieceKind.DeadTree)!=null};
            if(!LiveWildsRulesL2.Walk(_plan,installed,excluded,out at,out label))return false;
            // The existing ground query and native overlap refuse an unsafe viewpoint rather than teleporting there.
            at=PedManager.Ground(at+Vector3.up*60);
            if(Physics.CheckCapsule(at+Vector3.up*.5f,at+Vector3.up*1.8f,.4f,Layers.WorldMask,QueryTriggerInteraction.Ignore))return false;
            return true;
        }
    }
}
