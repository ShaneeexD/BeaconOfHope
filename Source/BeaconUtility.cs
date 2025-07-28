using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BeaconOfHope
{
    public static class BeaconUtility
    {
        // Track if an incident was triggered by a beacon
        private static Dictionary<string, bool> beaconTriggeredEvents = new Dictionary<string, bool>();
        
        // For testing: Force trigger an event through a command
        public static void TestTriggerEvent(Map map, string eventType)
        {
            if (map == null) return;
            
            IncidentDef incident = null;
            
            switch (eventType.ToLower())
            {
                case "wanderer":
                    incident = IncidentDefOf.WandererJoin;
                    break;
                case "refugee":
                    incident = DefDatabase<IncidentDef>.GetNamed("RefugeePodCrash", false);
                    break;
                case "pod":
                    incident = DefDatabase<IncidentDef>.GetNamed("ShipChunkDrop", false);
                    break;
                case "raid":
                    incident = IncidentDefOf.RaidEnemy;
                    break;
            }
            
            if (incident != null)
            {
                // Mark as beacon-triggered for notification
                MarkEventAsBeaconTriggered(incident.defName);
                
                // Execute the incident
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(incident.category, map);
                incident.Worker.TryExecute(parms);
            }
        }
        // Get all active beacons on a map
        public static IEnumerable<CompBeaconBroadcast> GetActiveBeacons(Map map)
        {
            if (map == null)
                return Enumerable.Empty<CompBeaconBroadcast>();
                
            return map.GetComponent<BeaconMapComponent>()?.ActiveBeacons ?? Enumerable.Empty<CompBeaconBroadcast>();
        }
        
        // Check if a specific broadcast mode is active on the map
        public static bool IsModeActiveOnMap(Map map, BeaconBroadcastMode mode)
        {
            return GetActiveBeacons(map).Any(b => b.CurrentMode == mode);
        }
        
        // Get the event chance multiplier based on active beacons
        public static float GetEventChanceMultiplier(Map map, IncidentDef incident)
        {
            // Check if this is a potential beacon-influenced event
            bool isPotentialBeaconEvent = 
                incident == IncidentDefOf.WandererJoin || 
                incident.defName == "RefugeePodCrash" ||
                incident.defName == "ShipChunkDrop" || 
                incident.defName.Contains("TransportPod");

            if (map == null)
                return 1f;
                
            // Default multiplier
            float multiplier = 1f;
            
            // Count active beacons by mode
            int wandererModeCount = 0;
            int emergencyChannelCount = 0;
            int openBroadcastCount = 0;
            
            foreach (var beacon in GetActiveBeacons(map))
            {
                switch (beacon.CurrentMode)
                {
                    case BeaconBroadcastMode.WandererMode:
                        wandererModeCount++;
                        break;
                    case BeaconBroadcastMode.EmergencyChannel:
                        emergencyChannelCount++;
                        break;
                    case BeaconBroadcastMode.OpenBroadcast:
                        openBroadcastCount++;
                        break;
                }
            }
            
            // Apply multipliers based on incident type and beacon modes
            if (incident == IncidentDefOf.WandererJoin)
            {
                // Wanderer mode increases wanderer join events
                multiplier += wandererModeCount * 0.5f;
                
                // Open broadcast slightly increases wanderer join events
                multiplier += openBroadcastCount * 0.2f;
            }
            else if (incident.defName == "RefugeePodCrash")
            {
                // Open broadcast significantly increases refugee events
                multiplier += openBroadcastCount * 0.75f;
                
                // Emergency channel slightly increases refugee events
                multiplier += emergencyChannelCount * 0.25f;
            }
            else if (incident.defName == "ShipChunkDrop" || incident.defName.Contains("TransportPod"))
            {
                // Emergency channel increases transport pod/ship part events
                multiplier += emergencyChannelCount * 0.6f;
            }
            else if (incident == IncidentDefOf.RaidEnemy)
            {
                // Open broadcast increases raid chance
                multiplier += openBroadcastCount * 0.4f;
            }
            
            // If the multiplier is greater than 1, it means beacons are affecting this event
            if (multiplier > 1f)
            {
                MarkEventAsBeaconTriggered(incident.defName);
            }
            
            return multiplier;
        }
        
        // Mark an event as being triggered by a beacon
        public static void MarkEventAsBeaconTriggered(string incidentDefName)
        {
            beaconTriggeredEvents[incidentDefName] = true;
        }
        
        // Check if an incident was triggered by a beacon
        public static bool WasTriggeredByBeacon(string incidentDefName)
        {
            return beaconTriggeredEvents.TryGetValue(incidentDefName, out bool triggered) && triggered;
        }
        
        // Reset the beacon trigger for an incident
        public static void ResetBeaconTrigger(string incidentDefName)
        {
            if (beaconTriggeredEvents.ContainsKey(incidentDefName))
            {
                beaconTriggeredEvents[incidentDefName] = false;
            }
        }
        
        // Calculate raid points multiplier based on beacon usage
        public static float GetRaidPointsMultiplier(Map map)
        {
            if (map == null)
                return 1f;
                
            // Count active beacons in open broadcast mode
            int openBroadcastCount = GetActiveBeacons(map)
                .Count(b => b.CurrentMode == BeaconBroadcastMode.OpenBroadcast);
            
            // No need to mark raid events here as they're handled in GetEventChanceMultiplier
                
            // Each open broadcast beacon increases raid points by 15%
            return 1f + (openBroadcastCount * 0.15f);
        }
    }
}
