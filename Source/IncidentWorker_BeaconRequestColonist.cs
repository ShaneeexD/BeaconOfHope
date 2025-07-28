using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace BeaconOfHope
{
    public class IncidentWorker_BeaconRequestColonist : IncidentWorker
    {
        // Silver cost to request a colonist
        private const int BaseSilverCost = 800;
        
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Check if player has researched the faction signal booster
            if (!ResearchProjectDef.Named("FactionSignalBoosterResearch").IsFinished)
                return false;
                
            // Check if there's at least one beacon with power
            if (!map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("BeaconOfHope"))
                .Any(b => b.GetComp<CompPowerTrader>()?.PowerOn == true))
                return false;
                
            // Check if player has enough silver
            if (map.resourceCounter.Silver < BaseSilverCost)
                return false;
                
            return true;
        }
        
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Find a random beacon to use
            Building beacon = map.listerBuildings.AllBuildingsColonistOfDef(ThingDef.Named("BeaconOfHope"))
                .Where(b => b.GetComp<CompPowerTrader>()?.PowerOn == true)
                .RandomElement();
                
            if (beacon == null)
                return false;
                
            // Calculate actual silver cost based on colony wealth and population
            int silverCost = CalculateSilverCost(map);
            
            // Show dialog to confirm the request
            DiaNode diaNode = new DiaNode(
                $"Using the advanced signal booster technology, you can request a colonist from friendly factions at a cost of {silverCost} silver. " +
                "The colonist's skills and traits will be random, but they will arrive with positive feelings toward your colony.\n\n" +
                "Would you like to proceed with the request?");
                
            DiaOption optionAccept = new DiaOption("Accept");
            optionAccept.action = delegate
            {
                // Deduct silver
                Thing silver = map.resourceCounter.Silver >= silverCost ?
                    ThingMaker.MakeThing(ThingDefOf.Silver) : null;
                    
                if (silver == null)
                {
                    Messages.Message("Cannot afford to request colonist. Need " + silverCost + " silver.", 
                        MessageTypeDefOf.RejectInput);
                    return;
                }
                
                silver.stackCount = silverCost;
                silver.Destroy();
                
                // Generate pawn
                Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.OutlanderCivil);
                Pawn pawn = GenerateColonist(map, faction);
                
                if (pawn != null)
                {
                    // Spawn the pawn near the beacon
                    IntVec3 spawnLoc = CellFinder.RandomClosewalkCellNear(beacon.Position, map, 5);
                    GenSpawn.Spawn(pawn, spawnLoc, map);
                    
                    // Add special thought
                    pawn.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("ArrivedViaBeacon"));
                    
                    // Display message
                    string text = $"{pawn.LabelShort} has arrived in response to your beacon signal.";
                    Messages.Message(text, pawn, MessageTypeDefOf.PositiveEvent);
                    
                    // Flash effect at beacon
                    FleckMaker.ThrowLightningGlow(beacon.DrawPos, map, 2f);
                }
                else
                {
                    // Refund silver if pawn generation failed
                    Thing refundSilver = ThingMaker.MakeThing(ThingDefOf.Silver);
                    refundSilver.stackCount = silverCost;
                    GenPlace.TryPlaceThing(refundSilver, beacon.Position, map, ThingPlaceMode.Near);
                    
                    Messages.Message("Failed to contact a new colonist. Your silver has been refunded.", 
                        MessageTypeDefOf.NegativeEvent);
                }
            };
            
            DiaOption optionReject = new DiaOption("Cancel");
            optionReject.resolveTree = true;
            
            diaNode.options.Add(optionAccept);
            diaNode.options.Add(optionReject);
            
            Find.WindowStack.Add(new Dialog_NodeTree(diaNode, true));
            
            return true;
        }
        
        private int CalculateSilverCost(Map map)
        {
            // Base cost
            int cost = BaseSilverCost;
            
            // Increase cost based on colony wealth
            float wealthMultiplier = Mathf.Clamp(map.wealthWatcher.WealthTotal / 100000f, 1f, 3f);
            cost = Mathf.RoundToInt(cost * wealthMultiplier);
            
            // Increase cost based on colonist count
            int colonistCount = map.mapPawns.FreeColonistsCount;
            float colonistMultiplier = Mathf.Clamp(colonistCount / 10f, 1f, 2.5f);
            cost = Mathf.RoundToInt(cost * colonistMultiplier);
            
            return cost;
        }
        
        private Pawn GenerateColonist(Map map, Faction faction)
        {
            // Generate a new colonist
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind: PawnKindDefOf.Colonist,
                faction: faction,
                context: PawnGenerationContext.NonPlayer,
                tile: map.Tile,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: true,
                mustBeCapableOfViolence: false,
                colonistRelationChanceFactor: 1f,
                forceAddFreeWarmLayerIfNeeded: true,
                allowGay: true,
                allowPregnant: true,
                allowFood: true,
                allowAddictions: true,
                inhabitant: false);
                
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            
            if (pawn != null)
            {
                // Set faction to player's faction
                pawn.SetFaction(Faction.OfPlayer);
                
                // Ensure the pawn is healthy
                pawn.health.Reset();
                
                // Give them some starting equipment
                PawnInventoryGenerator.GenerateInventoryFor(pawn, request);
            }
            
            return pawn;
        }
    }
}
