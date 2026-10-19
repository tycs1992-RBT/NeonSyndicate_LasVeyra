using UnityEngine;
namespace NeonSyndicate
{
    public sealed class FieldSource04M
    {
        public readonly string Id,Title,At,Material;
        public readonly int Count;
        public readonly bool Underground;
        public readonly Vector3 Offset;
        public FieldSource04M(string id,string title,string at,string material,int count,bool underground,Vector3 offset)
        {Id=id;Title=title;At=at;Material=material;Count=count;Underground=underground;Offset=offset;}
        public string Receipt { get {return "field_04m_"+Id;} }
    }
    public static class FieldSources04M
    {
        // Finite recoveries, once per save, deliberately not a respawning extraction farm or a boss-material vendor.
        public static readonly FieldSource04M[] All={
            new FieldSource04M("pressure_salt","Pressure House condensate","Venue:pressure.activity","coolant_salt",4,false,new Vector3(-1.8f,0,0)),
            new FieldSource04M("garden_seed","Garden shed seed","Under:gardens.habitat","seed_core",2,true,new Vector3(2,0,0)),
            new FieldSource04M("workshop_cog","Workshop salvage drawer","Under:workshops.store_room","regulator_cog",2,true,new Vector3(-2,0,0)),
            new FieldSource04M("quarry_shard","Quarry survey scree","Site:Quarry","shard_chunk",4,false,new Vector3(95,0,-100)),
            new FieldSource04M("outpost_frost","Outpost cold trap","Site:WardenOutpost","frost_essence",3,false,new Vector3(12,0,-24)),
            new FieldSource04M("canyon_dust","Canyon shed crystal","Site:Canyon","mite_dust",4,false,new Vector3(-70,0,-75))
        };
        public static int PageSize(float height){return Mathf.Clamp((int)((height-170f)/90f),1,6);}
        public static bool CanRecover(bool claimed,bool alive,bool onFoot,bool correctLayer,bool playing,bool safe,float distance,float height)
        {return !claimed && alive && onFoot && correctLayer && playing && safe && distance<=2.2f && Mathf.Abs(height)<=1.4f;}
    }
}
