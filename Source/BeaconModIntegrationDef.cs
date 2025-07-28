using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BeaconOfHope
{
    public class BeaconModIntegrationDef : Def
    {
        // Name of the mod this integration is for
        public string modName;
        
        // Description of what this integration does
        public new string description;
        
        // List of incident def names from the mod that are affected by beacons
        public List<string> affectedIncidents;
        
        public override string ToString()
        {
            return $"{defName}: {modName}";
        }
    }
    
    // Static class to handle mod integrations
    public static class ModIntegrationUtility
    {
        // Cache of loaded integrations
        private static List<BeaconModIntegrationDef> loadedIntegrations;
        
        // Initialize the integration system
        public static void Initialize()
        {
            loadedIntegrations = DefDatabase<BeaconModIntegrationDef>.AllDefsListForReading;
            
            if (loadedIntegrations.Count > 0)
            {
                Log.Message($"[Beacon of Hope] Loaded {loadedIntegrations.Count} mod integrations");
                
                foreach (var integration in loadedIntegrations)
                {
                    Log.Message($"[Beacon of Hope] Integration: {integration.modName} - {integration.description}");
                }
            }
        }
        
        // Check if an incident is affected by beacon integrations
        public static bool IsAffectedIncident(IncidentDef incident)
        {
            if (loadedIntegrations == null || loadedIntegrations.Count == 0)
                return false;
                
            foreach (var integration in loadedIntegrations)
            {
                if (integration.affectedIncidents != null && 
                    integration.affectedIncidents.Contains(incident.defName))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        // Get the multiplier for an integrated mod's incident
        public static float GetIntegratedIncidentMultiplier(IncidentDef incident, Map map)
        {
            if (!IsAffectedIncident(incident))
                return 1f;
                
            // Default multiplier
            float multiplier = 1f;
            
            // Count active beacons
            int activeBeaconCount = BeaconUtility.GetActiveBeacons(map).Count(b => true);
            
            if (activeBeaconCount > 0)
            {
                // Apply a multiplier based on the number of active beacons
                // More beacons = higher multiplier, but with diminishing returns
                multiplier += Math.Min(activeBeaconCount * 0.25f, 1.5f);
                
                // Special handling for specific mods
                if (incident.defName.StartsWith("VEE_"))
                {
                    // Vanilla Events Expanded
                    if (incident.defName.Contains("Reinforcement") || 
                        incident.defName.Contains("RefugeeGroup") ||
                        incident.defName.Contains("NewMember"))
                    {
                        multiplier += 0.2f;
                    }
                }
                else if (incident.defName.StartsWith("Hospitality_"))
                {
                    // Hospitality
                    multiplier += 0.3f;
                }
                else if (incident.defName.StartsWith("RimQuest_"))
                {
                    // RimQuest
                    multiplier += 0.25f;
                }
            }
            
            return multiplier;
        }
    }
}
