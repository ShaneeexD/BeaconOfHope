using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections;

namespace BeaconOfHope
{
    public class IncidentWorker_LostSpecialist : IncidentWorker
    {
        // Specialization types for the lost specialist
        private enum SpecialistType
        {
            Doctor,
            Researcher,
            Crafter,
            Miner,
            Farmer,
            Shooter
        }
        
        // Minimum skill level for the specialist's primary skills
        private const int MinPrimarySkillLevel = 12;
        
        // Chance for the specialist to have a hidden trait (addiction, health condition)
        private const float HiddenTraitChance = 0.3f;
        
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Only fire if there's an active beacon on the map
            if (!BeaconUtility.GetActiveBeacons(map).Any())
                return false;
                
            // Check if there's a valid spot to spawn the pawn
            IntVec3 spawnSpot;
            return TryFindEntryCell(map, out spawnSpot);
        }
        
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            
            // Mark as beacon-triggered for notification
            BeaconUtility.MarkEventAsBeaconTriggered(def.defName);
            
            // Find entry location
            IntVec3 spawnSpot;
            if (!TryFindEntryCell(map, out spawnSpot))
                return false;
                
            // Generate the specialist pawn
            Pawn specialist = GenerateSpecialist(map.Tile);
            if (specialist == null)
                return false;
                
            // Spawn the pawn
            GenSpawn.Spawn(specialist, spawnSpot, map, Rot4.South);
            
            // Add "found beacon" thought
            specialist.needs?.mood?.thoughts?.memories?.TryGainMemory(ThoughtDef.Named("SpecialistFoundBeacon"));
            
