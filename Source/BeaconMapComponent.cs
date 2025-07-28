using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BeaconOfHope
{
    public class BeaconMapComponent : MapComponent
    {
        private HashSet<CompBeaconBroadcast> activeBeacons = new HashSet<CompBeaconBroadcast>();
        
        public IEnumerable<CompBeaconBroadcast> ActiveBeacons => activeBeacons;
        
        public BeaconMapComponent(Map map) : base(map)
        {
        }
        
        public override void MapComponentTick()
        {
            base.MapComponentTick();
            
            // Only update the list every 250 ticks (about 4 seconds) to save performance
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                RefreshActiveBeacons();
            }
        }
        
        public void RegisterBeacon(CompBeaconBroadcast beacon)
        {
            if (beacon != null && beacon.IsActive)
            {
                activeBeacons.Add(beacon);
            }
        }
        
        public void DeregisterBeacon(CompBeaconBroadcast beacon)
        {
            if (beacon != null)
            {
                activeBeacons.Remove(beacon);
            }
        }
        
        private void RefreshActiveBeacons()
        {
            // Clear the current list
            activeBeacons.Clear();
            
            // Find all beacon buildings on the map
            List<Thing> beaconBuildings = map.listerThings.ThingsOfDef(ThingDef.Named("BeaconOfHope")).ToList();
            
            // Add all active beacons to the list
            foreach (Thing building in beaconBuildings)
            {
                CompBeaconBroadcast comp = building.TryGetComp<CompBeaconBroadcast>();
                if (comp != null && comp.IsActive)
                {
                    activeBeacons.Add(comp);
                }
            }
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            // No need to save the activeBeacons collection as it's refreshed regularly
        }
    }
}
