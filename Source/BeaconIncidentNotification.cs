using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace BeaconOfHope
{
    [StaticConstructorOnStartup]
    public static class BeaconIncidentNotificationPatch
    {
        static BeaconIncidentNotificationPatch()
        {
            // Apply patches when the game loads
            Harmony harmony = new Harmony("BeaconOfHope.IncidentNotifications");
            harmony.PatchAll();
        }
    }
    
    // Patch IncidentWorker.TryExecute to add notifications for beacon-triggered events
    [HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
    public static class IncidentWorker_TryExecute_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(bool __result, IncidentParms parms, IncidentWorker __instance)
        {
            // Only proceed if the incident executed successfully
            if (!__result) return;
            
            // Get the incident def
            IncidentDef def = __instance.def;
            if (def == null) return;
            
            // Check if this incident was triggered by a beacon
            if (BeaconUtility.WasTriggeredByBeacon(def.defName))
            {
                // Reset the flag so it doesn't trigger again for the same incident
                BeaconUtility.ResetBeaconTrigger(def.defName);
                
                // Show a notification message
                string message = GetBeaconNotificationMessage(def);
                if (!message.NullOrEmpty())
                {
                    Messages.Message(message, MessageTypeDefOf.PositiveEvent);
                }
            }
        }
        
        // Get the appropriate notification message based on the incident type
        private static string GetBeaconNotificationMessage(IncidentDef incident)
        {
            if (incident == IncidentDefOf.WandererJoin)
            {
                return "Your Beacon of Hope has attracted a wanderer to your colony.";
            }
            else if (incident.defName == "RefugeePodCrash" || incident.defName == "RefugeeChased")
            {
                return "Your beacon signal has been picked up by refugees seeking shelter.";
            }
            else if (incident.defName == "ShipChunkDrop" || incident.defName.Contains("TransportPod"))
            {
                return "Your emergency channel has detected a distress signal from a crashing transport pod.";
            }
            else if (incident == IncidentDefOf.RaidEnemy)
            {
                return "Warning: Your beacon signal has been intercepted by hostile forces!";
            }
            
            return null;
        }
    }
    
    // No debug tools implementation - using gizmos on the beacon instead
}