            // Send letter to player
            string letterLabel = "BeaconLostSpecialistArrived".Translate(specialist.skills.GetSkill(GetPrimarySkill(specialist)).def.label);
            string letterText = "BeaconLostSpecialistArrivedDesc".Translate(
                specialist.Name.ToStringShort,
                specialist.skills.GetSkill(GetPrimarySkill(specialist)).def.label,
                specialist.skills.GetSkill(GetPrimarySkill(specialist)).Level
            );
            
            Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent, specialist);
            
            return true;
        }
        
        // Find a valid edge cell to spawn the pawn
        private bool TryFindEntryCell(Map map, out IntVec3 result)
        {
            return CellFinder.TryFindRandomEdgeCellWith((IntVec3 c) => 
                map.reachability.CanReachColony(c) && !c.Fogged(map), map, CellFinder.EdgeRoadChance_Neutral, out result);
        }
        
        // Generate the specialist pawn
        private Pawn GenerateSpecialist(int tile)
        {
            // Determine specialist type
            SpecialistType specialistType = (SpecialistType)Rand.Range(0, Enum.GetValues(typeof(SpecialistType)).Length);
            
            // Generate pawn request
            PawnGenerationRequest request = new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                PawnGenerationContext.NonPlayer,
                -1,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: true,
                mustBeCapableOfViolence: false,
                colonistRelationChanceFactor: 0.5f,
                forceAddFreeWarmLayerIfNeeded: true,
                allowGay: true,
                allowPregnant: false,
                allowFood: true,
                allowAddictions: HiddenTraitChance > Rand.Value,
                inhabitant: false,
                certainlyBeenInCryptosleep: false,
                forceRedressWorldPawnIfFormerColonist: false,
                worldPawnFactionDoesntMatter: false,
                biocodeWeaponChance: 0f
            );
            
            // Generate the pawn
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            if (pawn == null)
                return null;
                
            // Apply specialist skills based on type
            ApplySpecialistSkills(pawn, specialistType);
            
            // Give appropriate equipment
            GiveSpecialistEquipment(pawn, specialistType);
            
            // Add hidden trait if applicable
            if (Rand.Value < HiddenTraitChance)
            {
                AddHiddenTrait(pawn);
            }
            
            return pawn;
        }
        
        // Apply specialist skills based on type
        private void ApplySpecialistSkills(Pawn pawn, SpecialistType type)
        {
            // Reset all skills to a baseline
            foreach (SkillRecord skill in pawn.skills.skills)
            {
                // Base skills between 2-6
                skill.Level = Rand.Range(2, 7);
            }
            
            // Apply primary skills based on specialist type
            switch (type)
            {
                case SpecialistType.Doctor:
                    pawn.skills.GetSkill(SkillDefOf.Medicine).Level = Rand.Range(MinPrimarySkillLevel, 20);
                    pawn.skills.GetSkill(SkillDefOf.Intellectual).Level = Rand.Range(8, 16);
                    pawn.skills.GetSkill(SkillDefOf.Social).Level = Rand.Range(6, 14);
                    break;
                    
                case SpecialistType.Researcher:
                    pawn.skills.GetSkill(SkillDefOf.Intellectual).Level = Rand.Range(MinPrimarySkillLevel, 20);
                    pawn.skills.GetSkill(SkillDefOf.Crafting).Level = Rand.Range(6, 14);
                    pawn.skills.GetSkill(SkillDefOf.Medicine).Level = Rand.Range(4, 12);
                    break;
                    
                case SpecialistType.Crafter:
                    pawn.skills.GetSkill(SkillDefOf.Crafting).Level = Rand.Range(MinPrimarySkillLevel, 20);
                    pawn.skills.GetSkill(SkillDefOf.Construction).Level = Rand.Range(8, 16);
                    pawn.skills.GetSkill(SkillDefOf.Artistic).Level = Rand.Range(6, 14);
                    break;
                    
                case SpecialistType.Miner:
                    pawn.skills.GetSkill(SkillDefOf.Mining).Level = Rand.Range(MinPrimarySkillLevel, 20);
                    pawn.skills.GetSkill(SkillDefOf.Construction).Level = Rand.Range(8, 16);
                    pawn.skills.GetSkill(SkillDefOf.Melee).Level = Rand.Range(6, 14);
                    break;
                    
                case SpecialistType.Farmer:
                    pawn.skills.GetSkill(SkillDefOf.Plants).Level = Rand.Range(MinPrimarySkillLevel, 20);
                    pawn.skills.GetSkill(SkillDefOf.Animals).Level = Rand.Range(8, 16);
                    pawn.skills.GetSkill(SkillDefOf.Cooking).Level = Rand.Range(6, 14);
                    break;
                    
                case SpecialistType.Shooter:
                    pawn.skills.GetSkill(SkillDefOf.Shooting).Level = Rand.Range(MinPrimarySkillLevel, 20);
                    pawn.skills.GetSkill(SkillDefOf.Melee).Level = Rand.Range(6, 14);
                    pawn.skills.GetSkill(SkillDefOf.Construction).Level = Rand.Range(4, 12);
                    break;
            }
            
            // Set passion for primary skill
            pawn.skills.GetSkill(GetPrimarySkill(pawn)).passion = Passion.Major;
            
            // Set some random passions for other skills
            int passionCount = Rand.Range(1, 3);
            for (int i = 0; i < passionCount; i++)
            {
                SkillRecord randomSkill = pawn.skills.skills.RandomElement();
                if (randomSkill.def != GetPrimarySkill(pawn) && randomSkill.passion == Passion.None)
                {
                    randomSkill.passion = Rand.Value < 0.25f ? Passion.Major : Passion.Minor;
                }
            }
        }
        
        // Get the primary skill for a specialist
        private SkillDef GetPrimarySkill(Pawn pawn)
        {
            // Find the highest skill level
            SkillRecord highestSkill = pawn.skills.skills.OrderByDescending(s => s.Level).First();
            return highestSkill.def;
        }
        
        // Give appropriate equipment based on specialist type
        private void GiveSpecialistEquipment(Pawn pawn, SpecialistType type)
        {
            // Base equipment for all specialists
            ThingDef apparelDef = DefDatabase<ThingDef>.GetNamed("Apparel_BasicShirt");
            ThingDef pantsDef = DefDatabase<ThingDef>.GetNamed("Apparel_Pants");
            
            // Create and add basic apparel
            Thing shirt = ThingMaker.MakeThing(apparelDef);
            Thing pants = ThingMaker.MakeThing(pantsDef);
            
            if (shirt != null && shirt is Apparel)
            {
                pawn.apparel.Wear((Apparel)shirt);
            }
            
            if (pants != null && pants is Apparel)
            {
                pawn.apparel.Wear((Apparel)pants);
            }
            
            // Add specialist-specific equipment
            switch (type)
            {
                case SpecialistType.Doctor:
                    // Medicine
                    Thing medicine = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
                    medicine.stackCount = Rand.Range(5, 15);
                    pawn.inventory.TryAddItemNotForSale(medicine);
                    
                    // Medical equipment if available
                    if (DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_Stethoscope") != null)
                    {
                        Thing medEquipment = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Apparel_Stethoscope"));
                        if (medEquipment != null && medEquipment is Apparel)
                        {
                            pawn.apparel.Wear((Apparel)medEquipment);
                        }
                    }
                    break;
                    
                case SpecialistType.Researcher:
                    // Datapad or similar if available
                    if (DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_Glasses") != null)
                    {
                        Thing glasses = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Apparel_Glasses"));
                        if (glasses != null && glasses is Apparel)
                        {
                            pawn.apparel.Wear((Apparel)glasses);
                        }
                    }
                    
                    // Research notes (silver)
                    Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                    silver.stackCount = Rand.Range(100, 300);
                    pawn.inventory.TryAddItemNotForSale(silver);
                    break;
                    
                case SpecialistType.Crafter:
                    // Components
                    Thing components = ThingMaker.MakeThing(ThingDefOf.ComponentIndustrial);
                    components.stackCount = Rand.Range(3, 8);
                    pawn.inventory.TryAddItemNotForSale(components);
                    
                    // Tool belt or similar if available
                    if (DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_ToolBelt") != null)
                    {
                        Thing toolBelt = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Apparel_ToolBelt"));
                        if (toolBelt != null && toolBelt is Apparel)
                        {
                            pawn.apparel.Wear((Apparel)toolBelt);
                        }
                    }
                    break;
                    
                case SpecialistType.Miner:
                    // Steel
                    Thing steel = ThingMaker.MakeThing(ThingDefOf.Steel);
                    steel.stackCount = Rand.Range(50, 150);
                    pawn.inventory.TryAddItemNotForSale(steel);
                    
                    // Mining helmet if available
                    if (DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_SimpleHelmet") != null)
                    {
                        Thing helmet = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Apparel_SimpleHelmet"));
                        if (helmet != null && helmet is Apparel)
                        {
                            pawn.apparel.Wear((Apparel)helmet);
                        }
                    }
                    break;
                    
                case SpecialistType.Farmer:
                    // Seeds (represented by raw food)
                    Thing food = ThingMaker.MakeThing(ThingDefOf.RawPotatoes);
                    food.stackCount = Rand.Range(20, 50);
                    pawn.inventory.TryAddItemNotForSale(food);
                    
                    // Cowboy hat
                    if (DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_CowboyHat") != null)
                    {
                        Thing hat = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Apparel_CowboyHat"));
                        if (hat != null && hat is Apparel)
                        {
                            pawn.apparel.Wear((Apparel)hat);
                        }
                    }
                    break;
                    
                case SpecialistType.Shooter:
                    // Give a decent weapon
                    ThingDef weaponDef = DefDatabase<ThingDef>.GetNamed("Gun_AssaultRifle");
                    Thing weapon = ThingMaker.MakeThing(weaponDef);
                    pawn.equipment.AddEquipment((ThingWithComps)weapon);
                    
                    // Flak vest if available
                    if (DefDatabase<ThingDef>.GetNamedSilentFail("Apparel_FlakVest") != null)
                    {
                        Thing vest = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Apparel_FlakVest"));
                        if (vest != null && vest is Apparel)
                        {
                            pawn.apparel.Wear((Apparel)vest);
                        }
                    }
                    break;
            }
        }
        
        // Add a hidden trait (health condition or addiction)
        private void AddHiddenTrait(Pawn pawn)
        {
            float rand = Rand.Value;
            
            if (rand < 0.4f)
            {
                // Add a minor health condition
                HediffDef hediffDef = null;
                
                switch (Rand.Range(0, 5))
                {
                    case 0:
                        hediffDef = DefDatabase<HediffDef>.GetNamed("BadBack");
                        break;
                    case 1:
                        hediffDef = DefDatabase<HediffDef>.GetNamed("Cataract");
                        break;
                    case 2:
                        hediffDef = DefDatabase<HediffDef>.GetNamed("Asthma");
                        break;
                    case 3:
                        hediffDef = DefDatabase<HediffDef>.GetNamed("HeartArteryBlockage");
                        break;
                    case 4:
                        hediffDef = DefDatabase<HediffDef>.GetNamed("Carcinoma");
                        break;
                }
                
                if (hediffDef != null)
                {
                    Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, null);
                    hediff.Severity = Rand.Range(0.15f, 0.4f); // Minor to moderate severity
                    pawn.health.AddHediff(hediff, null, null);
                }
            }
            else if (rand < 0.7f)
            {
                // Add a chemical addiction
                ChemicalDef chemicalDef = null;
                
                switch (Rand.Range(0, 3))
                {
                    case 0:
                        chemicalDef = ChemicalDefOf.Alcohol;
                        break;
                    case 1:
                        chemicalDef = DefDatabase<ChemicalDef>.GetNamed("Smokeleaf");
                        break;
                    case 2:
                        chemicalDef = DefDatabase<ChemicalDef>.GetNamed("Psychite");
                        break;
                }
                
                if (chemicalDef != null)
                {
                    Hediff_Addiction addiction = (Hediff_Addiction)HediffMaker.MakeHediff(chemicalDef.addictionHediff, pawn, null);
                    addiction.Severity = 0.5f;
                    pawn.health.AddHediff(addiction, null, null);
                }
            }
            else
            {
                // Add a useful trait or implant
                if (Rand.Value < 0.5f)
                {
                    // Add a useful trait
                    TraitDef traitDef = null;
                    
                    switch (Rand.Range(0, 5))
                    {
                        case 0:
                            traitDef = TraitDefOf.Industriousness;
                            break;
                        case 1:
                            traitDef = DefDatabase<TraitDef>.GetNamed("NaturalMood");
                            break;
                        case 2:
                            traitDef = DefDatabase<TraitDef>.GetNamed("TooSmart");
                            break;
                        case 3:
                            traitDef = TraitDefOf.Industriousness;
                            break;
                        case 4:
                            traitDef = DefDatabase<TraitDef>.GetNamed("Tough");
                            break;
                    }
                    
                    if (traitDef != null && !pawn.story.traits.HasTrait(traitDef))
                    {
                        pawn.story.traits.GainTrait(new Trait(traitDef, 1, false));
                    }
                }
                else
                {
                    // Add a useful implant
                    HediffDef implantDef = null;
                    
                    switch (Rand.Range(0, 3))
                    {
                        case 0:
                            implantDef = DefDatabase<HediffDef>.GetNamed("Joywire");
                            break;
                        case 1:
                            implantDef = DefDatabase<HediffDef>.GetNamed("Painstopper");
                            break;
                        case 2:
                            implantDef = DefDatabase<HediffDef>.GetNamed("CircadianAssistant");
                            break;
                    }
                    
                    if (implantDef != null)
                    {
                        BodyPartRecord brain = pawn.health.hediffSet.GetBrain();
                        if (brain != null)
                        {
                            Hediff hediff = HediffMaker.MakeHediff(implantDef, pawn, brain);
                            pawn.health.AddHediff(hediff, brain, null);
                        }
                    }
                }
            }
        }
    }
}
