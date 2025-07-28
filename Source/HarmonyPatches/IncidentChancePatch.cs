using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace BeaconOfHope.HarmonyPatches
{
    [HarmonyPatch(typeof(StorytellerComp), "IncidentChanceFinal")]
    public static class IncidentChanceFinalPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ref float __result, IncidentDef def, IIncidentTarget target)
        {
            // Only apply to map targets
            if (!(target is Map map))
                return;
            
            // Get the multiplier for this incident type based on active beacons
            float multiplier = BeaconUtility.GetEventChanceMultiplier(map, def);
            
            // Apply the multiplier to the result
            __result *= multiplier;
        }
    }
    
    [HarmonyPatch(typeof(IncidentWorker_RaidEnemy), "GenerateRaidLoot")]
    public static class RaidPointsPatch
    {
        [HarmonyPrefix]
        public static void Prefix(IncidentParms parms)
        {
            // Only apply to map targets
            if (!(parms.target is Map map))
                return;
            
            // Get the multiplier for raid points based on active beacons
            float multiplier = BeaconUtility.GetRaidPointsMultiplier(map);
            
            // Apply the multiplier to the points
            parms.points *= multiplier;
        }
    }
    
    [HarmonyPatch(typeof(IncidentWorker_WandererJoin), "TryExecuteWorker")]
    public static class WandererJoinPatch
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result, IncidentParms parms)
        {
            // Only proceed if the incident was successful
            if (!__result)
                return;
                
            // Only apply to map targets
            if (!(parms.target is Map map))
                return;
                
            // Check if any beacons are in wanderer mode
            if (BeaconUtility.IsModeActiveOnMap(map, BeaconBroadcastMode.WandererMode))
            {
                // Send a message to the player
                Messages.Message("A wanderer has joined your colony, attracted by your Beacon of Hope.", 
                    MessageTypeDefOf.PositiveEvent);
            }
        }
    }
}
