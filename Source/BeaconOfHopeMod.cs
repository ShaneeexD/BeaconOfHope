using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace BeaconOfHope
{
    public class BeaconOfHopeMod : Mod
    {
        /// <summary>
        /// Harmony instance for the mod
        /// </summary>
        public static Harmony HarmonyInstance { get; private set; }
        
        public BeaconOfHopeMod(ModContentPack content) : base(content)
        {
            try
            {
                // Initialize Harmony
                HarmonyInstance = new Harmony("yourname.beaconofhope");
                
                // Log that the mod is initializing
                Log.Message("[Beacon of Hope] Initializing mod with Harmony patches...");
                
                // Apply patches
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                
                // Log that the mod has been initialized
                Log.Message("[Beacon of Hope] Mod initialized successfully with Harmony patches!");
            }
            catch (Exception ex)
            {
                Log.Error($"[Beacon of Hope] Error initializing mod: {ex}");
            }
        }
    }
}
