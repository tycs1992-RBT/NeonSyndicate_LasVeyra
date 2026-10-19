namespace NeonSyndicate
{
    public static class KingdomCombatRules04N
    {
        /// <summary>Breaking the named Foreman or Djinn's tell earns its existing exposed-core recovery, not immediate melee.</summary>
        public static bool ExposeInterruptedBoss(HumanoidCreature kind,bool telegraph,bool realInterrupt)
        {return telegraph && realInterrupt && (kind==HumanoidCreature.GolemForeman || kind==HumanoidCreature.MirageDjinn);}
        /// <summary>The same geometry blocks planning, physical movement and recall admission.</summary>
        public static int CompanionObstacleMask {get{return Layers.WorldMask | (1<<Layers.Vehicles) | (1<<Layers.VehicleShell) | (1<<Layers.Debris);}}
    }
}
