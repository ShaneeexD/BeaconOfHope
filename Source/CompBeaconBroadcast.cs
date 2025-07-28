using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace BeaconOfHope
{
    public class CompBeaconBroadcast : ThingComp
    {
        // Current broadcast mode
        private BeaconBroadcastMode currentMode = BeaconBroadcastMode.Off;
        
        // Time since last event was triggered by this beacon
        private int ticksSinceLastEvent = 0;
        
        // Cooldown between events (in ticks)
        private const int MinTicksBetweenEvents = GenDate.TicksPerDay * 3; // 3 days by default
        
        // Random factor for event triggering
        private const float BaseEventChance = 0.01f; // Base chance per day when active
        
        public BeaconBroadcastMode CurrentMode => currentMode;
        
        public bool IsActive => currentMode != BeaconBroadcastMode.Off && parent.GetComp<CompPowerTrader>()?.PowerOn == true;
        
        public CompProperties_BeaconBroadcast Props => (CompProperties_BeaconBroadcast)props;
        
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentMode, "currentMode", BeaconBroadcastMode.Off);
            Scribe_Values.Look(ref ticksSinceLastEvent, "ticksSinceLastEvent", 0);
        }
        
        public override void CompTick()
        {
            base.CompTick();
            
            if (!IsActive)
                return;
            
            ticksSinceLastEvent++;
            
            // Only check for events if cooldown has passed
            if (ticksSinceLastEvent >= MinTicksBetweenEvents)
            {
                // Daily chance check (assuming 60 ticks per second × 60 seconds × 24 hours = 86400 ticks per day)
                if (Rand.MTBEventOccurs(1f / BaseEventChance, GenDate.TicksPerDay, 1))
                {
                    TryTriggerEvent();
                }
            }
        }
        
        private void TryTriggerEvent()
        {
            if (!IsActive)
                return;
            
            Map map = parent.Map;
            if (map == null)
                return;
            
            IncidentDef incidentDef = null;
            
            // Select incident based on current mode
            switch (currentMode)
            {
                case BeaconBroadcastMode.WandererMode:
                    incidentDef = IncidentDefOf.WandererJoin;
                    break;
                    
                case BeaconBroadcastMode.EmergencyChannel:
                    // Use transport pod crash incident
                    incidentDef = DefDatabase<IncidentDef>.GetNamed("ShipChunkDrop");
                    break;
                    
                case BeaconBroadcastMode.OpenBroadcast:
                    // For open broadcast, we have a chance of either refugees or raids
                    if (Rand.Value < 0.7f) // 70% chance for refugees
                    {
                        incidentDef = DefDatabase<IncidentDef>.GetNamed("RefugeePodCrash");
                    }
                    else // 30% chance for raid
                    {
                        incidentDef = IncidentDefOf.RaidEnemy;
                    }
                    break;
            }
            
            if (incidentDef != null)
            {
                // Try to create and execute the incident
                IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);
                if (incidentDef.Worker.CanFireNow(parms))
                {
                    incidentDef.Worker.TryExecute(parms);
                    ticksSinceLastEvent = 0;
                    
                    // Notify player
                    Messages.Message($"Beacon of Hope has triggered a {incidentDef.label} event.", 
                        parent, MessageTypeDefOf.NeutralEvent);
                }
            }
        }
        
        public void SetMode(BeaconBroadcastMode newMode)
        {
            if (currentMode != newMode)
            {
                currentMode = newMode;
                
                // Reset cooldown when changing modes
                if (newMode != BeaconBroadcastMode.Off)
                {
                    ticksSinceLastEvent = 0;
                }
                
                // Notify player of mode change
                string modeDesc = GetModeDescription(newMode);
                Messages.Message($"Beacon of Hope mode changed to: {modeDesc}", parent, MessageTypeDefOf.NeutralEvent);
            }
        }
        
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            // Power check gizmo from parent
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }
            
            // Only show mode selection if powered
            if (parent.GetComp<CompPowerTrader>()?.PowerOn != true)
                yield break;
            
            // Mode selection gizmo
            yield return new Command_Action
            {
                defaultLabel = "Set Broadcast Mode",
                defaultDesc = "Configure the beacon's broadcast mode.",
                icon = ContentFinder<Texture2D>.Get("UI/Commands/SetTargetFuelLevel"),
                action = () => Find.WindowStack.Add(new Dialog_BeaconModeSelection(this))
            };
            
            // Display current mode
            yield return new Command_Action
            {
                defaultLabel = $"Current Mode: {GetModeDescription(currentMode)}",
                defaultDesc = "The current broadcast mode of this beacon.",
                action = () => {}
            };
            
            // Debug test gizmos - only show in dev mode
            if (Prefs.DevMode)
            {
                yield return new Command_Action
                {
                    defaultLabel = "Debug: Test Events",
                    defaultDesc = "Test beacon events (Dev Mode only).",
                    action = () => {
                        List<FloatMenuOption> options = new List<FloatMenuOption>();
                        
                        options.Add(new FloatMenuOption("Test Wanderer Join", () => {
                            BeaconUtility.TestTriggerEvent(parent.Map, "wanderer");
                        }));
                        
                        options.Add(new FloatMenuOption("Test Refugee Pod", () => {
                            BeaconUtility.TestTriggerEvent(parent.Map, "refugee");
                        }));
                        
                        options.Add(new FloatMenuOption("Test Transport Pod", () => {
                            BeaconUtility.TestTriggerEvent(parent.Map, "pod");
                        }));
                        
                        options.Add(new FloatMenuOption("Test Raid", () => {
                            BeaconUtility.TestTriggerEvent(parent.Map, "raid");
                        }));
                        
                        options.Add(new FloatMenuOption("Test Lost Specialist", () => {
                            BeaconUtility.TestTriggerEvent(parent.Map, "specialist");
                        }));
                        
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                };
            }
        }
        
        public static string GetModeDescription(BeaconBroadcastMode mode)
        {
            switch (mode)
            {
                case BeaconBroadcastMode.Off:
                    return "Off";
                case BeaconBroadcastMode.WandererMode:
                    return "Wanderer Mode";
                case BeaconBroadcastMode.EmergencyChannel:
                    return "Emergency Channel";
                case BeaconBroadcastMode.OpenBroadcast:
                    return "Open Broadcast";
                default:
                    return "Unknown";
            }
        }
    }
    
    public class CompProperties_BeaconBroadcast : CompProperties
    {
        public bool isAdvanced = false;
        public bool isFactionBooster = false;
        
        public CompProperties_BeaconBroadcast()
        {
            this.compClass = typeof(CompBeaconBroadcast);
        }
    }
    
    public enum BeaconBroadcastMode
    {
        Off,
        WandererMode,
        EmergencyChannel,
        OpenBroadcast
    }
}
